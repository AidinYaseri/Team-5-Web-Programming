using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Animal.IntegrationTests.Client
{
    // WI-3 / WI-10: signing out of one device removes that device's access, and only that device's.
    // Each Device keeps its own cookies, like a separate browser, so nothing is shared between devices.
    public partial class ClientSignOutTests : IClassFixture<ClientSessionFactory>
    {
        private const string SignedInMarker = ">Sign out</button>";
        private readonly ClientSessionFactory _factory;

        public ClientSignOutTests(ClientSessionFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SignOut_OnDeviceA_EndsOnlyDeviceA()
        {
            using var deviceA = await SignInAsync("same-user");
            using var deviceB = await SignInAsync("same-user");
            Assert.NotEqual(deviceA.Cookies, deviceB.Cookies);
            Assert.True(await deviceA.IsSignedInAsync());
            Assert.True(await deviceB.IsSignedInAsync());

            await deviceA.SignOutAsync();

            Assert.False(await deviceA.IsSignedInAsync());
            Assert.True(await deviceB.IsSignedInAsync());
        }

        [Fact]
        public async Task SignOut_CopiedCookieFromBeforeSignOut_NoLongerWorks()
        {
            using var deviceA = await SignInAsync("cookie-copy-target");
            var copiedCookies = deviceA.Cookies;

            await deviceA.SignOutAsync();

            // Someone who copied A's cookie before the sign-out replays it from another browser.
            using var attacker = new Device(_factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false }), copiedCookies);
            Assert.False(await attacker.IsSignedInAsync());
        }

        [Fact]
        public async Task SignedInPages_NeverContainTheAccessToken()
        {
            using var device = await SignInAsync("no-token-in-html");

            var html = await device.GetStringAsync("/");

            Assert.DoesNotContain("test-access-token-for-", html);
        }

        private async Task<Device> SignInAsync(string subject)
        {
            var device = new Device(_factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false }));
            var response = await device.SendAsync(HttpMethod.Post, $"{ClientSessionFactory.SignInPath}?sub={subject}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            return device;
        }

        private sealed partial class Device(HttpClient client, IReadOnlyDictionary<string, string>? cookies = null) : IDisposable
        {
            private readonly Dictionary<string, string> _cookies = cookies is null ? [] : new(cookies);

            public IReadOnlyDictionary<string, string> Cookies => new Dictionary<string, string>(_cookies);

            public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null)
            {
                using var request = new HttpRequestMessage(method, url) { Content = content };
                if (_cookies.Count > 0)
                {
                    request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
                }

                var response = await client.SendAsync(request);
                if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                {
                    foreach (var header in setCookies)
                    {
                        var pair = header.Split(';', 2)[0].Split('=', 2);
                        var expired = header.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);
                        if (expired || pair[1].Length == 0) _cookies.Remove(pair[0]);
                        else _cookies[pair[0]] = pair[1];
                    }
                }

                return response;
            }

            public async Task<string> GetStringAsync(string url) =>
                await (await SendAsync(HttpMethod.Get, url)).Content.ReadAsStringAsync();

            public async Task<bool> IsSignedInAsync() => (await GetStringAsync("/")).Contains(SignedInMarker);

            // Same as clicking "Sign out": the form posts with its antiforgery token.
            public async Task SignOutAsync()
            {
                var token = AntiforgeryField().Match(await GetStringAsync("/")).Groups[1].Value;
                Assert.False(string.IsNullOrEmpty(token), "The signed-in page should contain the sign-out form.");

                var response = await SendAsync(HttpMethod.Post, "/auth/logout",
                    new FormUrlEncodedContent([new KeyValuePair<string, string>("__RequestVerificationToken", token)]));

                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            }

            public void Dispose() => client.Dispose();

            [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
            private static partial Regex AntiforgeryField();
        }
    }
}
