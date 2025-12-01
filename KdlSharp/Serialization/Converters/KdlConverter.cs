namespace KdlSharp.Serialization.Converters;

/// <summary>
/// Base class for type-specific KDL converters.
/// </summary>
/// <typeparam name="T">The type this converter handles.</typeparam>
public abstract class KdlConverter<T> : IKdlConverter
{
    /// <summary>
    /// Determines whether this converter can handle the specified type.
    /// </summary>
    public bool CanConvert(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    /// <summary>
    /// Writes a value to a KDL node.
    /// </summary>
    public void Write(KdlNode node, object? value, Type type, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        Write(node, (T?)value, options, context);
    }

    /// <summary>
    /// Reads a value from a KDL node.
    /// </summary>
    public object? Read(KdlNode node, Type type, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        return Read(node, options, context);
    }

    /// <summary>
    /// Writes a typed value to a KDL node.
    /// </summary>
    protected abstract void Write(KdlNode node, T? value, KdlSerializerOptions options, KdlSerializerContext? context);

    /// <summary>
    /// Reads a typed value from a KDL node.
    /// </summary>
    protected abstract T? Read(KdlNode node, KdlSerializerOptions options, KdlSerializerContext? context);
}

