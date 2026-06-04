using QuickAuth.WebApi.Application.Configurations;
using QuickAuth.WebApi.Application.Services;
using QuickAuth.WebApi.Application.Services.Impl;
using QuickAuth.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Mapeia a seção do JSON para a classe JwtSettings e a jeta como IOptions<JwtSettings>
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Adicionar interfaces
builder.Services.AddTransient<ITokenValidator, TokenValidator>();

// urls em minusculo
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Middleware de autenticacao antes de qualquer Auth
app.UseMiddleware<AuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "1332";
app.Run($"http://*:{port}");
// app.Run();

// Expõe a classe Program para o WebApplicationFactory<Program> nos testes de integração                                                                                
public partial class Program { } 
