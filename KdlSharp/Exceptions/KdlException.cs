namespace KdlSharp.Exceptions;

/// <summary>
/// Base exception for all KDL-related errors.
/// </summary>
public class KdlException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KdlException"/> class.
    /// </summary>
    public KdlException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="KdlException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public KdlException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="KdlException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public KdlException(string message, Exception innerException) : base(message, innerException) { }
}

