namespace bancoKRT.api.Application.Dtos;

public sealed record LimiteResponse(
    string Documento,
    string Agencia,
    string NumeroConta,
    decimal LimitePix);
