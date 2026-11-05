using Animal.Data.Context;
using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public sealed class AppointmentRepository(AnimalDbContext context) : IAppointmentRepository
    {
        public async Task<IReadOnlyList<Appointment>> GetForUserAsync(int userId, CancellationToken cancellationToken = default) =>
            await context.Appointments.AsNoTracking().Where(a => a.UserId == userId)
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.Id).ToListAsync(cancellationToken);

        public Task<Appointment?> GetForUserAsync(int userId, int appointmentId, CancellationToken cancellationToken = default) =>
            context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId && a.UserId == userId, cancellationToken);

        public Task<int?> GetShelterIdForPetAsync(int petId, CancellationToken cancellationToken = default) =>
            context.Pets.Where(p => p.Id == petId).Select(p => (int?)p.ShelterId).FirstOrDefaultAsync(cancellationToken);

        public Task<bool> IsSlotTakenAsync(int shelterId, DateTime appointmentDate, CancellationToken cancellationToken = default) =>
            context.Appointments.AnyAsync(a => a.ShelterId == shelterId && a.AppointmentDate == appointmentDate &&
                a.Status == AppointmentStatus.Scheduled, cancellationToken);

        public void Add(Appointment appointment) => context.Appointments.Add(appointment);
    }
}
