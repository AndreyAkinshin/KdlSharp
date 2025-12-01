namespace KdlSharp.Serialization.Converters;

/// <summary>
/// Defines a custom converter for serializing and deserializing types to/from KDL.
/// </summary>
public interface IKdlConverter
{
    /// <summary>
    /// Determines whether this converter can handle the specified type.
    /// </summary>
    bool CanConvert(Type type);

    /// <summary>
    /// Writes a value to a KDL node.
    /// </summary>
    /// <exception cref="Exceptions.KdlSerializationException">Thrown when the value cannot be serialized to the node.</exception>
    void Write(KdlNode node, object? value, Type type, KdlSerializerOptions options, KdlSerializerContext? context);

    /// <summary>
    /// Reads a value from a KDL node.
    /// </summary>
    /// <exception cref="Exceptions.KdlSerializationException">Thrown when the node cannot be deserialized to the target type.</exception>
    object? Read(KdlNode node, Type type, KdlSerializerOptions options, KdlSerializerContext? context);
}

