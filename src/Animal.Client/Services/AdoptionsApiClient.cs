using System.Net.Http.Json;
using Animal.Client.Contracts;

namespace Animal.Client.Services
{
    // Typed HttpClient for /api/adoptions. AccessTokenHandler attaches the signed-in user's token.
    public sealed class AdoptionsApiClient(HttpClient http)
    {
        public async Task<AdoptionResponse> ApplyAsync(int petId, CancellationToken cancellationToken = default)
        {
            using var response = await http.PostAsJsonAsync("api/adoptions", new CreateAdoptionRequest(petId), cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return (await response.Content.ReadFromJsonAsync<AdoptionResponse>(cancellationToken))!;
        }

        public async Task<IReadOnlyList<AdoptionResponse>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            using var response = await http.GetAsync("api/adoptions/mine", cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<AdoptionResponse>>(cancellationToken) ?? [];
        }

        // Reviewers only (adoptions.review). Without a status, every application is returned.
        public async Task<IReadOnlyList<AdoptionResponse>> GetQueueAsync(string? status = "Pending", CancellationToken cancellationToken = default)
        {
            var url = string.IsNullOrWhiteSpace(status) ? "api/adoptions" : "api/adoptions?status=" + Uri.EscapeDataString(status);
            using var response = await http.GetAsync(url, cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<AdoptionResponse>>(cancellationToken) ?? [];
        }

        public async Task ApproveAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await http.PostAsync($"api/adoptions/{id}/approve", content: null, cancellationToken: cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task RejectAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await http.PostAsync($"api/adoptions/{id}/reject", content: null, cancellationToken: cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }
}
