using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.API.Services
{
    public sealed class AppointmentService(
        IAppointmentRepository appointments,
        INotificationService notifications,
        TimeProvider timeProvider) : IAppointmentService
    {
        public async Task<IReadOnlyList<AppointmentResponse>> GetForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var results = await appointments.GetForUserAsync(userId, cancellationToken);
            return results.Select(AppointmentResponse.FromEntity).ToList();
        }

        public async Task<AppointmentResponse> CreateAsync(int userId, CreateAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (userId <= 0)
            {
                throw new DomainValidationException("A valid user is required to book an appointment.");
            }

            var shelterId = await appointments.GetShelterIdForPetAsync(request.PetId, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(Pet), request.PetId);

            var appointment = new Appointment
            {
                UserId = userId,
                PetId = request.PetId,
                ShelterId = shelterId,
                AppointmentDate = request.AppointmentDate?.UtcDateTime
                    ?? throw new DomainValidationException("A visit date and time are required."),
            };

            AppointmentRules.EnsureCanSchedule(appointment, timeProvider.GetUtcNow().UtcDateTime);
            AppointmentRules.EnsureSlotIsAvailable(await appointments.IsSlotTakenAsync(shelterId, appointment.AppointmentDate, cancellationToken));

            appointments.Add(appointment);
            await notifications.CreateAsync(userId, $"Your visit for pet {request.PetId} is scheduled for {appointment.AppointmentDate:O}.", cancellationToken);

            return AppointmentResponse.FromEntity(appointment);
        }

        public async Task<bool> CancelAsync(int userId, int appointmentId, CancellationToken cancellationToken = default)
        {
            var appointment = await appointments.GetForUserAsync(userId, appointmentId, cancellationToken);
            if (appointment is null)
            {
                return false;
            }

            AppointmentRules.Cancel(appointment);
            await notifications.CreateAsync(userId, $"Your visit for pet {appointment.PetId} on {appointment.AppointmentDate:O} was cancelled.", cancellationToken);
            return true;
        }
    }
}
