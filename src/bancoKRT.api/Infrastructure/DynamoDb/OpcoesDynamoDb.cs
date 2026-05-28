namespace bancoKRT.api.Infrastructure.DynamoDb;

public sealed class OpcoesDynamoDb
{
    public string TableName { get; set; } = "ContaLimitePix";
    public string Region { get; set; } = "sa-east-1";
    public string? ServiceUrl { get; set; }
    public bool UseInMemory { get; set; } = false;
}
