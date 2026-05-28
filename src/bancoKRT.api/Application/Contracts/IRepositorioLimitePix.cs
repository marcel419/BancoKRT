using bancoKRT.api.Domain;
using bancoKRT.api.Application.Dtos;

namespace bancoKRT.api.Application.Contracts;

public interface IRepositorioLimitePix
{
    Task CriarAsync(ContaLimitePix conta, CancellationToken cancellationToken);
    Task<ContaLimitePix?> ObterAsync(ChaveConta chave, CancellationToken cancellationToken);
    Task AtualizarLimiteAsync(ChaveConta chave, decimal limitePix, CancellationToken cancellationToken);
    Task RemoverAsync(ChaveConta chave, CancellationToken cancellationToken);
    Task<ResultadoConsumoLimite> TentarConsumirLimiteAsync(
        ChaveConta chave,
        string identificadorTransacao,
        decimal valor,
        CancellationToken cancellationToken);
}
