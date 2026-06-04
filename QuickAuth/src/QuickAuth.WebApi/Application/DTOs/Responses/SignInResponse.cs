using System.Net;

namespace QuickAuth.WebApi.Application.DTOs.Responses;

public class SignInResponse
{
    public HttpStatusCode Status { get; set; }
    public string Message { get; set; } = string.Empty;
}