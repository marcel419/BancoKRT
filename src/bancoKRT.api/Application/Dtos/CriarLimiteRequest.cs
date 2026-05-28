namespace bancoKRT.api.Application.Dtos;

public sealed record CriarLimiteRequest(
    string? Documento,
    string? Agencia,
    string? NumeroConta,
    decimal? LimitePix);
