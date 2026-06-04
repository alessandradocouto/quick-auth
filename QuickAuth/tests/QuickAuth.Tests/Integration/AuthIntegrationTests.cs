using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QuickAuth.WebApi.Application.Services;

namespace QuickAuth.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithToken(ITokenValidator tokenValidator)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITokenValidator));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddTransient(_ => tokenValidator);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Get_WithoutAuthorizationHeader_Returns401()
    {
        var tokenValidator = Substitute.For<ITokenValidator>();
        var client = CreateClientWithToken(tokenValidator);

        var response = await client.GetAsync("/api/auth");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithValidToken_Returns200()
    {
        var tokenValidator = Substitute.For<ITokenValidator>();
        tokenValidator
            .ValidateJwtToken(Arg.Any<string>())
            .Returns(new System.Security.Claims.ClaimsPrincipal());

        var client = CreateClientWithToken(tokenValidator);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer valid.token");

        var response = await client.GetAsync("/api/auth");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WithInvalidToken_Returns401()
    {
        var tokenValidator = Substitute.For<ITokenValidator>();
        tokenValidator
            .ValidateJwtToken(Arg.Any<string>())
            .ThrowsAsync(new SecurityTokenException("Invalid token."));

        var client = CreateClientWithToken(tokenValidator);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.token");

        var response = await client.GetAsync("/api/auth");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
