namespace bancoKRT.api.Security;

public static class PerfisAcesso
{
    public const string AnalistaFraude = "AnalistaFraude";
    public const string SistemaPix = "SistemaPix";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        AnalistaFraude,
        SistemaPix
    };
}
