namespace KdlSharp.Exceptions;

/// <summary>
/// Exception thrown when object serialization or deserialization fails.
/// </summary>
/// <remarks>
/// This exception is thrown in scenarios such as:
/// <list type="bullet">
/// <item>Attempting to serialize an unsupported type</item>
/// <item>Failing to bind properties during deserialization</item>
/// <item>Encountering circular references during serialization</item>
/// <item>Type conversion failures during deserialization</item>
/// </list>
/// </remarks>
public sealed class KdlSerializationException : KdlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KdlSerializationException"/> class.
    /// </summary>
    public KdlSerializationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="KdlSerializationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public KdlSerializationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="KdlSerializationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public KdlSerializationException(string message, Exception innerException) : base(message, innerException) { }
}

