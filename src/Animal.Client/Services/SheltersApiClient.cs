using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Animal.Client.Contracts;

namespace Animal.Client.Services
{
    public class SheltersApiClient
    {
        private readonly HttpClient _http;

        public SheltersApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ShelterResponse>> GetSheltersAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync("api/shelters", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<ShelterResponse>>(cancellationToken) ?? [];
        }

        public async Task<ShelterResponse?> GetShelterAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync($"api/shelters/{id}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ShelterResponse>(cancellationToken);
        }

        public async Task<List<PetResponse>> GetShelterPetsAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync($"api/shelters/{id}/pets", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<PetResponse>>(cancellationToken) ?? [];
        }

        public async Task<ShelterResponse> CreateShelterAsync(ShelterRequest request, CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/shelters")
            {
                Content = JsonContent.Create(request)
            };
            AddWritePermission(message);

            using var response = await _http.SendAsync(message, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return (await response.Content.ReadFromJsonAsync<ShelterResponse>(cancellationToken))!;
        }

        public async Task UpdateShelterAsync(int id, ShelterRequest request, CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(HttpMethod.Put, $"api/shelters/{id}")
            {
                Content = JsonContent.Create(request)
            };
            AddWritePermission(message);

            using var response = await _http.SendAsync(message, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task DeleteShelterAsync(int id, CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, $"api/shelters/{id}");
            AddWritePermission(message);

            using var response = await _http.SendAsync(message, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        private static void AddWritePermission(HttpRequestMessage message)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "shelters.write");
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException(response.StatusCode, PetsApiClient.ReadProblemErrors(body));
        }
    }
}