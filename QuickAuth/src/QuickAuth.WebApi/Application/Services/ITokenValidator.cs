using System.Security.Claims;

namespace QuickAuth.WebApi.Application.Services;

public interface ITokenValidator
{
    Task<ClaimsPrincipal> ValidateJwtToken(string token);
}