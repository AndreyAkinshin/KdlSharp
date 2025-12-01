using System.Collections.Concurrent;
using System.Reflection;
using KdlSharp.Serialization.Metadata;

namespace KdlSharp.Serialization.Reflection;

/// <summary>
/// Caches type metadata to avoid repeated reflection.
/// </summary>
internal sealed class TypeMetadataCache
{
    private readonly ConcurrentDictionary<Type, IKdlTypeMetadata> cache = new();
    private readonly KdlSerializerOptions options;

    public TypeMetadataCache(KdlSerializerOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets or creates metadata for the specified type.
    /// </summary>
    public IKdlTypeMetadata GetOrCreate(Type type)
    {
        return cache.GetOrAdd(type, CreateMetadata);
    }

    private IKdlTypeMetadata CreateMetadata(Type type)
    {
        // Determine node name
        var nodeAttribute = type.GetCustomAttribute<KdlNodeAttribute>();
        var nodeName = nodeAttribute?.Name ?? ConvertTypeName(type.Name);

        // Check if it's a record
        var isRecord = type.GetMethod("<Clone>$") != null;

        // Check if it's a collection
        var isCollection = type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);

        // Get all serializable members
        var members = GetSerializableMembers(type);

        return new TypeMetadata(type, nodeName, members, isRecord, isCollection);
    }

    private List<KdlMemberMetadata> GetSerializableMembers(Type type)
    {
        var members = new List<KdlMemberMetadata>();

        // Get properties (including inherited ones)
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Skip compiler-generated properties like EqualityContract for records
            if (prop.Name == "EqualityContract")
                continue;

            // Skip properties we can't read
            if (!prop.CanRead || prop.GetMethod == null || !prop.GetMethod.IsPublic)
                continue;

            var metadata = CreateMemberMetadata(prop.Name, prop.PropertyType, prop, prop);
            if (metadata != null && !metadata.IsIgnored)
            {
                members.Add(metadata);
            }
        }

        // Get fields (but skip for records, as they use properties)
        var isRecordType = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) != null;
        if (!isRecordType)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var metadata = CreateMemberMetadata(field.Name, field.FieldType, field, field);
                if (metadata != null && !metadata.IsIgnored)
                {
                    members.Add(metadata);
                }
            }
        }

        // Sort by position (arguments first, then properties)
        return members.OrderBy(m => m.Position == -1 ? int.MaxValue : m.Position)
                     .ThenBy(m => m.KdlName)
                     .ToList();
    }

    private KdlMemberMetadata? CreateMemberMetadata(string clrName, Type memberType, MemberInfo memberInfo, MemberInfo attributeSource)
    {
        // Check for ignore attribute
        var ignoreAttr = attributeSource.GetCustomAttribute<KdlIgnoreAttribute>();
        if (ignoreAttr?.Condition == KdlIgnoreCondition.Always)
        {
            return null; // Don't include ignored members at all
        }

        // Check for property attribute
        var propAttr = attributeSource.GetCustomAttribute<KdlPropertyAttribute>();

        // Determine KDL name
        var kdlName = propAttr?.Name ?? ConvertMemberName(clrName);

        // Determine position and kind
        int position = propAttr?.Position ?? -1;
        var kind = position >= 0 ? KdlMemberKind.Argument : KdlMemberKind.Property;

        // Check if complex type should be child node
        if (position == -1 && IsComplexType(memberType))
        {
            kind = KdlMemberKind.ChildNode;
        }

        // Determine if required
        var isRequired = IsNonNullableReferenceType(memberType, attributeSource);

        return new KdlMemberMetadata(
            clrName,
            kdlName,
            memberType,
            isRequired,
            position,
            ignoreAttr != null,
            ignoreAttr?.Condition ?? KdlIgnoreCondition.WhenNull,
            kind,
            memberInfo);
    }

    private bool IsComplexType(Type type)
    {
        // Simple types: primitives, strings, DateTime, Guid, enums
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) ||
            type == typeof(Guid) || type == typeof(decimal) || type.IsEnum ||
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Uri))
        {
            return false;
        }

        // Nullable simple types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return IsComplexType(underlyingType);
        }

        // Collections and arrays of simple types are treated as simple
        if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        return true; // Complex types become child nodes
    }

    private bool IsNonNullableReferenceType(Type type, MemberInfo memberInfo)
    {
        // Value types that are not Nullable<T> are always required
        if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
        {
            return true;
        }

        // Note: Full nullable reference type detection would require .NET 5+ NullabilityInfoContext
        // For .NET Standard 2.1 compatibility, we use a conservative approach
        return false;
    }

    private string ConvertTypeName(string typeName)
    {
        // Remove generic markers
        var backtickIndex = typeName.IndexOf('`');
        if (backtickIndex > 0)
        {
            typeName = typeName.Substring(0, backtickIndex);
        }

        return ConvertMemberName(typeName);
    }

    private string ConvertMemberName(string memberName)
    {
        return options.PropertyNamingPolicy switch
        {
            KdlNamingPolicy.CamelCase => ToCamelCase(memberName),
            KdlNamingPolicy.KebabCase => ToKebabCase(memberName),
            KdlNamingPolicy.SnakeCase => ToSnakeCase(memberName),
            KdlNamingPolicy.PascalCase => memberName,
            KdlNamingPolicy.None => memberName,
            _ => memberName
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(name[0]));

        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                result.Append('-');
                result.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                result.Append(name[i]);
            }
        }

        return result.ToString();
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(name[0]));

        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                result.Append(name[i]);
            }
        }

        return result.ToString();
    }

    private sealed class TypeMetadata : IKdlTypeMetadata
    {
        public Type Type { get; }
        public string NodeName { get; }
        public IReadOnlyList<KdlMemberMetadata> Members { get; }
        public bool IsRecord { get; }
        public bool IsCollection { get; }

        public TypeMetadata(
            Type type,
            string nodeName,
            List<KdlMemberMetadata> members,
            bool isRecord,
            bool isCollection)
        {
            Type = type;
            NodeName = nodeName;
            Members = members;
            IsRecord = isRecord;
            IsCollection = isCollection;
        }
    }
}

