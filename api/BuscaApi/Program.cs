using BuscaApi.Application.Services;
using BuscaApi.Auth;
using BuscaApi.Domain.Interfaces;
using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Interfaces.Services;
using BuscaApi.Infrastructure.Data;
using BuscaApi.Infrastructure.Repositorios;
using BuscaApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "FrontendVite";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5174", "http://127.0.0.1:5174")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Infraestrutura de dados
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<IContaRepository, ContaRepository>();
builder.Services.AddScoped<IPastaRepository, PastaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IEnvioRepository, EnvioRepository>();

// Aplicacao
builder.Services.AddScoped<IEnvioService, EnvioService>();

// Auth simulada via header X-Auth-Key (mesma instancia para a interface e a classe concreta)
builder.Services.AddScoped<AuthenticatedUser>();
builder.Services.AddScoped<IAuthenticatedUser>(sp => sp.GetRequiredService<AuthenticatedUser>());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsPolicy);

app.UseMiddleware<ExcecaoMiddleware>();
app.UseMiddleware<AuthSimuladaMiddleware>();

app.MapControllers();

app.Run();
