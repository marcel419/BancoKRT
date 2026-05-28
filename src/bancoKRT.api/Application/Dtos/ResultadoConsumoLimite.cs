namespace bancoKRT.api.Application.Dtos;

public enum StatusConsumoLimite
{
    Aprovado,
    LimiteInsuficiente,
    ContaNaoEncontrada,
    TransacaoJaProcessada
}

public sealed record ResultadoConsumoLimite(
    StatusConsumoLimite Status,
    decimal? LimiteRestante);
