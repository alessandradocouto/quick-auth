using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QuickAuth.WebApi.Application.Services;
using QuickAuth.WebApi.Middlewares;

namespace QuickAuth.Tests.Unit;

public class AuthMiddlewareTests
{
    private readonly ITokenValidator _tokenValidator = Substitute.For<ITokenValidator>();
    private readonly ILogger<AuthMiddleware> _logger = Substitute.For<ILogger<AuthMiddleware>>();

    private AuthMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, _logger, _tokenValidator);

    private static DefaultHttpContext CreateContext(string? authorizationHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (authorizationHeader is not null)
            context.Request.Headers["Authorization"] = authorizationHeader;
        return context;
    }

    [Fact]
    public async Task InvokeAsync_MissingAuthorizationHeader_Returns401()
    {
        var next = Substitute.For<RequestDelegate>();
        var middleware = CreateMiddleware(next);
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        await next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task InvokeAsync_HeaderWithoutBearerPrefix_Returns401()
    {
        var next = Substitute.For<RequestDelegate>();
        var middleware = CreateMiddleware(next);
        var context = CreateContext("invalid-token-without-bearer");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        await next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task InvokeAsync_InvalidToken_Returns401()
    {
        _tokenValidator
            .ValidateJwtToken(Arg.Any<string>())
            .ThrowsAsync(new SecurityTokenException("Invalid token."));

        var next = Substitute.For<RequestDelegate>();
        var middleware = CreateMiddleware(next);
        var context = CreateContext("Bearer invalid.jwt.token");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        await next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task InvokeAsync_ValidToken_CallsNextMiddleware()
    {
        _tokenValidator
            .ValidateJwtToken(Arg.Any<string>())
            .Returns(new System.Security.Claims.ClaimsPrincipal());

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = CreateMiddleware(next);
        var context = CreateContext("Bearer valid.jwt.token");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
