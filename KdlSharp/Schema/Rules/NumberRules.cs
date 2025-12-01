using KdlSharp.Values;

namespace KdlSharp.Schema.Rules;

/// <summary>
/// Validates number modulo constraint.
/// </summary>
public sealed class ModuloRule : ValidationRule
{
    private readonly decimal divisor;

    /// <summary>
    /// Gets the rule name ("%").
    /// </summary>
    public override string RuleName => "%";

    /// <summary>
    /// Initializes a new modulo rule.
    /// </summary>
    /// <param name="divisor">The divisor value.</param>
    /// <exception cref="ArgumentException"><paramref name="divisor"/> is zero.</exception>
    public ModuloRule(decimal divisor)
    {
        if (divisor == 0)
            throw new ArgumentException("Divisor cannot be zero", nameof(divisor));
        this.divisor = divisor;
    }

    /// <summary>
    /// Validates that the number is divisible by the divisor.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var number = GetNumberValue(value);
        if (number == null)
            return false;

        return number.Value % divisor == 0;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Value '{value}' is not divisible by {divisor}";
    }

    private static decimal? GetNumberValue(object? value)
    {
        if (value is decimal d)
            return d;
        if (value is int i)
            return i;
        if (value is long l)
            return l;
        if (value is double db)
            return (decimal)db;
        if (value is float f)
            return (decimal)f;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.Number)
            return kdlValue.AsNumber();
        return null;
    }
}

/// <summary>
/// Validates number is greater than threshold.
/// </summary>
public sealed class GreaterThanRule : ValidationRule
{
    private readonly decimal threshold;

    /// <summary>
    /// Gets the rule name (">").
    /// </summary>
    public override string RuleName => ">";

    /// <summary>
    /// Initializes a new greater-than rule.
    /// </summary>
    /// <param name="threshold">The threshold value.</param>
    public GreaterThanRule(decimal threshold)
    {
        this.threshold = threshold;
    }

    /// <summary>
    /// Validates that the number is greater than the threshold.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var number = GetNumberValue(value);
        if (number == null)
            return false;

        return number.Value > threshold;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Value '{value}' is not greater than {threshold}";
    }

    private static decimal? GetNumberValue(object? value)
    {
        if (value is decimal d)
            return d;
        if (value is int i)
            return i;
        if (value is long l)
            return l;
        if (value is double db)
            return (decimal)db;
        if (value is float f)
            return (decimal)f;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.Number)
            return kdlValue.AsNumber();
        return null;
    }
}

/// <summary>
/// Validates number is greater than or equal to threshold.
/// </summary>
public sealed class GreaterOrEqualRule : ValidationRule
{
    private readonly decimal threshold;

    /// <summary>
    /// Gets the rule name (">=").
    /// </summary>
    public override string RuleName => ">=";

    /// <summary>
    /// Initializes a new greater-than-or-equal rule.
    /// </summary>
    /// <param name="threshold">The threshold value.</param>
    public GreaterOrEqualRule(decimal threshold)
    {
        this.threshold = threshold;
    }

    /// <summary>
    /// Validates that the number is greater than or equal to the threshold.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var number = GetNumberValue(value);
        if (number == null)
            return false;

        return number.Value >= threshold;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Value '{value}' is not greater than or equal to {threshold}";
    }

    private static decimal? GetNumberValue(object? value)
    {
        if (value is decimal d)
            return d;
        if (value is int i)
            return i;
        if (value is long l)
            return l;
        if (value is double db)
            return (decimal)db;
        if (value is float f)
            return (decimal)f;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.Number)
            return kdlValue.AsNumber();
        return null;
    }
}

/// <summary>
/// Validates number is less than threshold.
/// </summary>
public sealed class LessThanRule : ValidationRule
{
    private readonly decimal threshold;

    /// <summary>
    /// Gets the rule name ("&lt;").
    /// </summary>
    public override string RuleName => "<";

    /// <summary>
    /// Initializes a new less-than rule.
    /// </summary>
    /// <param name="threshold">The threshold value.</param>
    public LessThanRule(decimal threshold)
    {
        this.threshold = threshold;
    }

    /// <summary>
    /// Validates that the number is less than the threshold.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var number = GetNumberValue(value);
        if (number == null)
            return false;

        return number.Value < threshold;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Value '{value}' is not less than {threshold}";
    }

    private static decimal? GetNumberValue(object? value)
    {
        if (value is decimal d)
            return d;
        if (value is int i)
            return i;
        if (value is long l)
            return l;
        if (value is double db)
            return (decimal)db;
        if (value is float f)
            return (decimal)f;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.Number)
            return kdlValue.AsNumber();
        return null;
    }
}

/// <summary>
/// Validates number is less than or equal to threshold.
/// </summary>
public sealed class LessOrEqualRule : ValidationRule
{
    private readonly decimal threshold;

    /// <summary>
    /// Gets the rule name ("&lt;=").
    /// </summary>
    public override string RuleName => "<=";

    /// <summary>
    /// Initializes a new less-than-or-equal rule.
    /// </summary>
    /// <param name="threshold">The threshold value.</param>
    public LessOrEqualRule(decimal threshold)
    {
        this.threshold = threshold;
    }

    /// <summary>
    /// Validates that the number is less than or equal to the threshold.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        var number = GetNumberValue(value);
        if (number == null)
            return false;

        return number.Value <= threshold;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Value '{value}' is not less than or equal to {threshold}";
    }

    private static decimal? GetNumberValue(object? value)
    {
        if (value is decimal d)
            return d;
        if (value is int i)
            return i;
        if (value is long l)
            return l;
        if (value is double db)
            return (decimal)db;
        if (value is float f)
            return (decimal)f;
        if (value is KdlValue kdlValue && kdlValue.ValueType == KdlValueType.Number)
            return kdlValue.AsNumber();
        return null;
    }
}

