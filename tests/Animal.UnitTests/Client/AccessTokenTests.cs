using System.Net;
using System.Security.Claims;
using Animal.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Animal.UnitTests.Client
{
    // WI-3 (decision D4): the client reads the access token on the server, from the auth cookie, and only
    // the API calls get it. These tests use a fake authentication service, so no identity provider is needed.
    public class AccessTokenTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void IsExpired_ExpiryInTheFuture_ReturnsFalse()
        {
            Assert.False(AccessTokenLifetime.IsExpired(Properties("token", Now.AddMinutes(10)), Now));
        }

        [Fact]
        public void IsExpired_ExpiryInThePastOrExactlyNow_ReturnsTrue()
        {
            Assert.True(AccessTokenLifetime.IsExpired(Properties("token", Now.AddSeconds(-1)), Now));
            Assert.True(AccessTokenLifetime.IsExpired(Properties("token", Now), Now));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not a date")]
        public void IsExpired_MissingOrUnreadableExpiry_ReturnsTrue(string? expiresAt)
        {
            var properties = new AuthenticationProperties();
            if (expiresAt is not null)
            {
                properties.StoreTokens([new AuthenticationToken { Name = "expires_at", Value = expiresAt }]);
            }

            Assert.True(AccessTokenLifetime.IsExpired(properties, Now));
            Assert.True(AccessTokenLifetime.IsExpired(null, Now));
        }

        [Fact]
        public async Task GetTokenAsync_SignedInWithValidToken_ReturnsTokenFromCookie()
        {
            var provider = Provider(AuthenticateResult.Success(Ticket(Properties("cookie-token", Now.AddMinutes(10)))));

            Assert.Equal("cookie-token", await provider.GetTokenAsync());
        }

        [Fact]
        public async Task GetTokenAsync_SignedOut_ReturnsNull()
        {
            var provider = Provider(AuthenticateResult.NoResult());

            Assert.Null(await provider.GetTokenAsync());
        }

        [Fact]
        public async Task GetTokenAsync_ExpiredToken_ReturnsNull()
        {
            var provider = Provider(AuthenticateResult.Success(Ticket(Properties("old-token", Now.AddMinutes(-1)))));

            Assert.Null(await provider.GetTokenAsync());
        }

        [Fact]
        public async Task GetTokenAsync_NoHttpContext_ReturnsNull()
        {
            var provider = new AccessTokenProvider(new HttpContextAccessor(), new FixedTime(Now));

            Assert.Null(await provider.GetTokenAsync());
        }

        [Fact]
        public async Task Handler_SignedIn_AddsBearerHeader()
        {
            var inner = new RecordingHandler();
            using var client = Client(Provider(AuthenticateResult.Success(Ticket(Properties("cookie-token", Now.AddMinutes(10))))), inner);

            await client.GetAsync("http://api.test/api/users/me");

            Assert.Equal("Bearer", inner.LastRequest!.Headers.Authorization?.Scheme);
            Assert.Equal("cookie-token", inner.LastRequest.Headers.Authorization?.Parameter);
        }

        [Fact]
        public async Task Handler_SignedOutOrExpired_SendsNoAuthorizationHeader()
        {
            foreach (var result in new[]
                     {
                         AuthenticateResult.NoResult(),
                         AuthenticateResult.Success(Ticket(Properties("old-token", Now.AddMinutes(-1))))
                     })
            {
                var inner = new RecordingHandler();
                using var client = Client(Provider(result), inner);

                await client.GetAsync("http://api.test/api/pets");

                Assert.Null(inner.LastRequest!.Headers.Authorization);
            }
        }

        [Fact]
        public async Task CookieValidation_ExpiredAccessToken_RejectsTheCookie()
        {
            var context = CookieContext(Properties("old-token", Now.AddMinutes(-1)));

            await AuthCookieEvents.RejectIfAccessTokenExpired(context, new FixedTime(Now));

            Assert.Null(context.Principal);
        }

        [Fact]
        public async Task CookieValidation_ValidAccessToken_KeepsTheCookie()
        {
            var context = CookieContext(Properties("cookie-token", Now.AddMinutes(10)));

            await AuthCookieEvents.RejectIfAccessTokenExpired(context, new FixedTime(Now));

            Assert.NotNull(context.Principal);
        }

        private static CookieValidatePrincipalContext CookieContext(AuthenticationProperties properties) =>
            new(new DefaultHttpContext(),
                new AuthenticationScheme("Cookies", null, typeof(CookieAuthenticationHandler)),
                new CookieAuthenticationOptions(),
                Ticket(properties));

        private static AuthenticationProperties Properties(string accessToken, DateTimeOffset expiresAt)
        {
            var properties = new AuthenticationProperties();
            properties.StoreTokens(
            [
                new AuthenticationToken { Name = "access_token", Value = accessToken },
                new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o") }
            ]);
            return properties;
        }

        private static AuthenticationTicket Ticket(AuthenticationProperties properties) =>
            new(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "Cookies")), properties, "Cookies");

        private static AccessTokenProvider Provider(AuthenticateResult result)
        {
            var services = new ServiceCollection()
                .AddSingleton<IAuthenticationService>(new FakeAuthenticationService(result))
                .BuildServiceProvider();
            var context = new DefaultHttpContext { RequestServices = services };
            return new AccessTokenProvider(new HttpContextAccessor { HttpContext = context }, new FixedTime(Now));
        }

        private static HttpClient Client(AccessTokenProvider provider, HttpMessageHandler inner) =>
            new(new AccessTokenHandler(provider) { InnerHandler = inner });

        private sealed class FakeAuthenticationService(AuthenticateResult result) : IAuthenticationService
        {
            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(result);
            public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
            public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
            public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
            public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        private sealed class FixedTime(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }
    }
}
