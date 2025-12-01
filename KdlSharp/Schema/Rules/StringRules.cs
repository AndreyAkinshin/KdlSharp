using System.Text.RegularExpressions;
using KdlSharp.Values;

namespace KdlSharp.Schema.Rules;

/// <summary>
/// Validates string against regex pattern.
/// </summary>
public sealed class PatternRule : ValidationRule
{
    private readonly Regex pattern;
    private readonly string patternString;

    /// <summary>
    /// Gets the rule name ("pattern").
    /// </summary>
    public override string RuleName => "pattern";

    /// <summary>
    /// Initializes a new pattern rule.
    /// </summary>
    /// <param name="pattern">The regex pattern string.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    public PatternRule(string pattern)
    {
        patternString = pattern ?? throw new ArgumentNullException(nameof(pattern));
        this.pattern = new Regex(pattern, RegexOptions.Compiled);
    }

    /// <summary>
    /// Validates that the string matches the regex pattern.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var str = GetStringValue(value);
        if (str == null)
            return false;

        return pattern.IsMatch(str);
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Value '{value}' does not match pattern '{patternString}'";
    }

    private static string? GetStringValue(object? value)
    {
        if (value is string s)
            return s;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.String)
            return kdlValue.AsString();
        return null;
    }
}

/// <summary>
/// Validates minimum string length.
/// </summary>
public sealed class MinLengthRule : ValidationRule
{
    private readonly int minLength;

    /// <summary>
    /// Gets the rule name ("min-length").
    /// </summary>
    public override string RuleName => "min-length";

    /// <summary>
    /// Initializes a new minimum length rule.
    /// </summary>
    /// <param name="minLength">The minimum length requirement.</param>
    /// <exception cref="ArgumentException"><paramref name="minLength"/> is negative.</exception>
    public MinLengthRule(int minLength)
    {
        if (minLength < 0)
            throw new ArgumentOutOfRangeException(nameof(minLength), "Min length must be non-negative");
        this.minLength = minLength;
    }

    /// <summary>
    /// Validates that the string length is at least the minimum length.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var str = GetStringValue(value);
        if (str == null)
            return false;

        return str.Length >= minLength;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        var str = GetStringValue(value);
        return $"String length {str?.Length ?? 0} is less than minimum {minLength}";
    }

    private static string? GetStringValue(object? value)
    {
        if (value is string s)
            return s;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.String)
            return kdlValue.AsString();
        return null;
    }
}

/// <summary>
/// Validates maximum string length.
/// </summary>
public sealed class MaxLengthRule : ValidationRule
{
    private readonly int maxLength;

    /// <summary>
    /// Gets the rule name ("max-length").
    /// </summary>
    public override string RuleName => "max-length";

    /// <summary>
    /// Initializes a new maximum length rule.
    /// </summary>
    /// <param name="maxLength">The maximum length requirement.</param>
    /// <exception cref="ArgumentException"><paramref name="maxLength"/> is negative.</exception>
    public MaxLengthRule(int maxLength)
    {
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be non-negative");
        this.maxLength = maxLength;
    }

    /// <summary>
    /// Validates that the string length does not exceed the maximum length.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var str = GetStringValue(value);
        if (str == null)
            return false;

        return str.Length <= maxLength;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        var str = GetStringValue(value);
        return $"String length {str?.Length ?? 0} exceeds maximum {maxLength}";
    }

    private static string? GetStringValue(object? value)
    {
        if (value is string s)
            return s;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.String)
            return kdlValue.AsString();
        return null;
    }
}

/// <summary>
/// Validates string format (email, url, uuid, date-time, etc.).
/// </summary>
public sealed class FormatRule : ValidationRule
{
    private readonly string format;
    private readonly Func<string, bool> validator;

    /// <summary>
    /// Gets the rule name ("format").
    /// </summary>
    public override string RuleName => "format";

    /// <summary>
    /// Initializes a new format rule.
    /// </summary>
    /// <param name="format">The format name (e.g., "email", "url", "uuid").</param>
    /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
    public FormatRule(string format)
    {
        this.format = format ?? throw new ArgumentNullException(nameof(format));
        validator = GetValidator(format);
    }

    /// <summary>
    /// Validates that the string conforms to the specified format.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var str = GetStringValue(value);
        if (str == null)
            return false;

        return validator(str);
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Value '{value}' is not a valid {format}";
    }

    private static string? GetStringValue(object? value)
    {
        if (value is string s)
            return s;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.String)
            return kdlValue.AsString();
        return null;
    }

    private static Func<string, bool> GetValidator(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "email" or "idn-email" => IsValidEmail,
            "url" or "uri" or "url-reference" or "iri" or "irl-reference" => IsValidUrl,
            "uuid" => IsValidUuid,
            "date-time" => IsValidDateTime,
            "date" => IsValidDate,
            "time" => IsValidTime,
            "hostname" or "idn-hostname" => IsValidHostname,
            "ipv4" => IsValidIpv4,
            "ipv6" => IsValidIpv6,
            "regex" => IsValidRegex,
            _ => _ => true // Unknown format, pass validation
        };
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == value;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private static bool IsValidUuid(string value)
    {
        return Guid.TryParse(value, out _);
    }

    private static bool IsValidDateTime(string value)
    {
        return DateTime.TryParse(value, out _);
    }

    private static bool IsValidDate(string value)
    {
        // For .NET Standard 2.1 compatibility
        return DateTime.TryParse(value, out _);
    }

    private static bool IsValidTime(string value)
    {
        // For .NET Standard 2.1 compatibility
        return TimeSpan.TryParse(value, out _);
    }

    private static bool IsValidHostname(string value)
    {
        return Uri.CheckHostName(value) != UriHostNameType.Unknown;
    }

    private static bool IsValidIpv4(string value)
    {
        return System.Net.IPAddress.TryParse(value, out var addr) && addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }

    private static bool IsValidIpv6(string value)
    {
        return System.Net.IPAddress.TryParse(value, out var addr) && addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
    }

    private static bool IsValidRegex(string value)
    {
        try
        {
            _ = new Regex(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

