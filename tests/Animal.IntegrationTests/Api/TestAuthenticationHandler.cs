using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Animal.IntegrationTests.Api
{
    public sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith(SchemeName + " ", StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var values = header[(SchemeName.Length + 1)..].Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 0 || !int.TryParse(values[0], out var userId) || userId <= 0)
            {
                return Task.FromResult(AuthenticateResult.Fail("The test token needs a positive user ID."));
            }

            var claims = new List<Claim> { new("user_id", userId.ToString()) };
            if (values.Length > 1)
            {
                claims.Add(new Claim("scope", values[1]));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }
    }
}
