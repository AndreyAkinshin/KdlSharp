namespace KdlSharp.Schema.Rules;

/// <summary>
/// Validates minimum count.
/// </summary>
public sealed class MinRule : ValidationRule
{
    private readonly int minCount;

    /// <summary>
    /// Gets the rule name ("min").
    /// </summary>
    public override string RuleName => "min";

    /// <summary>
    /// Initializes a new minimum count rule.
    /// </summary>
    /// <param name="minCount">The minimum count requirement.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minCount"/> is negative.</exception>
    public MinRule(int minCount)
    {
        if (minCount < 0)
            throw new ArgumentOutOfRangeException(nameof(minCount), "Min count must be non-negative");
        this.minCount = minCount;
    }

    /// <summary>
    /// Gets the minimum count requirement.
    /// </summary>
    public int MinCount => minCount;

    /// <summary>
    /// Always passes validation (handled at collection level).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>Always true.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        // Min/max validation is handled at the collection level by validator
        return true;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Count is less than minimum {minCount}";
    }
}

/// <summary>
/// Validates maximum count.
/// </summary>
public sealed class MaxRule : ValidationRule
{
    private readonly int maxCount;

    /// <summary>
    /// Gets the rule name ("max").
    /// </summary>
    public override string RuleName => "max";

    /// <summary>
    /// Initializes a new maximum count rule.
    /// </summary>
    /// <param name="maxCount">The maximum count requirement.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount"/> is negative.</exception>
    public MaxRule(int maxCount)
    {
        if (maxCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maxCount), "Max count must be non-negative");
        this.maxCount = maxCount;
    }

    /// <summary>
    /// Gets the maximum count requirement.
    /// </summary>
    public int MaxCount => maxCount;

    /// <summary>
    /// Always passes validation (handled at collection level).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>Always true.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        // Min/max validation is handled at the collection level by validator
        return true;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Count exceeds maximum {maxCount}";
    }
}

