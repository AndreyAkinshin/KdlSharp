namespace KdlSharp.Exceptions;

/// <summary>
/// Exception thrown when schema validation fails.
/// </summary>
public sealed class KdlValidationException : KdlException
{
    /// <summary>
    /// Gets the validation result containing all errors.
    /// </summary>
    public ValidationResult ValidationResult { get; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    /// <remarks>
    /// This is a convenience property equivalent to <c>ValidationResult.Errors</c>.
    /// </remarks>
    public IReadOnlyList<ValidationError> Errors => ValidationResult.Errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="KdlValidationException"/> class.
    /// </summary>
    /// <param name="validationResult">The validation result containing all errors.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validationResult"/> is null.</exception>
    public KdlValidationException(ValidationResult validationResult)
        : base($"Validation failed with {validationResult.Errors.Count} error(s)")
    {
        ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    }
}

