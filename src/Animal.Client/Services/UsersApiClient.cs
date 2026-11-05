using System.Net;
using System.Net.Http.Json;
using Animal.Client.Contracts;

namespace Animal.Client.Services
{
    public class UsersApiClient
    {
        private readonly HttpClient _http;

        public UsersApiClient(HttpClient http)
        {
            _http = http;
        }

        // Returns null when the authenticated user has not created a profile yet.
        public async Task<UserResponse?> GetMyProfileAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync("api/users/me", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken);
        }

        public async Task UpdateMyProfileAsync(
            UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            using var response = await _http.PutAsJsonAsync("api/users/me", request, cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }
}
