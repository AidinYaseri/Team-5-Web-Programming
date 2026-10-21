using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Not implemented yet. The slice owner switches these to the DTOs in docs/milestone-3/api-contracts.md.
    public interface IAppointmentService
    {
        Task<Appointment> GetAppointmentAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Appointment>> GetAppointmentsAsync(CancellationToken cancellationToken = default);
        Task<Appointment> CreateAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task UpdateAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task CancelAppointmentAsync(int id, CancellationToken cancellationToken = default);
    }
}
