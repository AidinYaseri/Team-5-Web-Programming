using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Animal.IntegrationTests.Api;

public sealed class TestTokenFactory : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);

    public const string Issuer = "https://test-issuer.example";
    public const string Audience = "test-audience";

    public SecurityKey SigningKey => new RsaSecurityKey(_rsa);

    public string CreateToken(
        string? issuer = null,
        string? audience = null,
        DateTimeOffset? expiresAt = null,
        string? subject = "test-subject",
        IEnumerable<string>? scopes = null,
        string scopeClaimType = "scope",
        RSA? signingKey = null,
        IEnumerable<Claim>? extraClaims = null)
    {
        var claims = new List<Claim>(extraClaims ?? []);
        if (subject is not null)
        {
            claims.Add(new Claim("sub", subject));
        }

        if (scopes is not null)
        {
            claims.Add(new Claim(scopeClaimType, string.Join(' ', scopes)));
        }

        var expiration = (expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30)).UtcDateTime;
        var notBefore = expiration < DateTime.UtcNow
            ? expiration.AddMinutes(-1)
            : DateTime.UtcNow.AddMinutes(-1);
        var key = signingKey ?? _rsa;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Issuer,
            Audience = audience ?? Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            NotBefore = notBefore,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(key),
                SecurityAlgorithms.RsaSha256)
        };

        return new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityTokenHandler().CreateToken(descriptor));
    }

    public void Dispose()
    {
        _rsa.Dispose();
    }
}
