using System.Net;
using System.Net.Http.Json;
using Animal.Client.Contracts;

namespace Animal.Client.Services
{
    // The browser-facing layer reaches the API only through this typed HTTP client.
    public sealed class PetsApiClient(HttpClient http)
    {
        public async Task<List<PetResponse>> GetPetsAsync(string? species, int? maxAge, bool availableOnly, CancellationToken cancellationToken = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(species)) query.Add("species=" + Uri.EscapeDataString(species.Trim()));
            if (maxAge.HasValue) query.Add("maxAge=" + maxAge.Value);
            if (availableOnly) query.Add("availableOnly=true");

            var url = "api/pets" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            using var response = await http.GetAsync(url, cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<PetResponse>>(cancellationToken) ?? [];
        }

        public async Task<PetResponse?> GetPetAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await http.GetAsync($"api/pets/{id}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<PetResponse>(cancellationToken);
        }

        public async Task<PetResponse> CreatePetAsync(PetRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await http.PostAsJsonAsync("api/pets", request, cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return (await response.Content.ReadFromJsonAsync<PetResponse>(cancellationToken))!;
        }

        public async Task UpdatePetAsync(int id, PetRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await http.PutAsJsonAsync($"api/pets/{id}", request, cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task DeletePetAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await http.DeleteAsync($"api/pets/{id}", cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }
}
