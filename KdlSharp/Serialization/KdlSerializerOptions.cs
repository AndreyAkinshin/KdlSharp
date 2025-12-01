namespace KdlSharp.Serialization;

/// <summary>
/// Options for KDL serialization and deserialization.
/// </summary>
/// <example>
/// <code>
/// var options = new KdlSerializerOptions
/// {
///     RootNodeName = "config",
///     PropertyNamingPolicy = KdlNamingPolicy.KebabCase,
///     IncludeNullValues = false
/// };
/// var serializer = new KdlSerializer(options);
/// </code>
/// </example>
public sealed class KdlSerializerOptions
{
    /// <summary>
    /// Gets or sets the root node name for serialization.
    /// </summary>
    public string RootNodeName { get; set; } = "root";

    /// <summary>
    /// Gets or sets the property naming policy.
    /// </summary>
    public KdlNamingPolicy PropertyNamingPolicy { get; set; } = KdlNamingPolicy.KebabCase;

    /// <summary>
    /// Gets or sets whether to include null values in serialization.
    /// </summary>
    public bool IncludeNullValues { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use arguments for simple values.
    /// </summary>
    /// <remarks>
    /// When <c>true</c> (the default), simple scalar values marked with a position attribute
    /// are serialized as positional arguments.
    /// When <c>false</c>, all values are serialized as named properties.
    /// </remarks>
    public bool UseArgumentsForSimpleValues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to flatten single-child objects.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, if an object has no arguments or properties but exactly one
    /// child node, the child's content is promoted to the parent node, reducing nesting.
    /// When <c>false</c> (the default), the full nested structure is preserved for round-trip fidelity.
    /// </remarks>
    public bool FlattenSingleChildObjects { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to write type annotations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, type annotations are written for values, e.g., <c>(i32)42</c> or <c>(string)"hello"</c>.
    /// When <c>false</c> (the default), values are written without type annotations.
    /// </remarks>
    public bool WriteTypeAnnotations { get; set; } = false;

    /// <summary>
    /// Gets or sets the target KDL version for serialization.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="KdlVersion.V1"/>, boolean values are written as <c>true</c>/<c>false</c>
    /// and null as <c>null</c>. When set to <see cref="KdlVersion.V2"/> (the default),
    /// boolean values are written as <c>#true</c>/<c>#false</c> and null as <c>#null</c>.
    /// </remarks>
    public KdlVersion TargetVersion { get; set; } = KdlVersion.V2;

    /// <summary>
    /// Gets the list of custom converters.
    /// </summary>
    public List<Converters.IKdlConverter> Converters { get; } = new();

    /// <summary>
    /// Creates a copy of these options, including all custom converters.
    /// </summary>
    public KdlSerializerOptions Clone()
    {
        var clone = new KdlSerializerOptions
        {
            RootNodeName = RootNodeName,
            PropertyNamingPolicy = PropertyNamingPolicy,
            IncludeNullValues = IncludeNullValues,
            UseArgumentsForSimpleValues = UseArgumentsForSimpleValues,
            FlattenSingleChildObjects = FlattenSingleChildObjects,
            WriteTypeAnnotations = WriteTypeAnnotations,
            TargetVersion = TargetVersion
        };

        // Copy custom converters to ensure the clone has the same converter configuration.
        foreach (var converter in Converters)
        {
            clone.Converters.Add(converter);
        }

        return clone;
    }
}

/// <summary>
/// Naming policies for KDL property names.
/// </summary>
public enum KdlNamingPolicy
{
    /// <summary>PascalCase (C# naming style).</summary>
    PascalCase,

    /// <summary>camelCase.</summary>
    CamelCase,

    /// <summary>snake_case.</summary>
    SnakeCase,

    /// <summary>kebab-case (recommended for KDL).</summary>
    KebabCase,

    /// <summary>Uses exact CLR member names without transformation.</summary>
    None
}

