using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using QuickAuth.WebApi.Application.DTOs.Requests;
using QuickAuth.WebApi.Application.DTOs.Responses;
using QuickAuth.WebApi.Domain.ValueOfObjects;

namespace QuickAuth.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignInController : ControllerBase
{
    [HttpPost]
    public IActionResult SignIn([FromBody] SignInRequest request)
    {
        SignInResponse response;

        if (request.Cpf.IsValidCpf())
        {
            response = new SignInResponse
            {
                Status = HttpStatusCode.OK,
                Message = "Access authorized. Valid CPF."
            };
            
            return Ok(response);
        }
            
        response = new SignInResponse
        {
            Status = HttpStatusCode.BadRequest,
            Message = "Access Failed. Invalid CPF."
        };
        
        return BadRequest(response);
    }
}