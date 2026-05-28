namespace bancoKRT.api.Domain;

public sealed class ExcecaoValidacaoDominio : Exception
{
    public ExcecaoValidacaoDominio(string mensagem)
        : base(mensagem)
    {
    }
}
