using bancoKRT.api.Application.Services;

namespace bancoKRT.api.Application;

public static class InjecaoDependencia
{
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        services.AddScoped<ServicoLimitePix>();
        return services;
    }
}
