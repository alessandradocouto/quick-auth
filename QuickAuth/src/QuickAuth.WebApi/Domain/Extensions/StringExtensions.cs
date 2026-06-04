namespace QuickAuth.WebApi.Domain.ValueOfObjects;

public static class StringExtensions
{
    public static bool IsValidCpf(this string? cpf)
    {
        // 1. Remove caracteres não numéricos (pontos e traço)
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        // Limpa a string deixando apenas números
        var cleanCPF = new string(cpf.Where(char.IsDigit).ToArray());

        // 2. O CPF deve ter exatamente 11 dígitos
        if (cleanCPF.Length != 11)
            return false;

        // 3. Evita CPFs com todos os números iguais (ex: 111.111.111-11)
        // Eles passam no teste matemático, mas são inválidos pela Receita
        if (cleanCPF.All(c => c == cleanCPF[0]))
            return false;

        // 4. Cálculo do Primeiro Dígito Verificador
        int[] multipliers1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int sum = 0;

        for (int i = 0; i < 9; i++)
        {
            sum += (cleanCPF[i] - '0') * multipliers1[i];
        }

        int remainder = sum % 11;
        int firstDigit = remainder < 2 ? 0 : 11 - remainder;

        // Se o primeiro dígito calculado não bater com o do CPF, é inválido
        if (cleanCPF[9] - '0' != firstDigit)
            return false;

        // 5. Cálculo do Segundo Dígito Verificador
        int[] multipliers2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        sum = 0;

        for (int i = 0; i < 10; i++)
        {
            sum += (cleanCPF[i] - '0') * multipliers2[i];
        }

        remainder = sum % 11;
        int secondDigit = remainder < 2 ? 0 : 11 - remainder;

        // Retorna true se o segundo dígito também bater
        return cleanCPF[10] - '0' == secondDigit;
    }
}