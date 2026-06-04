using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using QuickAuth.WebApi.Application.Services;

namespace QuickAuth.Tests.Integration;

public class SignInIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SignInIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var tokenValidator = Substitute.For<ITokenValidator>();
        tokenValidator
            .ValidateJwtToken(Arg.Any<string>())
            .Returns(new System.Security.Claims.ClaimsPrincipal());

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITokenValidator));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddTransient(_ => tokenValidator);
            });
        });
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test.token");
        return client;
    }

    [Fact]
    public async Task Post_ValidCpf_Returns200()
    {
        var response = await CreateAuthenticatedClient()
            .PostAsJsonAsync("/api/signin", new { Cpf = "529.982.247-25" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_InvalidCpf_Returns400()
    {
        var response = await CreateAuthenticatedClient()
            .PostAsJsonAsync("/api/signin", new { Cpf = "123.456.789-00" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmptyCpf_Returns400()
    {
        var response = await CreateAuthenticatedClient()
            .PostAsJsonAsync("/api/signin", new { Cpf = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithoutAuthorizationHeader_Returns401()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/signin", new { Cpf = "529.982.247-25" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
