using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Animal.API.Auth
{
    public class PermissionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public PermissionAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var value))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var header = value.ToString();
            if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Unsupported authorization scheme."));
            }

            var token = header[7..].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing bearer token."));
            }

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };
            var permissions = token.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (permissions.Contains("shelters.write", StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("permission", "shelters.write"));
            }

            if (permissions.Contains("pets.write", StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("permission", "pets.write"));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}