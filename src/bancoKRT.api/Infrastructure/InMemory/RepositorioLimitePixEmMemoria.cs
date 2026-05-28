using System.Collections.Concurrent;
using bancoKRT.api.Application.Contracts;
using bancoKRT.api.Application.Dtos;
using bancoKRT.api.Application.Exceptions;
using bancoKRT.api.Domain;

namespace bancoKRT.api.Infrastructure.InMemory;

public sealed class RepositorioLimitePixEmMemoria : IRepositorioLimitePix
{
    private readonly ConcurrentDictionary<string, ContaLimitePix> _contas = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _transacoesProcessadas = new();

    public Task CriarAsync(ContaLimitePix conta, CancellationToken cancellationToken)
    {
        var chaveArmazenamento = ChaveArmazenamento(conta.Chave);

        if (!_contas.TryAdd(chaveArmazenamento, Copiar(conta)))
        {
            throw new ExcecaoConflito("Ja existe limite cadastrado para essa conta.");
        }

        _transacoesProcessadas.TryAdd(chaveArmazenamento, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }

    public Task<ContaLimitePix?> ObterAsync(ChaveConta chave, CancellationToken cancellationToken)
    {
        return Task.FromResult(_contas.TryGetValue(ChaveArmazenamento(chave), out var conta) ? Copiar(conta) : null);
    }

    public Task AtualizarLimiteAsync(ChaveConta chave, decimal limitePix, CancellationToken cancellationToken)
    {
        if (!_contas.TryGetValue(ChaveArmazenamento(chave), out var conta))
        {
            throw new ExcecaoNaoEncontrado("Conta nao encontrada na base de limites.");
        }

        conta.AlterarLimite(limitePix);
        return Task.CompletedTask;
    }

    public Task RemoverAsync(ChaveConta chave, CancellationToken cancellationToken)
    {
        var chaveArmazenamento = ChaveArmazenamento(chave);

        if (!_contas.TryRemove(chaveArmazenamento, out _))
        {
            throw new ExcecaoNaoEncontrado("Conta nao encontrada na base de limites.");
        }

        _transacoesProcessadas.TryRemove(chaveArmazenamento, out _);
        return Task.CompletedTask;
    }

    public Task<ResultadoConsumoLimite> TentarConsumirLimiteAsync(
        ChaveConta chave,
        string identificadorTransacao,
        decimal valor,
        CancellationToken cancellationToken)
    {
        if (!_contas.TryGetValue(ChaveArmazenamento(chave), out var conta))
        {
            return Task.FromResult(new ResultadoConsumoLimite(StatusConsumoLimite.ContaNaoEncontrada, null));
        }

        lock (conta)
        {
            var transacoes = _transacoesProcessadas.GetOrAdd(
                ChaveArmazenamento(chave),
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            if (transacoes.Contains(identificadorTransacao))
            {
                return Task.FromResult(new ResultadoConsumoLimite(StatusConsumoLimite.TransacaoJaProcessada, conta.LimitePix));
            }

            if (!conta.PodeAprovar(valor))
            {
                return Task.FromResult(new ResultadoConsumoLimite(StatusConsumoLimite.LimiteInsuficiente, conta.LimitePix));
            }

            conta.Consumir(valor);
            transacoes.Add(identificadorTransacao);

            return Task.FromResult(new ResultadoConsumoLimite(StatusConsumoLimite.Aprovado, conta.LimitePix));
        }
    }

    private static ContaLimitePix Copiar(ContaLimitePix conta)
    {
        return new ContaLimitePix(
            new ChaveConta(conta.Chave.Documento, conta.Chave.Agencia, conta.Chave.NumeroConta),
            conta.LimitePix);
    }

    private static string ChaveArmazenamento(ChaveConta chave) => $"{chave.Documento}|{chave.ChaveOrdenacao}";
}
