namespace KdlSharp.Exceptions;

/// <summary>
/// Exception thrown when KDL parsing fails.
/// </summary>
public sealed class KdlParseException : KdlException
{
    /// <summary>
    /// Gets the line number where the error occurred (1-indexed).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column number where the error occurred (1-indexed).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the source text surrounding the error (if available).
    /// </summary>
    public string? SourceContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KdlParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred (1-indexed).</param>
    /// <param name="column">The column number where the error occurred (1-indexed).</param>
    /// <param name="sourceContext">Optional source text surrounding the error.</param>
    public KdlParseException(string message, int line, int column, string? sourceContext = null)
        : base($"Parse error at line {line}, column {column}: {message}")
    {
        Line = line;
        Column = column;
        SourceContext = sourceContext;
    }
}

