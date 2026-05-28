namespace bancoKRT.api.Application.Exceptions;

public sealed class ExcecaoConflito : Exception
{
    public ExcecaoConflito(string mensagem)
        : base(mensagem)
    {
    }
}
