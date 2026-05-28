namespace bancoKRT.api.Application.Dtos;

public sealed record DecisaoPixResponse(
    bool Aprovada,
    string Motivo,
    decimal? LimiteRestante);
