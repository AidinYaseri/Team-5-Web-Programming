using System.Net;
using System.Net.Http.Json;
using Animal.Client.Contracts;

namespace Animal.Client.Services
{
    public sealed class NotificationsApiClient(HttpClient http)
    {
        public async Task<IReadOnlyList<NotificationResponse>> GetAsync(bool unreadOnly = false, CancellationToken cancellationToken = default)
        {
            using var response = await http.GetAsync($"api/notifications?unreadOnly={unreadOnly.ToString().ToLowerInvariant()}", cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<NotificationResponse>>(cancellationToken) ?? [];
        }

        public async Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await http.PostAsync($"api/notifications/{id}/read", content: null, cancellationToken: cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }
}
