using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace bancoKRT.api.Security;

public sealed class AutenticacaoPerfilHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Perfil";
    private const string PerfilHeader = "X-Perfil";

    public AutenticacaoPerfilHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var perfil = Request.Headers[PerfilHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(perfil))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!PerfisAcesso.Todos.Contains(perfil))
        {
            return Task.FromResult(AuthenticateResult.Fail("Perfil de acesso invalido."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "AcessoPerfil"),
            new Claim(ClaimTypes.Role, perfil)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
