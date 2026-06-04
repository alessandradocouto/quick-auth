using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using QuickAuth.WebApi.Application.Configurations;

namespace QuickAuth.WebApi.Application.Services.Impl;

public class TokenValidator : ITokenValidator
{
    private readonly ILogger<TokenValidator> _logger;
    
    private readonly JwtSettings _jwtSettings;

    public TokenValidator(ILogger<TokenValidator> logger, IOptions<JwtSettings> jwtSettings)
    {
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }


    public async Task<ClaimsPrincipal> ValidateJwtToken(string token)
    {
        _logger.LogInformation($"Starting validation...");
    
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
    
        _logger.LogInformation($"Set parameters...");

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = "test",
            ValidateAudience = true,
            ValidAudience = "test-app",
            ValidateLifetime = true,
            ValidTypes = ["JWT"],
            ValidAlgorithms = new List<string>(){ SecurityAlgorithms.HmacSha256 }
        };
    
        var handler = new JsonWebTokenHandler();
    
        _logger.LogInformation($"Searching results...");
    
        var result = await handler.ValidateTokenAsync(token, validationParams);
    
        if (result.IsValid)
        {
            _logger.LogInformation($"Token JWT valided with successfully. Back to Middleware");
            return new ClaimsPrincipal(result.ClaimsIdentity);
        }
    
        _logger.LogWarning($"Token validation failed: {result.Exception?.Message}");
        throw new SecurityTokenException("Invalid token registration.", result.Exception);
    }
}