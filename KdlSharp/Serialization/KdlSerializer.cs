using System.Runtime.CompilerServices;
using KdlSharp.Exceptions;
using KdlSharp.Serialization.Converters;
using KdlSharp.Serialization.Metadata;
using KdlSharp.Serialization.Reflection;
using KdlSharp.Values;

namespace KdlSharp.Serialization;

/// <summary>
/// Compares objects by reference identity (not by value equality).
/// Used for cycle detection during serialization.
/// </summary>
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
}

/// <summary>
/// Provides methods for serializing and deserializing objects to/from KDL.
/// </summary>
public sealed class KdlSerializer
{
    private readonly KdlSerializerOptions options;
    private readonly TypeMetadataCache metadataCache;
    private readonly Dictionary<Type, IKdlConverter> converterCache;

    /// <summary>
    /// Initializes a new serializer with optional options.
    /// </summary>
    /// <param name="options">Optional serializer options for controlling output format.</param>
    /// <example>
    /// <code>
    /// var serializer = new KdlSerializer(new KdlSerializerOptions
    /// {
    ///     RootNodeName = "config",
    ///     PropertyNamingPolicy = KdlNamingPolicy.KebabCase
    /// });
    /// </code>
    /// </example>
    public KdlSerializer(KdlSerializerOptions? options = null)
    {
        // Clone the provided options to avoid mutating the caller's instance
        this.options = options?.Clone() ?? new KdlSerializerOptions();
        metadataCache = new TypeMetadataCache(this.options);
        converterCache = new Dictionary<Type, IKdlConverter>();

        // Register built-in converters, avoiding duplicates
        // Custom converters from options take precedence (they're first in the list)
        foreach (var builtIn in BuiltInConverters.GetAll())
        {
            // Only add if no existing converter handles this type
            var builtInTargetType = GetConverterTargetType(builtIn);
            if (builtInTargetType != null && !this.options.Converters.Any(c => c.CanConvert(builtInTargetType)))
            {
                this.options.Converters.Add(builtIn);
            }
        }
    }

    private static Type? GetConverterTargetType(IKdlConverter converter)
    {
        // Find the generic type argument from KdlConverter<T>
        var converterType = converter.GetType();
        while (converterType != null)
        {
            if (converterType.IsGenericType &&
                converterType.GetGenericTypeDefinition() == typeof(KdlConverter<>))
            {
                return converterType.GetGenericArguments()[0];
            }
            converterType = converterType.BaseType;
        }
        return null;
    }

    /// <summary>
    /// Serializes an object to a KDL string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="context">Optional context for custom serialization behavior.</param>
    /// <returns>A KDL-formatted string representation of the object.</returns>
    /// <exception cref="KdlSerializationException">Thrown when the object cannot be serialized (e.g., unsupported type, circular reference, type conversion failure).</exception>
    /// <example>
    /// <code>
    /// public record AppConfig(string Name, int Port);
    /// var config = new AppConfig("MyApp", 8080);
    /// var serializer = new KdlSerializer();
    /// string kdl = serializer.Serialize(config);
    /// // Output: root name="MyApp" port=8080
    /// </code>
    /// </example>
    public string Serialize<T>(T value, KdlSerializerContext? context = null)
    {
        var doc = ToDocument(value, context);
        var formatterSettings = new Settings.KdlFormatterSettings
        {
            TargetVersion = options.TargetVersion
        };
        return doc.ToKdlString(formatterSettings);
    }

    /// <summary>
    /// Serializes an object to a stream.
    /// </summary>
    /// <exception cref="KdlSerializationException">Thrown when the object cannot be serialized (e.g., unsupported type, circular reference, type conversion failure).</exception>
    public void Serialize<T>(T value, Stream stream, KdlSerializerContext? context = null)
    {
        var kdl = Serialize(value, context);
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 4096, true);
        writer.Write(kdl);
    }

    /// <summary>
    /// Serializes an object to a TextWriter.
    /// </summary>
    /// <exception cref="KdlSerializationException">Thrown when the object cannot be serialized (e.g., unsupported type, circular reference, type conversion failure).</exception>
    public void Serialize<T>(T value, TextWriter writer, KdlSerializerContext? context = null)
    {
        var kdl = Serialize(value, context);
        writer.Write(kdl);
    }

    /// <summary>
    /// Converts an object to a KdlDocument.
    /// </summary>
    /// <exception cref="KdlSerializationException">Thrown when the object cannot be serialized (e.g., unsupported type, circular reference, type conversion failure).</exception>
    public KdlDocument ToDocument<T>(T value, KdlSerializerContext? context = null)
    {
        var doc = new KdlDocument();

        if (value == null)
        {
            if (options.IncludeNullValues)
            {
                var nullNode = new KdlNode(options.RootNodeName);
                nullNode.AddArgument(KdlNull.Instance);
                doc.Nodes.Add(nullNode);
            }
            return doc;
        }

        var type = value.GetType();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var node = SerializeObject(value, type, options.RootNodeName, context, visited);
        if (node != null)
        {
            doc.Nodes.Add(node);
        }

        return doc;
    }

    private KdlNode? SerializeObject(object? value, Type type, string nodeName, KdlSerializerContext? context, HashSet<object> visited)
    {
        if (value == null)
        {
            if (!options.IncludeNullValues)
                return null;

            var nullNode = new KdlNode(nodeName);
            nullNode.AddArgument(KdlNull.Instance);
            return nullNode;
        }

        // Cycle detection for reference types (excluding strings which are immutable and safe)
        if (!type.IsValueType && type != typeof(string))
        {
            if (!visited.Add(value))
            {
                throw new KdlSerializationException(
                    $"Circular reference detected while serializing object of type '{type.Name}'. " +
                    "KDL does not support circular references. Consider using IDs or breaking the cycle before serialization.");
            }
        }

        try
        {
            return SerializeObjectCore(value, type, nodeName, context, visited);
        }
        finally
        {
            // Remove from visited set when leaving scope (allows same object in different branches)
            if (!type.IsValueType && type != typeof(string))
            {
                visited.Remove(value);
            }
        }
    }

    private KdlNode? SerializeObjectCore(object value, Type type, string nodeName, KdlSerializerContext? context, HashSet<object> visited)
    {
        // Check for custom converter
        var converter = FindConverter(type);
        if (converter != null)
        {
            var converterNode = new KdlNode(nodeName);
            converter.Write(converterNode, value, type, options, context);
            return converterNode;
        }

        // Handle simple types
        if (IsSimpleType(type))
        {
            var simpleNode = new KdlNode(nodeName);
            var kdlValue = MemberBinder.ConvertToKdlValue(value);
            kdlValue = ApplyTypeAnnotation(kdlValue, type);
            simpleNode.AddArgument(kdlValue);
            return simpleNode;
        }

        // Handle collections
        if (value is System.Collections.IEnumerable enumerable && type != typeof(string))
        {
            var collectionNode = new KdlNode(nodeName);

            // Determine element type
            Type elementType = typeof(object);
            if (type.IsArray)
            {
                elementType = type.GetElementType()!;
            }
            else if (type.IsGenericType)
            {
                elementType = type.GetGenericArguments()[0];
            }

            foreach (var item in enumerable)
            {
                if (item == null)
                {
                    if (options.IncludeNullValues)
                    {
                        collectionNode.AddArgument(KdlNull.Instance);
                    }
                }
                else if (IsSimpleType(elementType) || IsSimpleType(item.GetType()))
                {
                    // Simple types go as arguments
                    var kdlValue = MemberBinder.ConvertToKdlValue(item);
                    kdlValue = ApplyTypeAnnotation(kdlValue, item.GetType());
                    collectionNode.AddArgument(kdlValue);
                }
                else
                {
                    // Complex types (POCOs) go as child nodes
                    // Use runtime type for polymorphic serialization and naming policy for node name
                    var itemNode = SerializeObject(item, item.GetType(), GetCollectionItemNodeName(item.GetType()), context, visited);
                    if (itemNode != null)
                    {
                        collectionNode.AddChild(itemNode);
                    }
                }
            }
            return collectionNode;
        }

        // Handle complex objects using metadata
        var metadata = metadataCache.GetOrCreate(type);
        var objectNode = new KdlNode(nodeName);

        // Serialize members based on their kind
        // When UseArgumentsForSimpleValues is false, arguments become properties
        var arguments = options.UseArgumentsForSimpleValues
            ? metadata.Members.Where(m => m.Kind == KdlMemberKind.Argument).OrderBy(m => m.Position)
            : Enumerable.Empty<KdlMemberMetadata>();
        var properties = options.UseArgumentsForSimpleValues
            ? metadata.Members.Where(m => m.Kind == KdlMemberKind.Property)
            : metadata.Members.Where(m => m.Kind == KdlMemberKind.Argument || m.Kind == KdlMemberKind.Property);
        var childNodes = metadata.Members.Where(m => m.Kind == KdlMemberKind.ChildNode);

        // Add arguments
        foreach (var member in arguments)
        {
            var memberValue = member.GetValue(value);
            if (MemberBinder.ShouldIgnore(memberValue, member.IgnoreCondition))
                continue;

            if (memberValue == null)
            {
                if (options.IncludeNullValues)
                {
                    objectNode.AddArgument(KdlNull.Instance);
                }
            }
            else
            {
                var kdlValue = MemberBinder.ConvertToKdlValue(memberValue);
                kdlValue = ApplyTypeAnnotation(kdlValue, member.MemberType);
                objectNode.AddArgument(kdlValue);
            }
        }

        // Add properties
        foreach (var member in properties)
        {
            var memberValue = member.GetValue(value);
            // When IncludeNullValues is true, override WhenNull ignore condition
            var effectiveIgnoreCondition = options.IncludeNullValues && member.IgnoreCondition == KdlIgnoreCondition.WhenNull
                ? (KdlIgnoreCondition)(-1) // Use invalid value to trigger default case (never ignore)
                : member.IgnoreCondition;
            if (MemberBinder.ShouldIgnore(memberValue, effectiveIgnoreCondition))
                continue;

            if (memberValue == null)
            {
                if (options.IncludeNullValues)
                {
                    objectNode.AddProperty(member.KdlName, KdlNull.Instance);
                }
            }
            else
            {
                // Check if this is a collection - serialize as child node
                if (member.MemberType != typeof(string) &&
                    typeof(System.Collections.IEnumerable).IsAssignableFrom(member.MemberType))
                {
                    var collectionNode = new KdlNode(member.KdlName);

                    // Determine element type
                    Type elementType = typeof(object);
                    if (member.MemberType.IsArray)
                    {
                        elementType = member.MemberType.GetElementType()!;
                    }
                    else if (member.MemberType.IsGenericType)
                    {
                        elementType = member.MemberType.GetGenericArguments()[0];
                    }

                    foreach (var item in (System.Collections.IEnumerable)memberValue)
                    {
                        if (item == null)
                        {
                            if (options.IncludeNullValues)
                            {
                                collectionNode.AddArgument(KdlNull.Instance);
                            }
                        }
                        else if (IsSimpleType(elementType) || IsSimpleType(item.GetType()))
                        {
                            // Simple types go as arguments
                            var kdlValue = MemberBinder.ConvertToKdlValue(item);
                            kdlValue = ApplyTypeAnnotation(kdlValue, item.GetType());
                            collectionNode.AddArgument(kdlValue);
                        }
                        else
                        {
                            // Complex types (POCOs) go as child nodes
                            // Use runtime type for polymorphic serialization and naming policy for node name
                            var itemNode = SerializeObject(item, item.GetType(), GetCollectionItemNodeName(item.GetType()), context, visited);
                            if (itemNode != null)
                            {
                                collectionNode.AddChild(itemNode);
                            }
                        }
                    }
                    objectNode.AddChild(collectionNode);
                }
                else
                {
                    var kdlValue = MemberBinder.ConvertToKdlValue(memberValue);
                    kdlValue = ApplyTypeAnnotation(kdlValue, member.MemberType);
                    objectNode.AddProperty(member.KdlName, kdlValue);
                }
            }
        }

        // Add child nodes
        foreach (var member in childNodes)
        {
            var memberValue = member.GetValue(value);
            if (MemberBinder.ShouldIgnore(memberValue, member.IgnoreCondition))
                continue;

            // Use runtime type for polymorphic serialization to preserve derived type properties
            var effectiveType = memberValue?.GetType() ?? member.MemberType;
            var childNode = SerializeObject(memberValue, effectiveType, member.KdlName, context, visited);
            if (childNode != null)
            {
                objectNode.AddChild(childNode);
            }
        }

        // Apply FlattenSingleChildObjects: if the node has no arguments/properties but exactly one child,
        // and the child has content, we can "flatten" by moving the child's content into this node
        if (options.FlattenSingleChildObjects &&
            objectNode.Arguments.Count == 0 &&
            objectNode.Properties.Count == 0 &&
            objectNode.Children.Count == 1)
        {
            var singleChild = objectNode.Children[0];
            // Move child's arguments, properties, and nested children to the parent
            foreach (var arg in singleChild.Arguments)
            {
                objectNode.AddArgument(arg);
            }
            foreach (var prop in singleChild.Properties)
            {
                objectNode.AddProperty(prop.Key, prop.Value);
            }
            // Clear and re-add grandchildren
            objectNode.Children.Clear();
            foreach (var grandChild in singleChild.Children)
            {
                objectNode.AddChild(grandChild);
            }
        }

        return objectNode;
    }

    private bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type.IsEnum ||
               Nullable.GetUnderlyingType(type) != null;
    }

    private IKdlConverter? FindConverter(Type type)
    {
        if (converterCache.TryGetValue(type, out var cached))
            return cached;

        foreach (var converter in options.Converters)
        {
            if (converter.CanConvert(type))
            {
                converterCache[type] = converter;
                return converter;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts a KdlDocument to an object.
    /// </summary>
    public T FromDocument<T>(KdlDocument document, KdlSerializerContext? context = null)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (document.Nodes.Count == 0)
            throw new KdlSerializationException("Document contains no nodes");

        var type = typeof(T);
        var node = document.Nodes[0]; // Use first node as root

        var result = DeserializeObject(node, type, context);
        return (T)result!;
    }

    private object? DeserializeObject(KdlNode node, Type type, KdlSerializerContext? context)
    {
        // Handle null
        if (node.Arguments.Count == 1 && node.Arguments[0].IsNull())
        {
            return null;
        }

        // Check for custom converter
        var converter = FindConverter(type);
        if (converter != null)
        {
            return converter.Read(node, type, options, context);
        }

        // Handle simple types from arguments
        if (IsSimpleType(type) && node.Arguments.Count > 0)
        {
            return MemberBinder.BindValue(node.Arguments[0], type);
        }

        // Handle collections
        if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return DeserializeCollection(node, type, context);
        }

        // Handle complex objects
        var metadata = metadataCache.GetOrCreate(type);

        // For records, we need to use the primary constructor
        object instance;
        if (metadata.IsRecord)
        {
            // Find the primary constructor (the one with the most parameters matching member names)
            var constructors = type.GetConstructors();
            var primaryCtor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

            if (primaryCtor != null)
            {
                var parameters = primaryCtor.GetParameters();
                var args = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var member = metadata.Members.FirstOrDefault(m =>
                        m.ClrName.Equals(param.Name, StringComparison.OrdinalIgnoreCase));

                    if (member != null)
                    {
                        // Get value from node based on member kind
                        // When UseArgumentsForSimpleValues=false, arguments are serialized as properties
                        if (member.Kind == KdlMemberKind.Argument)
                        {
                            if (options.UseArgumentsForSimpleValues && member.Position >= 0 && member.Position < node.Arguments.Count)
                            {
                                args[i] = MemberBinder.BindValue(node.Arguments[member.Position], param.ParameterType);
                            }
                            else
                            {
                                // Read from property (when UseArgumentsForSimpleValues=false or argument not found)
                                var propValue = node.GetProperty(member.KdlName);
                                if (propValue != null)
                                {
                                    args[i] = MemberBinder.BindValue(propValue, param.ParameterType);
                                }
                                else
                                {
                                    args[i] = param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType);
                                }
                            }
                        }
                        else if (member.Kind == KdlMemberKind.Property)
                        {
                            var propValue = node.GetProperty(member.KdlName);
                            if (propValue != null)
                            {
                                args[i] = MemberBinder.BindValue(propValue, param.ParameterType);
                            }
                            else
                            {
                                args[i] = param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType);
                            }
                        }
                        else if (member.Kind == KdlMemberKind.ChildNode)
                        {
                            // Look for child node
                            var childNode = node.Children.FirstOrDefault(c => c.Name == member.KdlName);
                            if (childNode != null)
                            {
                                args[i] = DeserializeObject(childNode, param.ParameterType, context);
                            }
                            else if (options.FlattenSingleChildObjects)
                            {
                                // When FlattenSingleChildObjects is enabled, the child's content is in the parent
                                // Create a virtual node from the parent's properties
                                var virtualNode = new KdlNode(member.KdlName);
                                foreach (var arg in node.Arguments)
                                {
                                    virtualNode.AddArgument(arg);
                                }
                                foreach (var prop in node.Properties)
                                {
                                    virtualNode.AddProperty(prop.Key, prop.Value);
                                }
                                foreach (var child in node.Children)
                                {
                                    virtualNode.AddChild(child);
                                }
                                args[i] = DeserializeObject(virtualNode, param.ParameterType, context);
                            }
                            else
                            {
                                args[i] = param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType);
                            }
                        }
                        else
                        {
                            args[i] = param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType);
                        }
                    }
                    else
                    {
                        args[i] = param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType);
                    }
                }

                instance = primaryCtor.Invoke(args);
            }
            else
            {
                instance = Activator.CreateInstance(type)!;
            }
        }
        else
        {
            instance = Activator.CreateInstance(type)!;
        }

        // Deserialize arguments
        // When UseArgumentsForSimpleValues=false, arguments are serialized as properties
        var arguments = metadata.Members.Where(m => m.Kind == KdlMemberKind.Argument).OrderBy(m => m.Position).ToList();
        if (options.UseArgumentsForSimpleValues)
        {
            for (int i = 0; i < Math.Min(arguments.Count, node.Arguments.Count); i++)
            {
                var member = arguments[i];
                var value = MemberBinder.BindValue(node.Arguments[i], member.MemberType);
                member.SetValue(instance, value);
            }
        }
        else
        {
            // Read argument members from properties when UseArgumentsForSimpleValues=false
            foreach (var member in arguments)
            {
                var kdlValue = node.GetProperty(member.KdlName);
                if (kdlValue != null)
                {
                    var value = MemberBinder.BindValue(kdlValue, member.MemberType);
                    member.SetValue(instance, value);
                }
            }
        }

        // Deserialize properties
        var properties = metadata.Members.Where(m => m.Kind == KdlMemberKind.Property).ToList();
        foreach (var member in properties)
        {
            // Check if this property is serialized as a collection (child node)
            if (member.MemberType != typeof(string) &&
                typeof(System.Collections.IEnumerable).IsAssignableFrom(member.MemberType))
            {
                // Look for child node with collection data
                var childNode = node.Children.FirstOrDefault(c => c.Name == member.KdlName);
                if (childNode != null)
                {
                    var collectionValue = DeserializeCollection(childNode, member.MemberType, context);
                    member.SetValue(instance, collectionValue);
                }
            }
            else
            {
                // Regular property
                var kdlValue = node.GetProperty(member.KdlName);
                if (kdlValue != null)
                {
                    var value = MemberBinder.BindValue(kdlValue, member.MemberType);
                    member.SetValue(instance, value);
                }
            }
        }

        // Deserialize child nodes
        var childMembers = metadata.Members.Where(m => m.Kind == KdlMemberKind.ChildNode).ToList();
        foreach (var child in node.Children)
        {
            var member = childMembers.FirstOrDefault(m => m.KdlName == child.Name);
            if (member != null)
            {
                var childValue = DeserializeObject(child, member.MemberType, context);
                member.SetValue(instance, childValue);
            }
        }

        // Handle FlattenSingleChildObjects deserialization:
        // When enabled and there's exactly one child member that wasn't found as a child node,
        // treat the current node's properties as the flattened child's properties
        if (options.FlattenSingleChildObjects && childMembers.Count == 1)
        {
            var singleChildMember = childMembers[0];
            if (!node.Children.Any(c => c.Name == singleChildMember.KdlName))
            {
                // Create a virtual node from the current node's remaining properties
                // The child's content was flattened into the parent
                var virtualNode = new KdlNode(singleChildMember.KdlName);
                foreach (var arg in node.Arguments)
                {
                    virtualNode.AddArgument(arg);
                }
                foreach (var prop in node.Properties)
                {
                    // Skip properties that were already consumed by this type's property members
                    if (!properties.Any(p => p.KdlName == prop.Key) && !arguments.Any(a => a.KdlName == prop.Key))
                    {
                        virtualNode.AddProperty(prop.Key, prop.Value);
                    }
                }
                foreach (var child in node.Children)
                {
                    virtualNode.AddChild(child);
                }
                var childValue = DeserializeObject(virtualNode, singleChildMember.MemberType, context);
                singleChildMember.SetValue(instance, childValue);
            }
        }
        else
        {
            // Check for missing required child nodes - if a property exists instead, it's an error
            foreach (var member in childMembers)
            {
                if (!node.Children.Any(c => c.Name == member.KdlName))
                {
                    // No child node found - check if there's a property with this name (which would be incorrect)
                    var propValue = node.GetProperty(member.KdlName);
                    if (propValue != null)
                    {
                        throw new KdlSerializationException($"Property '{member.KdlName}' must be a child node, not a property");
                    }
                }
            }
        }

        return instance;
    }

    private object DeserializeCollection(KdlNode node, Type collectionType, KdlSerializerContext? context)
    {
        // Determine element type
        Type elementType;
        if (collectionType.IsArray)
        {
            elementType = collectionType.GetElementType()!;
        }
        else if (collectionType.IsGenericType)
        {
            elementType = collectionType.GetGenericArguments()[0];
        }
        else
        {
            elementType = typeof(object);
        }

        // Deserialize elements from arguments (simple types) or children (complex types)
        var elements = new List<object?>();

        // Check if element type is simple (can be KdlValue) or complex (POCO)
        bool isSimpleElement = IsSimpleType(elementType);

        if (isSimpleElement)
        {
            // Deserialize from arguments
            foreach (var arg in node.Arguments)
            {
                var element = MemberBinder.BindValue(arg, elementType);
                elements.Add(element);
            }
        }
        else
        {
            // Deserialize from children (complex objects)
            foreach (var child in node.Children)
            {
                var element = DeserializeObject(child, elementType, context);
                elements.Add(element);
            }
        }

        // Convert to appropriate collection type
        if (collectionType.IsArray)
        {
            var array = Array.CreateInstance(elementType, elements.Count);
            for (int i = 0; i < elements.Count; i++)
            {
                array.SetValue(elements[i], i);
            }
            return array;
        }
        else if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
            foreach (var element in elements)
            {
                list.Add(element);
            }
            return list;
        }
        else
        {
            // Try to construct the collection type
            var instance = Activator.CreateInstance(collectionType)!;
            if (instance is System.Collections.IList list)
            {
                foreach (var element in elements)
                {
                    list.Add(element);
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Deserializes a KDL string to an object.
    /// </summary>
    /// <exception cref="KdlParseException">Thrown when the KDL string has invalid syntax.</exception>
    /// <exception cref="KdlSerializationException">Thrown when the KDL cannot be deserialized to the target type (e.g., empty document, type mismatch, binding failure).</exception>
    public T Deserialize<T>(string kdl, KdlSerializerContext? context = null)
    {
        var doc = KdlDocument.Parse(kdl, GetParserSettings());
        return FromDocument<T>(doc, context);
    }

    /// <summary>
    /// Deserializes a stream to an object.
    /// </summary>
    /// <exception cref="KdlParseException">Thrown when the KDL string has invalid syntax.</exception>
    /// <exception cref="KdlSerializationException">Thrown when the KDL cannot be deserialized to the target type (e.g., empty document, type mismatch, binding failure).</exception>
    public T Deserialize<T>(Stream stream, KdlSerializerContext? context = null)
    {
        var doc = KdlDocument.ParseStream(stream, GetParserSettings(), leaveOpen: true);
        return FromDocument<T>(doc, context);
    }

    /// <summary>
    /// Deserializes a KDL string to an object of the specified type.
    /// </summary>
    /// <exception cref="KdlParseException">Thrown when the KDL string has invalid syntax.</exception>
    /// <exception cref="KdlSerializationException">Thrown when the KDL cannot be deserialized to the target type (e.g., empty document, type mismatch, binding failure).</exception>
    public object Deserialize(string kdl, Type returnType, KdlSerializerContext? context = null)
    {
        var doc = KdlDocument.Parse(kdl, GetParserSettings());

        if (doc.Nodes.Count == 0)
            throw new KdlSerializationException("Document contains no nodes");

        var node = doc.Nodes[0];
        return DeserializeObject(node, returnType, context)!;
    }

    /// <summary>
    /// Attempts to deserialize a KDL string to an object without throwing exceptions.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to.</typeparam>
    /// <param name="kdl">The KDL string to deserialize.</param>
    /// <param name="result">When this method returns, contains the deserialized object if successful; otherwise, the default value.</param>
    /// <returns><c>true</c> if deserialization was successful; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (serializer.TryDeserialize&lt;MyConfig&gt;(kdlText, out var config))
    /// {
    ///     // Use config
    /// }
    /// </code>
    /// </example>
    public bool TryDeserialize<T>(string kdl, out T? result)
    {
        return TryDeserialize(kdl, out result, out _);
    }

    /// <summary>
    /// Attempts to deserialize a KDL string to an object without throwing exceptions, providing error details on failure.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to.</typeparam>
    /// <param name="kdl">The KDL string to deserialize.</param>
    /// <param name="result">When this method returns, contains the deserialized object if successful; otherwise, the default value.</param>
    /// <param name="error">When this method returns, contains the exception if deserialization failed; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization was successful; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (serializer.TryDeserialize&lt;MyConfig&gt;(kdlText, out var config, out var error))
    /// {
    ///     // Use config
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Deserialization failed: {error.Message}");
    /// }
    /// </code>
    /// </example>
    public bool TryDeserialize<T>(string kdl, out T? result, out Exception? error)
    {
        try
        {
            result = Deserialize<T>(kdl);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            result = default;
            error = ex;
            return false;
        }
    }

    /// <summary>
    /// Asynchronously deserializes a stream to an object.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to.</typeparam>
    /// <param name="stream">The stream containing the KDL document.</param>
    /// <param name="context">Optional context for custom deserialization behavior.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous deserialization operation.</returns>
    /// <exception cref="KdlParseException">Thrown when the KDL has invalid syntax.</exception>
    /// <exception cref="KdlSerializationException">Thrown when the KDL cannot be deserialized to the target type.</exception>
    public async Task<T> DeserializeStreamAsync<T>(Stream stream, KdlSerializerContext? context = null, CancellationToken cancellationToken = default)
    {
        var doc = await KdlDocument.ParseStreamAsync(stream, GetParserSettings(), leaveOpen: true, cancellationToken).ConfigureAwait(false);
        return FromDocument<T>(doc, context);
    }

    /// <summary>
    /// Asynchronously serializes a sequence of objects as a stream of top-level nodes.
    /// </summary>
    public async Task SerializeStreamAsync<T>(
        IAsyncEnumerable<T> values,
        Stream stream,
        KdlSerializerContext? context = null,
        CancellationToken cancellationToken = default)
    {
        // Use UTF8 encoding without BOM to avoid writing bytes for empty sequences
        var utf8NoBom = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using var writer = new StreamWriter(stream, utf8NoBom, 4096, true);
        var formatterSettings = new Settings.KdlFormatterSettings
        {
            TargetVersion = options.TargetVersion
        };

        // Reuse StringWriter and KdlWriter to reduce allocations while preserving async I/O
        var stringWriter = new StringWriter();
        using var kdlWriter = new Formatting.KdlWriter(stringWriter, formatterSettings);

        await foreach (var value in values.WithCancellation(cancellationToken))
        {
            // Check cancellation token explicitly to ensure cancellation is respected
            // even if the async enumerator doesn't check it
            cancellationToken.ThrowIfCancellationRequested();

            // Each item gets its own visited set for cycle detection
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var node = SerializeObject(value, value?.GetType() ?? typeof(T), options.RootNodeName, context, visited);
            if (node != null)
            {
                // Write node to reusable StringWriter
                kdlWriter.WriteNode(node);

                // Async write to output stream
                var kdl = stringWriter.ToString();
                await writer.WriteAsync(kdl).ConfigureAwait(false);

                // Clear StringWriter for next node (reuse underlying StringBuilder)
                stringWriter.GetStringBuilder().Clear();
            }
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    private Settings.KdlParserSettings GetParserSettings()
    {
        return new Settings.KdlParserSettings
        {
            TargetVersion = options.TargetVersion
        };
    }

    private string GetCollectionItemNodeName(Type elementType)
    {
        // Get metadata for the element type, which applies naming policy
        var metadata = metadataCache.GetOrCreate(elementType);
        return metadata.NodeName;
    }

    private KdlValue ApplyTypeAnnotation(KdlValue value, Type clrType)
    {
        if (!options.WriteTypeAnnotations)
            return value;

        // Map CLR types to KDL type annotation names
        var typeName = GetTypeAnnotationName(clrType);
        if (typeName != null)
        {
            // Create a new instance to avoid mutating singletons
            // Note: Clone() returns singletons when TypeAnnotation is null, so we need
            // to explicitly create new instances for KdlBoolean and KdlNull
            KdlValue cloned = value switch
            {
                KdlBoolean b => new KdlBoolean(b.Value),
                KdlNull => new KdlNull(),
                _ => value.Clone()
            };
            cloned.TypeAnnotation = new KdlAnnotation(typeName);
            return cloned;
        }
        return value;
    }

    private static string? GetTypeAnnotationName(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        // Map common CLR types to KDL type annotation names
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "i32";
        if (type == typeof(long)) return "i64";
        if (type == typeof(short)) return "i16";
        if (type == typeof(sbyte)) return "i8";
        if (type == typeof(uint)) return "u32";
        if (type == typeof(ulong)) return "u64";
        if (type == typeof(ushort)) return "u16";
        if (type == typeof(byte)) return "u8";
        if (type == typeof(float)) return "f32";
        if (type == typeof(double)) return "f64";
        if (type == typeof(decimal)) return "decimal128";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(DateTime)) return "datetime";
        if (type == typeof(DateTimeOffset)) return "datetime";
        if (type == typeof(TimeSpan)) return "duration";
        if (type == typeof(Guid)) return "uuid";
        if (type == typeof(Uri)) return "url";
        if (type.IsEnum) return "string";

        return null;
    }
}

