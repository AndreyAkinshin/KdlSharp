namespace KdlSharp.Schema;

/// <summary>
/// Base class for all validation rules.
/// </summary>
public abstract class ValidationRule
{
    /// <summary>
    /// Rule name for error reporting.
    /// </summary>
    public abstract string RuleName { get; }

    /// <summary>
    /// Validates a value.
    /// </summary>
    public abstract bool Validate(object? value, ValidationContext context);

    /// <summary>
    /// Gets the error message for this rule.
    /// </summary>
    public abstract string GetErrorMessage(object? value);
}

/// <summary>
/// Context for validation operations.
/// </summary>
public sealed class ValidationContext
{
    private readonly List<string> pathSegments = new();
    private readonly List<ValidationError> errors = new();

    /// <summary>
    /// Current validation path.
    /// </summary>
    public string CurrentPath => string.Join("/", pathSegments);

    /// <summary>
    /// All validation errors collected so far.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => errors;

    /// <summary>
    /// Pushes a path segment.
    /// </summary>
    public void PushPath(string segment)
    {
        pathSegments.Add(segment);
    }

    /// <summary>
    /// Pops a path segment.
    /// </summary>
    public void PopPath()
    {
        if (pathSegments.Count > 0)
        {
            pathSegments.RemoveAt(pathSegments.Count - 1);
        }
    }

    /// <summary>
    /// Adds a validation error.
    /// </summary>
    public void AddError(string ruleName, string message)
    {
        errors.Add(new ValidationError(CurrentPath, message, ruleName));
    }

    /// <summary>
    /// Clears all errors.
    /// </summary>
    public void Clear()
    {
        pathSegments.Clear();
        errors.Clear();
    }
}

// Note: ValidationError is defined in ValidationResult.cs in the root namespace

