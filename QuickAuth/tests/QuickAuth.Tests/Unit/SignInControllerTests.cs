using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using QuickAuth.WebApi.Application.DTOs.Requests;
using QuickAuth.WebApi.Application.DTOs.Responses;
using QuickAuth.WebApi.Controllers;

namespace QuickAuth.Tests.Unit;

public class SignInControllerTests
{
    private readonly SignInController _controller = new();

    [Fact]
    public void SignIn_ValidCpf_ReturnsOkWithAuthorizedMessage()
    {
        var request = new SignInRequest { Cpf = "529.982.247-25" };

        var result = _controller.SignIn(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<SignInResponse>().Subject;
        response.Status.Should().Be(HttpStatusCode.OK);
        response.Message.Should().Contain("authorized");
    }

    [Fact]
    public void SignIn_InvalidCpf_ReturnsBadRequestWithFailedMessage()
    {
        var request = new SignInRequest { Cpf = "123.456.789-00" };

        var result = _controller.SignIn(request);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = bad.Value.Should().BeOfType<SignInResponse>().Subject;
        response.Status.Should().Be(HttpStatusCode.BadRequest);
        response.Message.Should().Contain("Invalid");
    }

    [Fact]
    public void SignIn_AllSameDigitsCpf_ReturnsBadRequest()
    {
        var request = new SignInRequest { Cpf = "111.111.111-11" };

        var result = _controller.SignIn(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
