extern alias ClientApp;

using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ClientProgram = ClientApp::Program;

namespace Animal.IntegrationTests.Client
{
    // WI-3: runs the real Blazor client (Development settings, pointing at the local Keycloak) and checks that the
    // access token can't reach the browser and that the auth cookie is locked down. No identity provider is called:
    // OIDC only contacts Keycloak when someone actually signs in.
    public class ClientTokenHandlingTests : IClassFixture<WebApplicationFactory<ClientProgram>>
    {
        private readonly WebApplicationFactory<ClientProgram> _factory;

        public ClientTokenHandlingTests(WebApplicationFactory<ClientProgram> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/auth/access-token")]
        [InlineData("/auth.js")]
        public async Task OldTokenRouteAndScript_AreGone(string url)
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task HomePage_LoadsNoTokenScript()
        {
            using var client = _factory.CreateClient();

            var html = await client.GetStringAsync("/");

            Assert.DoesNotContain("auth.js", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("access-token", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AuthCookie_IsHttpOnlyLaxNotSlidingAndExpiresWithTheToken()
        {
            var options = CookieOptions(_factory);

            Assert.True(options.Cookie.HttpOnly);
            Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
            Assert.False(options.SlidingExpiration);
            Assert.Equal(TimeSpan.FromMinutes(30), options.ExpireTimeSpan);
            Assert.NotNull(options.Events.OnValidatePrincipal);
            // Sessions are kept on the server, so sign-out can end them (see ClientSignOutTests).
            Assert.IsType<ClientApp::Animal.Client.Services.InMemoryTicketStore>(options.SessionStore);
        }

        [Fact]
        public void AuthCookie_OutsideDevelopment_IsHttpsOnly()
        {
            using var production = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

            Assert.Equal(CookieSecurePolicy.Always, CookieOptions(production).Cookie.SecurePolicy);
        }

        [Fact]
        public void SignIn_UsesCodeFlowWithPkceAndKeepsTokensInTheCookie()
        {
            var options = _factory.Services
                .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                .Get(OpenIdConnectDefaults.AuthenticationScheme);

            Assert.Equal("code", options.ResponseType);
            Assert.True(options.UsePkce);
            Assert.True(options.SaveTokens);
            Assert.Equal("animal-client", options.ClientId);
        }

        private static CookieAuthenticationOptions CookieOptions(WebApplicationFactory<ClientProgram> factory) =>
            factory.Services
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
