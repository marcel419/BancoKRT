using bancoKRT.api.Application.Contracts;
using bancoKRT.api.Application.Dtos;
using bancoKRT.api.Application.Exceptions;
using bancoKRT.api.Domain;

namespace bancoKRT.api.Application.Services;

public sealed class ServicoLimitePix
{
    private const string MotivoAprovada = "Transacao PIX aprovada.";
    private const string MotivoLimiteInsuficiente = "Transacao PIX negada por limite insuficiente.";
    private const string MotivoContaNaoEncontrada = "Transacao PIX negada porque a conta nao possui limite cadastrado.";
    private const string MotivoJaProcessada = "Transacao PIX ja processada anteriormente.";

    private readonly IRepositorioLimitePix _repositorio;

    public ServicoLimitePix(IRepositorioLimitePix repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<LimiteResponse> CriarAsync(CriarLimiteRequest requisicao, CancellationToken cancellationToken)
    {
        var conta = new ContaLimitePix(
            ParaChave(requisicao.Documento, requisicao.Agencia, requisicao.NumeroConta),
            DecimalObrigatorio(requisicao.LimitePix, "limite PIX"));

        await _repositorio.CriarAsync(conta, cancellationToken);
        return ParaResponse(conta);
    }

    public async Task<LimiteResponse> ObterAsync(string documento, string agencia, string numeroConta, CancellationToken cancellationToken)
    {
        var conta = await ObterObrigatorioAsync(ParaChave(documento, agencia, numeroConta), cancellationToken);
        return ParaResponse(conta);
    }

    public async Task<LimiteResponse> AlterarAsync(
        string documento,
        string agencia,
        string numeroConta,
        AlterarLimiteRequest requisicao,
        CancellationToken cancellationToken)
    {
        var chave = ParaChave(documento, agencia, numeroConta);
        var limitePix = DecimalObrigatorio(requisicao.LimitePix, "limite PIX");
        _ = new ContaLimitePix(chave, limitePix);

        await _repositorio.AtualizarLimiteAsync(chave, limitePix, cancellationToken);
        return await ObterAsync(documento, agencia, numeroConta, cancellationToken);
    }

    public Task RemoverAsync(string documento, string agencia, string numeroConta, CancellationToken cancellationToken)
    {
        return _repositorio.RemoverAsync(ParaChave(documento, agencia, numeroConta), cancellationToken);
    }

    public async Task<DecisaoPixResponse> AvaliarAsync(AvaliarPixRequest requisicao, CancellationToken cancellationToken)
    {
        var valor = DecimalObrigatorio(requisicao.Valor, "valor da transacao PIX");

        ContaLimitePix.ValidarValorTransacao(valor);

        var identificadorTransacao = TextoObrigatorio(requisicao.IdentificadorTransacao, "identificador da transacao");
        var chave = ParaChave(requisicao.Documento, requisicao.Agencia, requisicao.NumeroConta);

        var resultado = await _repositorio.TentarConsumirLimiteAsync(chave, identificadorTransacao, valor, cancellationToken);

        return resultado.Status switch
        {
            StatusConsumoLimite.Aprovado => new DecisaoPixResponse(true, MotivoAprovada, resultado.LimiteRestante),
            StatusConsumoLimite.TransacaoJaProcessada => new DecisaoPixResponse(true, MotivoJaProcessada, resultado.LimiteRestante),
            StatusConsumoLimite.ContaNaoEncontrada => new DecisaoPixResponse(false, MotivoContaNaoEncontrada, null),
            _ => new DecisaoPixResponse(false, MotivoLimiteInsuficiente, resultado.LimiteRestante)
        };
    }

    private async Task<ContaLimitePix> ObterObrigatorioAsync(ChaveConta chave, CancellationToken cancellationToken)
    {
        var conta = await _repositorio.ObterAsync(chave, cancellationToken);

        if (conta is null)
        {
            throw new ExcecaoNaoEncontrado("Conta nao encontrada na base de limites.");
        }

        return conta;
    }

    private static ChaveConta ParaChave(string? documento, string? agencia, string? numeroConta)
    {
        return new ChaveConta(
            TextoObrigatorio(documento, "documento"),
            TextoObrigatorio(agencia, "agencia"),
            TextoObrigatorio(numeroConta, "numero da conta"));
    }

    private static LimiteResponse ParaResponse(ContaLimitePix conta)
    {
        return new LimiteResponse(conta.Chave.Documento, conta.Chave.Agencia, conta.Chave.NumeroConta, conta.LimitePix);
    }

    private static string TextoObrigatorio(string? valor, string nomeCampo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ExcecaoValidacaoDominio($"O campo {nomeCampo} e obrigatorio.");
        }

        return valor.Trim();
    }

    private static decimal DecimalObrigatorio(decimal? valor, string nomeCampo)
    {
        if (valor is null)
        {
            throw new ExcecaoValidacaoDominio($"O campo {nomeCampo} e obrigatorio.");
        }

        return valor.Value;
    }
}
