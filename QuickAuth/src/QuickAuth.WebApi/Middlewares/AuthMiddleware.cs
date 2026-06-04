using System.Security;
using Microsoft.IdentityModel.Tokens;
using QuickAuth.WebApi.Application.Services;

namespace QuickAuth.WebApi.Middlewares;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;
    private readonly ITokenValidator _tokenValidator;

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger, ITokenValidator tokenValidator)
    {
        _next = next;
        _logger = logger;
        _tokenValidator = tokenValidator;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            _logger.LogInformation("Starting everything...");
            // extrair token
            var tokenJwt = await ExtractTokenJwtAsync(context.Request.Headers, context);
            
            if (string.IsNullOrEmpty(tokenJwt))
            {
                await ErrorResponseAsync(context, "Authorization header is missing or invalid.");
                return; // <-- impede de chamar ValidateJwtToken("") e _next
            }
            
            // validar token
            var principal = await _tokenValidator.ValidateJwtToken(tokenJwt);
            _logger.LogInformation("Next Middleware or going to Controller.");
            await _next(context);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogError($"Invalid token: {ex?.Message} - {ex?.StackTrace}");
            await ErrorResponseAsync(context, "Invalid or expired token.");
        }

        _logger.LogInformation("Exit of AuthMiddleware.");
    }
    
    // Extrai o token jwt
    private async Task<string> ExtractTokenJwtAsync(IHeaderDictionary headers, HttpContext context)
    {
        _logger.LogInformation($"Trying to extract token...");
        var getAuthorization = headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(getAuthorization) || (!getAuthorization.ToString().StartsWith("Bearer ")))
        {
            await ErrorResponseAsync(context, "Authorization header is missing.");
            return string.Empty;                                                                                          
        }
        
        _logger.LogInformation($"Extracted Token JWT.");
        return getAuthorization.Substring("Bearer ".Length).Trim();
    }
    
    private async Task ErrorResponseAsync(HttpContext context, string message)
    {   // Se http respondeu(enviou) status code + headres pro navegador, nao irei setar erros
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }
}