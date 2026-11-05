using Animal.API.Contracts;

namespace Animal.API.Services
{
    public interface IAppointmentService
    {
        Task<IReadOnlyList<AppointmentResponse>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<AppointmentResponse> CreateAsync(int userId, CreateAppointmentRequest request, CancellationToken cancellationToken = default);
        Task<bool> CancelAsync(int userId, int appointmentId, CancellationToken cancellationToken = default);
    }
}
