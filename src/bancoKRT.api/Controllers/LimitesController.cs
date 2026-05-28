using bancoKRT.api.Application.Dtos;
using bancoKRT.api.Application.Services;
using bancoKRT.api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bancoKRT.api.Controllers;

[ApiController]
[Authorize(Roles = PerfisAcesso.AnalistaFraude)]
[Route("api/limites")]
public sealed class LimitesController : ControllerBase
{
    private readonly ServicoLimitePix _servico;

    public LimitesController(ServicoLimitePix servico)
    {
        _servico = servico;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(CriarLimiteRequest requisicao, CancellationToken cancellationToken)
    {
        var resposta = await _servico.CriarAsync(requisicao, cancellationToken);

        return CreatedAtAction(
            nameof(Obter),
            new { documento = resposta.Documento, agencia = resposta.Agencia, numeroConta = resposta.NumeroConta },
            resposta);
    }

    [HttpGet("{documento}/{agencia}/{numeroConta}")]
    public async Task<IActionResult> Obter(string documento, string agencia, string numeroConta, CancellationToken cancellationToken)
    {
        return Ok(await _servico.ObterAsync(documento, agencia, numeroConta, cancellationToken));
    }

    [HttpPatch("{documento}/{agencia}/{numeroConta}")]
    public async Task<IActionResult> Alterar(
        string documento,
        string agencia,
        string numeroConta,
        AlterarLimiteRequest requisicao,
        CancellationToken cancellationToken)
    {
        return Ok(await _servico.AlterarAsync(documento, agencia, numeroConta, requisicao, cancellationToken));
    }

    [HttpDelete("{documento}/{agencia}/{numeroConta}")]
    public async Task<IActionResult> Remover(string documento, string agencia, string numeroConta, CancellationToken cancellationToken)
    {
        await _servico.RemoverAsync(documento, agencia, numeroConta, cancellationToken);
        return NoContent();
    }
}
