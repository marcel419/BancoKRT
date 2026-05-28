namespace bancoKRT.api.Application.Dtos;

public sealed record AvaliarPixRequest(
    string? IdentificadorTransacao,
    string? Documento,
    string? Agencia,
    string? NumeroConta,
    decimal? Valor);
