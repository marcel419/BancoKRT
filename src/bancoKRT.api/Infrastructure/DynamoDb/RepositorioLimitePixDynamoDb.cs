using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using bancoKRT.api.Application.Contracts;
using bancoKRT.api.Application.Dtos;
using bancoKRT.api.Application.Exceptions;
using bancoKRT.api.Domain;
using Microsoft.Extensions.Options;

namespace bancoKRT.api.Infrastructure.DynamoDb;

public sealed class RepositorioLimitePixDynamoDb : IRepositorioLimitePix
{
    private const string AtributoDocumento = "Documento";
    private const string AtributoChaveConta = "ChaveConta";
    private const string AtributoAgencia = "Agencia";
    private const string AtributoNumeroConta = "NumeroConta";
    private const string AtributoLimitePix = "LimitePix";
    private const string AtributoTransacoesProcessadas = "TransacoesProcessadas";

    private readonly IAmazonDynamoDB _client;
    private readonly OpcoesDynamoDb _opcoes;

    public RepositorioLimitePixDynamoDb(IAmazonDynamoDB client, IOptions<OpcoesDynamoDb> opcoes)
    {
        _client = client;
        _opcoes = opcoes.Value;
    }

    public async Task CriarAsync(ContaLimitePix conta, CancellationToken cancellationToken)
    {
        try
        {
            await _client.PutItemAsync(new PutItemRequest
            {
                TableName = _opcoes.TableName,
                Item = ParaItem(conta),
                ConditionExpression = $"attribute_not_exists({AtributoDocumento}) AND attribute_not_exists({AtributoChaveConta})"
            }, cancellationToken);
        }
        catch (ConditionalCheckFailedException)
        {
            throw new ExcecaoConflito("Ja existe limite cadastrado para essa conta.");
        }
    }

    public async Task<ContaLimitePix?> ObterAsync(ChaveConta chave, CancellationToken cancellationToken)
    {
        var resposta = await _client.GetItemAsync(new GetItemRequest
        {
            TableName = _opcoes.TableName,
            Key = ParaChaveDynamoDb(chave),
            ConsistentRead = true
        }, cancellationToken);

        return resposta.Item.Count == 0 ? null : ParaDominio(resposta.Item);
    }

    public async Task AtualizarLimiteAsync(ChaveConta chave, decimal limitePix, CancellationToken cancellationToken)
    {
        try
        {
            await _client.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _opcoes.TableName,
                Key = ParaChaveDynamoDb(chave),
                UpdateExpression = $"SET {AtributoLimitePix} = :limit",
                ConditionExpression = $"attribute_exists({AtributoDocumento}) AND attribute_exists({AtributoChaveConta})",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":limit"] = Numero(limitePix)
                }
            }, cancellationToken);
        }
        catch (ConditionalCheckFailedException)
        {
            throw new ExcecaoNaoEncontrado("Conta nao encontrada na base de limites.");
        }
    }

    public async Task RemoverAsync(ChaveConta chave, CancellationToken cancellationToken)
    {
        try
        {
            await _client.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = _opcoes.TableName,
                Key = ParaChaveDynamoDb(chave),
                ConditionExpression = $"attribute_exists({AtributoDocumento}) AND attribute_exists({AtributoChaveConta})"
            }, cancellationToken);
        }
        catch (ConditionalCheckFailedException)
        {
            throw new ExcecaoNaoEncontrado("Conta nao encontrada na base de limites.");
        }
    }

    public async Task<ResultadoConsumoLimite> TentarConsumirLimiteAsync(
        ChaveConta chave,
        string identificadorTransacao,
        decimal valor,
        CancellationToken cancellationToken)
    {
        try
        {
            var resposta = await _client.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _opcoes.TableName,
                Key = ParaChaveDynamoDb(chave),
                UpdateExpression = $"SET {AtributoLimitePix} = {AtributoLimitePix} - :amount ADD {AtributoTransacoesProcessadas} :transactionSet",
                ConditionExpression = $"attribute_exists({AtributoDocumento}) AND {AtributoLimitePix} >= :amount AND (attribute_not_exists({AtributoTransacoesProcessadas}) OR NOT contains({AtributoTransacoesProcessadas}, :transactionId))",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":amount"] = Numero(valor),
                    [":transactionId"] = new AttributeValue(identificadorTransacao),
                    [":transactionSet"] = new AttributeValue { SS = new List<string> { identificadorTransacao } }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            }, cancellationToken);

            return new ResultadoConsumoLimite(
                StatusConsumoLimite.Aprovado,
                decimal.Parse(resposta.Attributes[AtributoLimitePix].N, System.Globalization.CultureInfo.InvariantCulture));
        }
        catch (ConditionalCheckFailedException)
        {
            var itemConta = await _client.GetItemAsync(new GetItemRequest
            {
                TableName = _opcoes.TableName,
                Key = ParaChaveDynamoDb(chave),
                ConsistentRead = true
            }, cancellationToken);

            if (itemConta.Item.Count == 0)
            {
                return new ResultadoConsumoLimite(StatusConsumoLimite.ContaNaoEncontrada, null);
            }

            var limiteRestante = decimal.Parse(itemConta.Item[AtributoLimitePix].N, System.Globalization.CultureInfo.InvariantCulture);

            if (TransacaoJaProcessada(itemConta.Item, identificadorTransacao))
            {
                return new ResultadoConsumoLimite(StatusConsumoLimite.TransacaoJaProcessada, limiteRestante);
            }

            return new ResultadoConsumoLimite(StatusConsumoLimite.LimiteInsuficiente, limiteRestante);
        }
    }

    private static Dictionary<string, AttributeValue> ParaChaveDynamoDb(ChaveConta chave)
    {
        return new Dictionary<string, AttributeValue>
        {
            [AtributoDocumento] = new AttributeValue(chave.Documento),
            [AtributoChaveConta] = new AttributeValue(chave.ChaveOrdenacao)
        };
    }

    private static Dictionary<string, AttributeValue> ParaItem(ContaLimitePix conta)
    {
        var item = ParaChaveDynamoDb(conta.Chave);
        item[AtributoAgencia] = new AttributeValue(conta.Chave.Agencia);
        item[AtributoNumeroConta] = new AttributeValue(conta.Chave.NumeroConta);
        item[AtributoLimitePix] = Numero(conta.LimitePix);
        return item;
    }

    private static ContaLimitePix ParaDominio(Dictionary<string, AttributeValue> item)
    {
        return new ContaLimitePix(
            new ChaveConta(item[AtributoDocumento].S, item[AtributoAgencia].S, item[AtributoNumeroConta].S),
            decimal.Parse(item[AtributoLimitePix].N, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static AttributeValue Numero(decimal valor)
    {
        return new AttributeValue { N = valor.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }

    private static bool TransacaoJaProcessada(Dictionary<string, AttributeValue> item, string identificadorTransacao)
    {
        return item.TryGetValue(AtributoTransacoesProcessadas, out var transacoesProcessadas)
            && transacoesProcessadas.SS.Contains(identificadorTransacao, StringComparer.OrdinalIgnoreCase);
    }
}
