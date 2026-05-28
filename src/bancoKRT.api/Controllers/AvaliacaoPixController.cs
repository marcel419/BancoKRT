using bancoKRT.api.Application.Dtos;
using bancoKRT.api.Application.Services;
using bancoKRT.api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bancoKRT.api.Controllers;

[ApiController]
[Authorize(Roles = PerfisAcesso.SistemaPix)]
[Route("api/pix")]
public sealed class AvaliacaoPixController : ControllerBase
{
    private readonly ServicoLimitePix _servico;

    public AvaliacaoPixController(ServicoLimitePix servico)
    {
        _servico = servico;
    }

    [HttpPost("avaliar")]
    public async Task<IActionResult> Avaliar(AvaliarPixRequest requisicao, CancellationToken cancellationToken)
    {
        return Ok(await _servico.AvaliarAsync(requisicao, cancellationToken));
    }
}
