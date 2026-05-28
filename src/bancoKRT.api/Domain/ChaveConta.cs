namespace bancoKRT.api.Domain;

public sealed record ChaveConta
{
    public ChaveConta(string documento, string agencia, string numeroConta)
    {
        Documento = NormalizarDocumento(documento);
        Agencia = NormalizarCampoNumerico(agencia, "agencia");
        NumeroConta = NormalizarNumeroConta(numeroConta);
    }

    public string Documento { get; }
    public string Agencia { get; }
    public string NumeroConta { get; }
    public string ChaveOrdenacao => $"{Agencia}#{NumeroConta}";

    private static string Obrigatorio(string valor, string nomeCampo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ExcecaoValidacaoDominio($"O campo {nomeCampo} e obrigatorio.");
        }

        return valor.Trim();
    }

    private static string NormalizarDocumento(string valor)
    {
        var documentoOriginal = Obrigatorio(valor, "documento");

        if (documentoOriginal.Any(caractere => !char.IsDigit(caractere) && caractere is not '.' and not '-' && !char.IsWhiteSpace(caractere)))
        {
            throw new ExcecaoValidacaoDominio("O documento deve conter apenas numeros ou mascara de CPF.");
        }

        var documento = ApenasDigitos(documentoOriginal);

        if (documento.Length != 11)
        {
            throw new ExcecaoValidacaoDominio("O documento deve conter 11 digitos.");
        }

        return documento;
    }

    private static string NormalizarCampoNumerico(string valor, string nomeCampo)
    {
        var normalizado = Obrigatorio(valor, nomeCampo);

        if (normalizado.Any(caractere => !char.IsDigit(caractere)))
        {
            throw new ExcecaoValidacaoDominio($"O campo {nomeCampo} deve conter apenas numeros.");
        }

        return normalizado;
    }

    private static string NormalizarNumeroConta(string valor)
    {
        var numeroContaOriginal = Obrigatorio(valor, "numero da conta");

        if (numeroContaOriginal.Any(caractere => !char.IsDigit(caractere) && caractere is not '-' && !char.IsWhiteSpace(caractere)))
        {
            throw new ExcecaoValidacaoDominio("O numero da conta deve conter apenas numeros ou hifen.");
        }

        var numeroConta = ApenasDigitos(numeroContaOriginal);

        if (numeroConta.Length == 0)
        {
            throw new ExcecaoValidacaoDominio("O numero da conta deve conter ao menos um digito.");
        }

        return numeroConta;
    }

    private static string ApenasDigitos(string valor)
    {
        return new string(valor.Where(char.IsDigit).ToArray());
    }
}
