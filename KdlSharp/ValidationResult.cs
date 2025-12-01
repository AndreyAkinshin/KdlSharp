namespace KdlSharp;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets whether the validation succeeded (no errors).
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    private ValidationResult()
    {
        Errors = Array.Empty<ValidationError>();
    }

    private ValidationResult(ValidationError[] errors)
    {
        Errors = errors;
    }

    /// <summary>
    /// Initializes a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new ValidationResult();

    /// <summary>
    /// Initializes a failed validation result with errors.
    /// </summary>
    public static ValidationResult Failure(params ValidationError[] errors) => new ValidationResult(errors);

    /// <summary>
    /// Gets errors for a specific path.
    /// </summary>
    public IEnumerable<ValidationError> GetErrorsForPath(string path)
    {
        return Errors.Where(e => e.Path == path);
    }

    /// <summary>
    /// Gets errors by rule name.
    /// </summary>
    public IEnumerable<ValidationError> GetErrorsByRule(string ruleName)
    {
        return Errors.Where(e => e.RuleName == ruleName);
    }

    /// <summary>
    /// Formats all errors as a single string.
    /// </summary>
    public string FormatErrors()
    {
        if (IsValid)
            return "Validation succeeded";

        return string.Join("\n", Errors.Select(e => $"[{e.RuleName}] {e.Path}: {e.Message}"));
    }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Gets the path to the invalid element (e.g., "node1/child2/prop").
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the validation rule that failed.
    /// </summary>
    public string RuleName { get; }

    /// <summary>
    /// Initializes a new validation error.
    /// </summary>
    /// <param name="path">The path to the invalid element.</param>
    /// <param name="message">The error message.</param>
    /// <param name="ruleName">The name of the rule that failed.</param>
    /// <exception cref="ArgumentNullException">Any parameter is null.</exception>
    public ValidationError(string path, string message, string ruleName)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
    }
}

