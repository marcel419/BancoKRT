namespace bancoKRT.api.Application.Exceptions;

public sealed class ExcecaoNaoEncontrado : Exception
{
    public ExcecaoNaoEncontrado(string mensagem)
        : base(mensagem)
    {
    }
}
