# bancoKRT.api

API .NET 8 para gestao de limite PIX do Banco KRT.

## O que foi implementado

- Cadastro de limite PIX por documento, agencia e conta.
- Consulta de limite cadastrado.
- Alteracao de limite PIX.
- Remocao do registro de limite.
- Avaliacao de transacao PIX com aprovacao ou negacao.
- Consumo de limite somente quando a transacao for aprovada.
- Idempotencia por `identificadorTransacao`, evitando desconto duplicado em caso de reenvio.
- Validacao de documento, agencia, conta, valores monetarios e campos obrigatorios.
- Controle de acesso por perfil:
  - `AnalistaFraude` para manutencao dos limites.
  - `SistemaPix` para avaliacao de transacoes PIX.
- MVC com controladores.
- Organizacao em camadas: Dominio, Aplicacao, Infraestrutura e Controladores.
- Repositorio DynamoDB e repositorio em memoria para desenvolvimento local.
- Testes dos principais fluxos de negocio.

## Endpoints

Todos os endpoints exigem o cabecalho `X-Perfil`.

### Perfil AnalistaFraude

`POST /api/limites`

Cabecalho:

```text
X-Perfil: AnalistaFraude
```

```json
{
  "documento": "123.456.789-00",
  "agencia": "0001",
  "numeroConta": "12345-6",
  "limitePix": 1000
}
```

`GET /api/limites/{documento}/{agencia}/{numeroConta}`

`PATCH /api/limites/{documento}/{agencia}/{numeroConta}`

```json
{
  "limitePix": 1500
}
```

`DELETE /api/limites/{documento}/{agencia}/{numeroConta}`

### Perfil SistemaPix

`POST /api/pix/avaliar`

Cabecalho:

```text
X-Perfil: SistemaPix
```

```json
{
  "identificadorTransacao": "pix-20260528-0001",
  "documento": "123.456.789-00",
  "agencia": "0001",
  "numeroConta": "12345-6",
  "valor": 250
}
```

Resposta aprovada:

```json
{
  "aprovada": true,
  "motivo": "Transacao PIX aprovada.",
  "limiteRestante": 750
}
```

Se a mesma transacao for reenviada com o mesmo `identificadorTransacao`, a API retorna aprovada e nao consome limite novamente:

```json
{
  "aprovada": true,
  "motivo": "Transacao PIX ja processada anteriormente.",
  "limiteRestante": 750
}
```

## DynamoDB

Por padrao o projeto esta configurado para usar a tabela DynamoDB `ContaLimitePix`:

```json
"DynamoDb": {
  "TableName": "ContaLimitePix",
  "Region": "sa-east-1",
  "ServiceUrl": null,
  "UseInMemory": false
}
```

Para testar sem AWS, altere `UseInMemory` para `true` em `src/bancoKRT.api/appsettings.json`.

Para usar DynamoDB, configure as credenciais AWS no ambiente.

Modelo da tabela: `infrastructure/dynamodb-table.json`.

Chaves da tabela:

- chave de particao: `Documento`
- chave de ordenacao: `ChaveConta`, no formato `agencia#conta`

Com essa estrutura, o mesmo documento pode possuir mais de uma conta. O item tambem armazena `Agencia`, `NumeroConta`, `LimitePix` e `TransacoesProcessadas`.

O consumo de limite no DynamoDB usa atualizacao condicional com `LimitePix >= :amount` e controle de transacoes ja processadas em `TransacoesProcessadas`, evitando desconto quando o limite for insuficiente ou quando a mesma transacao for reenviada.

## Como validar

```powershell
dotnet restore
dotnet build
dotnet run --project .\tests\bancoKRT.api.Tests\bancoKRT.api.Tests.csproj
dotnet run --project .\src\bancoKRT.api\bancoKRT.api.csproj --urls http://localhost:5013
```

Exemplo de chamada local:

```powershell
curl -X POST http://localhost:5013/api/limites `
  -H "Content-Type: application/json" `
  -H "X-Perfil: AnalistaFraude" `
  -d "{\"documento\":\"123.456.789-00\",\"agencia\":\"0001\",\"numeroConta\":\"12345-6\",\"limitePix\":1000}"
```
