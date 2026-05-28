using bancoKRT.api.Application;
using bancoKRT.api.Infrastructure;
using bancoKRT.api.Middleware;
using bancoKRT.api.Security;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services
    .AddAuthentication(AutenticacaoPerfilHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AutenticacaoPerfilHandler>(
        AutenticacaoPerfilHandler.SchemeName,
        _ => { });
builder.Services.AddAuthorization();
builder.Services.AdicionarAplicacao();
builder.Services.AdicionarInfraestrutura(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<MiddlewareExcecoesApi>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
