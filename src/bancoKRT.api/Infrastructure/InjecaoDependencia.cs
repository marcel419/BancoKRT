using Amazon;
using Amazon.DynamoDBv2;
using bancoKRT.api.Application.Contracts;
using bancoKRT.api.Infrastructure.DynamoDb;
using bancoKRT.api.Infrastructure.InMemory;

namespace bancoKRT.api.Infrastructure;

public static class InjecaoDependencia
{
    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("DynamoDb");
        services.Configure<OpcoesDynamoDb>(section);

        var options = section.Get<OpcoesDynamoDb>() ?? new OpcoesDynamoDb();

        if (options.UseInMemory)
        {
            services.AddSingleton<IRepositorioLimitePix, RepositorioLimitePixEmMemoria>();
            return services;
        }

        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                config.ServiceURL = options.ServiceUrl;
            }

            return new AmazonDynamoDBClient(config);
        });

        services.AddScoped<IRepositorioLimitePix, RepositorioLimitePixDynamoDb>();
        return services;
    }
}
