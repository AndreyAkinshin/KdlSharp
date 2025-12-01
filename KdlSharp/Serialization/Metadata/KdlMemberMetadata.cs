using System.Reflection;
using KdlSharp.Exceptions;

namespace KdlSharp.Serialization.Metadata;

/// <summary>
/// Represents metadata about a serializable member (property or field).
/// </summary>
public sealed class KdlMemberMetadata
{
    /// <summary>
    /// Gets the CLR name of the member.
    /// </summary>
    public string ClrName { get; }

    /// <summary>
    /// Gets the KDL name of the member (after applying naming policy and attributes).
    /// </summary>
    public string KdlName { get; }

    /// <summary>
    /// Gets the type of the member.
    /// </summary>
    public Type MemberType { get; }

    /// <summary>
    /// Gets whether this member is required (non-nullable reference type or marked as required).
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets the position of this member when serialized as an argument (-1 for properties).
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Gets whether this member should be ignored during serialization.
    /// </summary>
    public bool IsIgnored { get; }

    /// <summary>
    /// Gets the ignore condition for this member.
    /// </summary>
    public KdlIgnoreCondition IgnoreCondition { get; }

    /// <summary>
    /// Gets the kind of this member (property, argument, or child node).
    /// </summary>
    public KdlMemberKind Kind { get; }

    /// <summary>
    /// Gets the MemberInfo for this member (PropertyInfo or FieldInfo).
    /// </summary>
    public MemberInfo MemberInfo { get; }

    /// <summary>
    /// Initializes a new instance of KdlMemberMetadata.
    /// </summary>
    public KdlMemberMetadata(
        string clrName,
        string kdlName,
        Type memberType,
        bool isRequired,
        int position,
        bool isIgnored,
        KdlIgnoreCondition ignoreCondition,
        KdlMemberKind kind,
        MemberInfo memberInfo)
    {
        ClrName = clrName ?? throw new ArgumentNullException(nameof(clrName));
        KdlName = kdlName ?? throw new ArgumentNullException(nameof(kdlName));
        MemberType = memberType ?? throw new ArgumentNullException(nameof(memberType));
        IsRequired = isRequired;
        Position = position;
        IsIgnored = isIgnored;
        IgnoreCondition = ignoreCondition;
        Kind = kind;
        MemberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));
    }

    /// <summary>
    /// Checks equality based on all properties.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is KdlMemberMetadata other &&
               ClrName == other.ClrName &&
               KdlName == other.KdlName &&
               MemberType == other.MemberType &&
               IsRequired == other.IsRequired &&
               Position == other.Position &&
               IsIgnored == other.IsIgnored &&
               IgnoreCondition == other.IgnoreCondition &&
               Kind == other.Kind &&
               MemberInfo.Equals(other.MemberInfo);
    }

    /// <summary>
    /// Gets hash code based on properties.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(ClrName, KdlName, MemberType, IsRequired, Position, IsIgnored, IgnoreCondition, Kind);
    }

    /// <summary>
    /// Gets the value of this member from an object.
    /// </summary>
    public object? GetValue(object obj)
    {
        return MemberInfo switch
        {
            PropertyInfo prop => prop.GetValue(obj),
            FieldInfo field => field.GetValue(obj),
            _ => throw new KdlSerializationException($"Unknown member type: {MemberInfo.GetType()}")
        };
    }

    /// <summary>
    /// Sets the value of this member on an object.
    /// </summary>
    public void SetValue(object obj, object? value)
    {
        switch (MemberInfo)
        {
            case PropertyInfo prop:
                prop.SetValue(obj, value);
                break;
            case FieldInfo field:
                field.SetValue(obj, value);
                break;
            default:
                throw new KdlSerializationException($"Unknown member type: {MemberInfo.GetType()}");
        }
    }
}

/// <summary>
/// Specifies the kind of a KDL member.
/// </summary>
public enum KdlMemberKind
{
    /// <summary>
    /// Member is serialized as a KDL property (key=value).
    /// </summary>
    Property,

    /// <summary>
    /// Member is serialized as a positional argument.
    /// </summary>
    Argument,

    /// <summary>
    /// Member is serialized as a child node.
    /// </summary>
    ChildNode
}

