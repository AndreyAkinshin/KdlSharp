namespace KdlSharp.Exceptions;

/// <summary>
/// Exception thrown when a query is invalid.
/// </summary>
public sealed class KdlQueryException : KdlException
{
    /// <summary>
    /// Gets the invalid query string.
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KdlQueryException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="query">The invalid query string that caused the error.</param>
    public KdlQueryException(string message, string query)
        : base($"Query error: {message}\nQuery: {query}")
    {
        Query = query ?? throw new ArgumentNullException(nameof(query));
    }
}

