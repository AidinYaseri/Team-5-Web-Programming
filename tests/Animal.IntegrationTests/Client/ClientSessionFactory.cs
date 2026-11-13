extern alias ClientApp;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ClientProgram = ClientApp::Program;

namespace Animal.IntegrationTests.Client
{
    // Runs the real Blazor client with sign-in turned off (no Keycloak needed). Inside this test host only,
    // POST /test/sign-in signs a user in through the client's real cookie handler, the same call the OIDC handler
    // makes after Keycloak redirects back, so each test "device" gets its own real auth cookie.
    public class ClientSessionFactory : WebApplicationFactory<ClientProgram>
    {
        public const string SignInPath = "/test/sign-in";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Empty authority: the client skips OIDC, so sign-out only clears the cookie (no call to Keycloak).
            builder.UseSetting("Authentication:Authority", string.Empty);
            builder.UseSetting("Authentication:ClientId", string.Empty);
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, TestSignInStartupFilter>());
        }

        private sealed class TestSignInStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path == SignInPath)
                    {
                        var subject = context.Request.Query["sub"].ToString();
                        var properties = new AuthenticationProperties();
                        properties.StoreTokens(
                        [
                            new AuthenticationToken { Name = "access_token", Value = $"test-access-token-for-{subject}-{Guid.NewGuid():N}" },
                            new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddMinutes(30).ToString("o") }
                        ]);
                        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject), new Claim("name", subject)], "test-sign-in"));
                        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                        return;
                    }

                    await nextMiddleware(context);
                });
                next(app);
            };
        }
    }
}
