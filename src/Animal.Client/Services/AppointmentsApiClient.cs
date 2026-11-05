using System.Net.Http.Json;
using Animal.Client.Contracts;

namespace Animal.Client.Services
{
    public sealed class AppointmentsApiClient(HttpClient http)
    {
        public async Task<IReadOnlyList<AppointmentResponse>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            using var response = await http.GetAsync("api/appointments", cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<AppointmentResponse>>(cancellationToken) ?? [];
        }

        public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await http.PostAsJsonAsync("api/appointments", request, cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return (await response.Content.ReadFromJsonAsync<AppointmentResponse>(cancellationToken))!;
        }

        public async Task CancelAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await http.PostAsync($"api/appointments/{id}/cancel", content: null, cancellationToken: cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }
}
