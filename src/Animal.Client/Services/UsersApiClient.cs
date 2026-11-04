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

            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken);
        }

        public async Task UpdateMyProfileAsync(
            UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            using var response = await _http.PutAsJsonAsync("api/users/me", request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException(response.StatusCode, ReadProblemErrors(body));
        }

        private static List<string> ReadProblemErrors(string body)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(body))
            {
                return messages;
            }

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
                {
                    return messages;
                }

                if (root.TryGetProperty("errors", out var errors))
                {
                    if (errors.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        messages.AddRange(errors.EnumerateArray().Select(error => error.ToString()));
                    }
                    else if (errors.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var field in errors.EnumerateObject())
                        {
                            messages.AddRange(field.Value.EnumerateArray().Select(error => error.ToString()));
                        }
                    }
                }

                if (messages.Count == 0
                    && root.TryGetProperty("detail", out var detail)
                    && detail.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    messages.Add(detail.GetString()!);
                }

                if (messages.Count == 0
                    && root.TryGetProperty("title", out var title)
                    && title.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    messages.Add(title.GetString()!);
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // The caller falls back to the status code for non-JSON responses.
            }

            return messages;
        }
    }
}
