using Animal.Domain.Entities;

namespace Animal.Data.Repositories
{
    public interface IAppointmentRepository
    {
        Task<IReadOnlyList<Appointment>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<Appointment?> GetForUserAsync(int userId, int appointmentId, CancellationToken cancellationToken = default);
        Task<int?> GetShelterIdForPetAsync(int petId, CancellationToken cancellationToken = default);
        Task<bool> IsSlotTakenAsync(int shelterId, DateTime appointmentDate, CancellationToken cancellationToken = default);
        void Add(Appointment appointment);
    }
}
