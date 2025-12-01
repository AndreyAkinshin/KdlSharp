using System.Numerics;

namespace KdlSharp.Utilities;

/// <summary>
/// Provides shared number parsing utilities for hex, octal, and binary number formats.
/// </summary>
internal static class NumberParser
{
    /// <summary>
    /// Parses a raw number text that may be in hex (0x), octal (0o), or binary (0b) format.
    /// </summary>
    /// <param name="rawText">The raw number text to parse.</param>
    /// <param name="decimalValue">The parsed decimal value.</param>
    /// <returns>True if the number was in a non-decimal format and was parsed; false if decimal parsing should be used.</returns>
    public static bool TryParseNonDecimal(string rawText, out decimal decimalValue)
    {
        decimalValue = 0;

        if (rawText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var digits = rawText.Substring(2).Replace("_", "");
            var bigInt = BigInteger.Parse("0" + digits, System.Globalization.NumberStyles.HexNumber);
            decimalValue = ConvertBigIntToDecimal(bigInt, rawText);
            return true;
        }
        else if (rawText.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
        {
            var digits = rawText.Substring(2).Replace("_", "");
            var bigInt = BigInteger.Zero;
            foreach (var c in digits)
            {
                bigInt = bigInt * 8 + (c - '0');
            }
            decimalValue = ConvertBigIntToDecimal(bigInt, rawText);
            return true;
        }
        else if (rawText.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            var digits = rawText.Substring(2).Replace("_", "");
            var bigInt = BigInteger.Zero;
            foreach (var c in digits)
            {
                bigInt = bigInt * 2 + (c - '0');
            }
            decimalValue = ConvertBigIntToDecimal(bigInt, rawText);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Converts a BigInteger to decimal, throwing if the value is out of range.
    /// </summary>
    private static decimal ConvertBigIntToDecimal(BigInteger bigInt, string rawText)
    {
        if (bigInt > (BigInteger)decimal.MaxValue)
            throw new OverflowException($"Number is too large to fit in decimal: {rawText}");
        if (bigInt < (BigInteger)decimal.MinValue)
            throw new OverflowException($"Number is too small to fit in decimal: {rawText}");
        return (decimal)bigInt;
    }
}
