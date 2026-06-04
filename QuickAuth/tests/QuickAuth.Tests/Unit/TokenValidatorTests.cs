using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using QuickAuth.WebApi.Application.Configurations;
using QuickAuth.WebApi.Application.Services.Impl;

namespace QuickAuth.Tests.Unit;

public class TokenValidatorTests
{
    private const string ValidSecret = "this-is-a-test-secret-key-32-cha";

    private static TokenValidator CreateValidator(string? secretKey = null)
    {
        var logger = Substitute.For<ILogger<TokenValidator>>();
        var settings = Options.Create(new JwtSettings { SecretKey = secretKey ?? ValidSecret });
        return new TokenValidator(logger, settings);
    }

    private static string GenerateToken(
        string? secret = null,
        DateTime? expires = null,
        string issuer = "test",
        string audience = "test-app",
        IEnumerable<Claim>? claims = null)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret ?? ValidSecret));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims ?? []),
            TokenType = "JWT"
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    [Fact]
    public async Task ValidateJwtToken_TokenValido_DeveRetornarClaimsPrincipalComClaims()
    {
        // Arrange
        var claim = new Claim("role", "admin");
        var token = GenerateToken(claims: [claim]);
        var validator = CreateValidator();

        // Act
        var principal = await validator.ValidateJwtToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
    }

    [Fact]
    public async Task ValidateJwtToken_TokenExpirado_DeveLancarSecurityTokenException()
    {
        // Arrange
        var token = GenerateToken(expires: DateTime.UtcNow.AddHours(-1));
        var validator = CreateValidator();

        // Act
        var act = () => validator.ValidateJwtToken(token);

        // Assert
        await act.Should().ThrowAsync<SecurityTokenException>();
    }

    [Fact]
    public async Task ValidateJwtToken_AssinaturaInvalida_DeveLancarSecurityTokenException()
    {
        // Arrange — token assinado com chave diferente da configurada no validator
        var token = GenerateToken(secret: "outra-chave-completamente-diferente");
        var validator = CreateValidator();

        // Act
        var act = () => validator.ValidateJwtToken(token);

        // Assert
        await act.Should().ThrowAsync<SecurityTokenException>();
    }

    [Fact]
    public async Task ValidateJwtToken_TokenMalformado_DeveLancarSecurityTokenException()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var act = () => validator.ValidateJwtToken("token.malformado.invalido");

        // Assert
        await act.Should().ThrowAsync<SecurityTokenException>();
    }
}
