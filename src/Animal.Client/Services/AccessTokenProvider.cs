using Microsoft.JSInterop;

namespace Animal.Client.Services
{
    public sealed class AccessTokenProvider(IJSRuntime jsRuntime)
    {
        private string? _accessToken;

        public async ValueTask<string?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(_accessToken)) return _accessToken;

            try
            {
                _accessToken = await jsRuntime.InvokeAsync<string?>("animalAuth.getAccessToken", cancellationToken);
                return _accessToken;
            }
            catch (JSException)
            {
                // During static rendering JS interop isn't available. Public pages can still call the API.
                return null;
            }
            catch (InvalidOperationException)
            {
                // Blazor Server also throws InvalidOperationException when prerendering hasn't become interactive yet.
                // The unauthenticated pet browse endpoint remains usable during that first render.
                return null;
            }
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            _accessToken = await jsRuntime.InvokeAsync<string?>("animalAuth.getAccessToken", cancellationToken);
        }
    }
}
