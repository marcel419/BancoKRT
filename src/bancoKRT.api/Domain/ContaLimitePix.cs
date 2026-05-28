namespace bancoKRT.api.Domain;

public sealed class ContaLimitePix
{
    public ContaLimitePix(ChaveConta chave, decimal limitePix)
    {
        Chave = chave;
        AlterarLimite(limitePix);
    }

    public ChaveConta Chave { get; }
    public decimal LimitePix { get; private set; }

    public void AlterarLimite(decimal limitePix)
    {
        if (limitePix < 0)
        {
            throw new ExcecaoValidacaoDominio("O limite PIX deve ser maior ou igual a zero.");
        }

        GarantirDuasCasasDecimais(limitePix, "limite PIX");
        LimitePix = limitePix;
    }

    public bool PodeAprovar(decimal valor)
    {
        if (valor <= 0)
        {
            return false;
        }

        ValidarValorTransacao(valor);
        return LimitePix >= valor;
    }

    public static void ValidarValorTransacao(decimal valor)
    {
        if (valor <= 0)
        {
            throw new ExcecaoValidacaoDominio("O valor da transacao PIX deve ser maior que zero.");
        }

        GarantirDuasCasasDecimais(valor, "valor da transacao PIX");
    }

    public void Consumir(decimal valor)
    {
        ValidarValorTransacao(valor);

        if (!PodeAprovar(valor))
        {
            throw new ExcecaoValidacaoDominio("Limite insuficiente para realizar a transacao PIX.");
        }

        LimitePix -= valor;
    }

    private static void GarantirDuasCasasDecimais(decimal valor, string nomeCampo)
    {
        if (decimal.Round(valor, 2) != valor)
        {
            throw new ExcecaoValidacaoDominio($"O campo {nomeCampo} deve ter no maximo duas casas decimais.");
        }
    }
}
