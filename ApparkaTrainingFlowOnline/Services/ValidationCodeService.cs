using System.Security.Cryptography;
using System.Text;

namespace ApparkaTrainingFlowOnline.Services;

/// <summary>
/// Centraliza la generación, normalización y hash de códigos temporales.
/// El mismo algoritmo se utiliza al crear y al validar el código.
/// </summary>
public sealed class ValidationCodeService
{
    public string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var value = input.Trim();
        if (value.Any(c => !char.IsAsciiDigit(c) && !char.IsWhiteSpace(c) && c != '-'))
            return false;

        normalized = new string(value.Where(char.IsAsciiDigit).ToArray());
        return normalized.Length == 6;
    }

    public string Hash(string normalizedCode)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedCode));
        return Convert.ToHexString(bytes);
    }
}
