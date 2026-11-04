using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Animal.Client.Contracts;

namespace Animal.Client.Services
{
    // Typed HttpClient for /api/pets. This is the only way the client reaches the backend.
    public class PetsApiClient
    {
        private readonly HttpClient _http;

        public PetsApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<PetResponse>> GetPetsAsync(string? species, int? maxAge, bool availableOnly, CancellationToken cancellationToken = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(species))
            {
                query.Add("species=" + Uri.EscapeDataString(species.Trim()));
            }
            if (maxAge.HasValue)
            {
                query.Add("maxAge=" + maxAge.Value);
            }
            if (availableOnly)
            {
                query.Add("availableOnly=true");
            }

            var url = "api/pets" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

            using var response = await _http.GetAsync(url, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<PetResponse>>(cancellationToken) ?? [];
        }

        // Returns null when the pet doesn't exist, so a page can show "not found" instead of an error.
        public async Task<PetResponse?> GetPetAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync($"api/pets/{id}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<PetResponse>(cancellationToken);
        }

        public async Task<PetResponse> CreatePetAsync(PetRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await _http.PostAsJsonAsync("api/pets", request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return (await response.Content.ReadFromJsonAsync<PetResponse>(cancellationToken))!;
        }

        public async Task UpdatePetAsync(int id, PetRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await _http.PutAsJsonAsync($"api/pets/{id}", request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task DeletePetAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await _http.DeleteAsync($"api/pets/{id}", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException(response.StatusCode, ReadProblemErrors(body));
        }

        // The API sends two shapes of ProblemDetails:
        //   model validation: "errors": { "Name": ["The Name field is required."] }
        //   domain rules:     "errors": ["Shelter 99 does not exist."]
        // Anything else falls back to "detail" or "title".
        private static List<string> ReadProblemErrors(string body)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(body))
            {
                return messages;
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return messages;
                }

                if (root.TryGetProperty("errors", out var errors))
                {
                    if (errors.ValueKind == JsonValueKind.Array)
                    {
                        messages.AddRange(errors.EnumerateArray().Select(e => e.ToString()));
                    }
                    else if (errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in errors.EnumerateObject())
                        {
                            messages.AddRange(field.Value.EnumerateArray().Select(e => e.ToString()));
                        }
                    }
                }

                if (messages.Count == 0 && root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                {
                    messages.Add(detail.GetString()!);
                }

                if (messages.Count == 0 && root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                {
                    messages.Add(title.GetString()!);
                }
            }
            catch (JsonException)
            {
                // Not JSON (e.g. a proxy error page). The caller falls back to the status code.
            }

            return messages;
        }
    }
}
