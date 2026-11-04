using System.Net;
using System.Text.Json;

namespace Animal.Client.Services
{
    internal static class ApiResponse
    {
        public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var messages = ReadMessages(body);
            if (messages.Count == 0)
            {
                messages.Add(response.StatusCode switch
                {
                    HttpStatusCode.BadRequest => "Check the information and try again.",
                    HttpStatusCode.Unauthorized => "Sign in to continue.",
                    HttpStatusCode.Forbidden => "Your account doesn't have permission for this action.",
                    HttpStatusCode.NotFound => "The requested item could not be found.",
                    HttpStatusCode.Conflict => "This action conflicts with the current information.",
                    HttpStatusCode.InternalServerError => "The service had a problem. Try again later.",
                    _ => $"The service returned {(int)response.StatusCode}."
                });
            }

            throw new ApiException(response.StatusCode, messages);
        }

        private static List<string> ReadMessages(string body)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(body)) return messages;

            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return messages;

                if (root.TryGetProperty("errors", out var errors))
                {
                    if (errors.ValueKind == JsonValueKind.Array)
                        messages.AddRange(errors.EnumerateArray().Select(error => error.ToString()));
                    else if (errors.ValueKind == JsonValueKind.Object)
                        foreach (var field in errors.EnumerateObject())
                            messages.AddRange(field.Value.EnumerateArray().Select(error => error.ToString()));
                }

                if (messages.Count == 0 && root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    messages.Add(detail.GetString()!);
                if (messages.Count == 0 && root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    messages.Add(title.GetString()!);
            }
            catch (JsonException)
            {
                // Use the readable status fallback for non-ProblemDetails responses.
            }

            return messages;
        }
    }
}
