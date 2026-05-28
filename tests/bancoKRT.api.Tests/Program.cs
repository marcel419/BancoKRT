using bancoKRT.api.Application.Dtos;
using bancoKRT.api.Application.Exceptions;
using bancoKRT.api.Application.Services;
using bancoKRT.api.Domain;
using bancoKRT.api.Infrastructure.InMemory;

var tests = new List<(string Name, Func<Task> Action)>
{
    ("cadastra e consulta limite PIX", CadastraEConsultaLimite),
    ("aprova PIX e desconta limite", AprovaPixEDescontaLimite),
    ("nega PIX sem descontar limite", NegaPixSemDescontarLimite),
    ("nao desconta limite em transacao repetida", NaoDescontaTransacaoRepetida),
    ("altera limite PIX", AlteraLimite),
    ("valida campos obrigatorios", ValidaCamposObrigatorios),
    ("falha ao remover conta inexistente", FalhaAoRemoverContaInexistente)
};

var failures = 0;

foreach (var test in tests)
{
    try
    {
        await test.Action();
        Console.WriteLine($"PASS - {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL - {test.Name}: {exception.Message}");
    }
}

Environment.ExitCode = failures;

static async Task CadastraEConsultaLimite()
{
    var servico = NovoServico();

    await servico.CriarAsync(NovaRequisicaoCriarLimite(500m), CancellationToken.None);
    var conta = await servico.ObterAsync("12345678900", "0001", "12345-6", CancellationToken.None);

    Igual(500m, conta.LimitePix);
    Igual("12345678900", conta.Documento);
    Igual("0001", conta.Agencia);
    Igual("123456", conta.NumeroConta);
}

static async Task AprovaPixEDescontaLimite()
{
    var servico = NovoServico();

    await servico.CriarAsync(NovaRequisicaoCriarLimite(500m), CancellationToken.None);
    var decisao = await servico.AvaliarAsync(NovaRequisicaoPix(125m), CancellationToken.None);

    Verdadeiro(decisao.Aprovada, "A transacao deveria ser aprovada.");
    Igual(375m, decisao.LimiteRestante!.Value);
}

static async Task NegaPixSemDescontarLimite()
{
    var servico = NovoServico();

    await servico.CriarAsync(NovaRequisicaoCriarLimite(100m), CancellationToken.None);
    var decisao = await servico.AvaliarAsync(NovaRequisicaoPix(150m), CancellationToken.None);
    var conta = await servico.ObterAsync("12345678900", "0001", "12345-6", CancellationToken.None);

    Falso(decisao.Aprovada, "A transacao deveria ser negada.");
    Igual(100m, conta.LimitePix);
}

static async Task NaoDescontaTransacaoRepetida()
{
    var servico = NovoServico();

    await servico.CriarAsync(NovaRequisicaoCriarLimite(500m), CancellationToken.None);

    var primeiraDecisao = await servico.AvaliarAsync(NovaRequisicaoPixComId("pix-duplicado", 125m), CancellationToken.None);
    var segundaDecisao = await servico.AvaliarAsync(NovaRequisicaoPixComId("pix-duplicado", 125m), CancellationToken.None);
    var conta = await servico.ObterAsync("12345678900", "0001", "12345-6", CancellationToken.None);

    Verdadeiro(primeiraDecisao.Aprovada, "A primeira transacao deveria ser aprovada.");
    Verdadeiro(segundaDecisao.Aprovada, "A repeticao idempotente deveria retornar aprovada.");
    Igual(375m, conta.LimitePix);
}

static async Task AlteraLimite()
{
    var servico = NovoServico();

    await servico.CriarAsync(NovaRequisicaoCriarLimite(100m), CancellationToken.None);
    var conta = await servico.AlterarAsync(
        "12345678900",
        "0001",
        "12345-6",
        new AlterarLimiteRequest(300m),
        CancellationToken.None);

    Igual(300m, conta.LimitePix);
}

static async Task ValidaCamposObrigatorios()
{
    var servico = NovoServico();

    await LancaExcecaoAsync<ExcecaoValidacaoDominio>(() =>
        servico.CriarAsync(new CriarLimiteRequest(null, "0001", "12345-6", 100m), CancellationToken.None));

    await LancaExcecaoAsync<ExcecaoValidacaoDominio>(() =>
        servico.AvaliarAsync(new AvaliarPixRequest(null, "12345678900", "0001", "12345-6", 10m), CancellationToken.None));
}

static async Task FalhaAoRemoverContaInexistente()
{
    var servico = NovoServico();

    await LancaExcecaoAsync<ExcecaoNaoEncontrado>(() =>
        servico.RemoverAsync("12345678900", "0001", "12345-6", CancellationToken.None));
}

static ServicoLimitePix NovoServico() => new(new RepositorioLimitePixEmMemoria());

static CriarLimiteRequest NovaRequisicaoCriarLimite(decimal limitePix)
{
    return new CriarLimiteRequest("123.456.789-00", "0001", "12345-6", limitePix);
}

static AvaliarPixRequest NovaRequisicaoPix(decimal valor)
{
    return NovaRequisicaoPixComId(Guid.NewGuid().ToString("N"), valor);
}

static AvaliarPixRequest NovaRequisicaoPixComId(string identificadorTransacao, decimal valor)
{
    return new AvaliarPixRequest(identificadorTransacao, "12345678900", "0001", "12345-6", valor);
}

static void Verdadeiro(bool condicao, string mensagem)
{
    if (!condicao)
    {
        throw new InvalidOperationException(mensagem);
    }
}

static void Falso(bool condicao, string mensagem)
{
    if (condicao)
    {
        throw new InvalidOperationException(mensagem);
    }
}

static void Igual<T>(T esperado, T obtido)
{
    if (!EqualityComparer<T>.Default.Equals(esperado, obtido))
    {
        throw new InvalidOperationException($"Esperado: {esperado}. Obtido: {obtido}.");
    }
}

static async Task LancaExcecaoAsync<TException>(Func<Task> acao)
    where TException : Exception
{
    try
    {
        await acao();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Esperava excecao do tipo {typeof(TException).Name}.");
}
