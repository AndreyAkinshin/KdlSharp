namespace KdlSharp.Query;

/// <summary>
/// Represents a complete KDL query.
/// </summary>
public sealed class Query
{
    /// <summary>
    /// Gets the list of selectors in this query.
    /// </summary>
    public IReadOnlyList<Selector> Selectors { get; }

    /// <summary>
    /// Initializes a new query with the specified selectors.
    /// </summary>
    /// <param name="selectors">The list of selectors.</param>
    /// <exception cref="ArgumentNullException"><paramref name="selectors"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="selectors"/> is empty.</exception>
    public Query(IReadOnlyList<Selector> selectors)
    {
        Selectors = selectors ?? throw new ArgumentNullException(nameof(selectors));
        if (selectors.Count == 0)
            throw new ArgumentException("Query must have at least one selector", nameof(selectors));
    }
}

/// <summary>
/// Represents a selector chain (e.g., "a > b >> c").
/// </summary>
public sealed class Selector
{
    /// <summary>
    /// Gets the list of segments in this selector chain.
    /// </summary>
    public IReadOnlyList<SelectorSegment> Segments { get; }

    /// <summary>
    /// Initializes a new selector with the specified segments.
    /// </summary>
    /// <param name="segments">The list of segments.</param>
    /// <exception cref="ArgumentNullException"><paramref name="segments"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="segments"/> is empty.</exception>
    public Selector(IReadOnlyList<SelectorSegment> segments)
    {
        Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        if (segments.Count == 0)
            throw new ArgumentException("Selector must have at least one segment", nameof(segments));
    }
}

/// <summary>
/// Represents a single segment in a selector chain.
/// </summary>
public sealed class SelectorSegment
{
    /// <summary>
    /// Gets the filter for this segment.
    /// </summary>
    public Filter Filter { get; }

    /// <summary>
    /// Gets the operator connecting this segment to the next, or null if this is the last segment.
    /// </summary>
    public SelectorOperator? Operator { get; }

    /// <summary>
    /// Initializes a new selector segment.
    /// </summary>
    /// <param name="filter">The filter for this segment.</param>
    /// <param name="op">The operator connecting to the next segment.</param>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null.</exception>
    public SelectorSegment(Filter filter, SelectorOperator? op = null)
    {
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        Operator = op;
    }
}

/// <summary>
/// Selector operators for connecting filter segments.
/// </summary>
public enum SelectorOperator
{
    /// <summary>&gt; - Direct child</summary>
    Child,
    /// <summary>&gt;&gt; - Descendant</summary>
    Descendant,
    /// <summary>+ - Next sibling</summary>
    NextSibling,
    /// <summary>++ - Following sibling</summary>
    FollowingSibling
}

/// <summary>
/// Represents a filter (either top() or matchers).
/// </summary>
public abstract class Filter
{
}

/// <summary>
/// Represents the top() filter.
/// </summary>
public sealed class TopFilter : Filter
{
    /// <summary>
    /// Gets the singleton instance of the top filter.
    /// </summary>
    public static readonly TopFilter Instance = new TopFilter();
    private TopFilter() { }
}

/// <summary>
/// Represents a filter with matchers.
/// </summary>
public sealed class MatchersFilter : Filter
{
    /// <summary>
    /// Gets the type matcher, or null if no type filtering.
    /// </summary>
    public TypeMatcher? TypeMatcher { get; }

    /// <summary>
    /// Gets the node name to match, or null to match any name.
    /// </summary>
    public string? NodeName { get; }

    /// <summary>
    /// Gets the list of accessor matchers (inside []).
    /// </summary>
    public IReadOnlyList<AccessorMatcher> AccessorMatchers { get; }

    /// <summary>
    /// Initializes a new matchers filter.
    /// </summary>
    /// <param name="typeMatcher">Optional type matcher.</param>
    /// <param name="nodeName">Optional node name to match.</param>
    /// <param name="accessorMatchers">Optional list of accessor matchers.</param>
    public MatchersFilter(
        TypeMatcher? typeMatcher = null,
        string? nodeName = null,
        IReadOnlyList<AccessorMatcher>? accessorMatchers = null)
    {
        TypeMatcher = typeMatcher;
        NodeName = nodeName;
        AccessorMatchers = accessorMatchers ?? Array.Empty<AccessorMatcher>();
    }
}

/// <summary>
/// Represents a type matcher (e.g., (foo) or ()).
/// </summary>
public sealed class TypeMatcher
{
    /// <summary>Type name, or null for any type ().</summary>
    public string? TypeName { get; }

    /// <summary>
    /// Initializes a new type matcher.
    /// </summary>
    /// <param name="typeName">The type name to match, or null for any type.</param>
    public TypeMatcher(string? typeName)
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Gets whether this matcher matches any type.
    /// </summary>
    public bool IsAnyType => TypeName == null;
}

/// <summary>
/// Represents an accessor matcher (inside []).
/// </summary>
public sealed class AccessorMatcher
{
    /// <summary>
    /// Gets the comparison expression, or null if just checking for existence.
    /// </summary>
    public Comparison? Comparison { get; }

    /// <summary>
    /// Gets the accessor, or null if empty brackets [].
    /// </summary>
    public Accessor? Accessor { get; }

    /// <summary>
    /// Initializes a new accessor matcher.
    /// </summary>
    /// <param name="comparison">Optional comparison expression.</param>
    /// <param name="accessor">Optional accessor.</param>
    public AccessorMatcher(Comparison? comparison = null, Accessor? accessor = null)
    {
        Comparison = comparison;
        Accessor = accessor;
    }

    /// <summary>
    /// Gets whether this is an empty matcher [].
    /// </summary>
    public bool IsEmpty => Comparison == null && Accessor == null;
}

/// <summary>
/// Represents a comparison expression.
/// </summary>
public sealed class Comparison
{
    /// <summary>
    /// Gets the left-hand side accessor.
    /// </summary>
    public Accessor Left { get; }

    /// <summary>
    /// Gets the comparison operator.
    /// </summary>
    public MatcherOperator Operator { get; }

    /// <summary>
    /// Gets the right-hand side value (string, number, boolean, null, or TypeMatcher).
    /// </summary>
    public object Right { get; }

    /// <summary>
    /// Initializes a new comparison expression.
    /// </summary>
    /// <param name="left">The left-hand side accessor.</param>
    /// <param name="op">The comparison operator.</param>
    /// <param name="right">The right-hand side value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
    public Comparison(Accessor left, MatcherOperator op, object right)
    {
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Operator = op;
        Right = right ?? throw new ArgumentNullException(nameof(right));
    }
}

/// <summary>
/// Comparison operators.
/// </summary>
public enum MatcherOperator
{
    /// <summary>=</summary>
    Equal,
    /// <summary>!=</summary>
    NotEqual,
    /// <summary>&gt;</summary>
    GreaterThan,
    /// <summary>&lt;</summary>
    LessThan,
    /// <summary>&gt;=</summary>
    GreaterThanOrEqual,
    /// <summary>&lt;=</summary>
    LessThanOrEqual,
    /// <summary>^= (starts with)</summary>
    StartsWith,
    /// <summary>$= (ends with)</summary>
    EndsWith,
    /// <summary>*= (contains)</summary>
    Contains
}

/// <summary>
/// Represents an accessor (val(), prop(), name(), tag(), etc.).
/// </summary>
public abstract class Accessor
{
}

/// <summary>
/// Accesses a value by index.
/// </summary>
public sealed class ValAccessor : Accessor
{
    /// <summary>Value index (0-based).</summary>
    public int Index { get; }

    /// <summary>
    /// Initializes a new value accessor.
    /// </summary>
    /// <param name="index">The value index (0-based).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public ValAccessor(int index = 0)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative");
        Index = index;
    }
}

/// <summary>
/// Accesses a property by name.
/// </summary>
public sealed class PropAccessor : Accessor
{
    /// <summary>
    /// Gets the property name to access.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Initializes a new property accessor.
    /// </summary>
    /// <param name="propertyName">The property name to access.</param>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null.</exception>
    public PropAccessor(string propertyName)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    }
}

/// <summary>
/// Accesses the node name.
/// </summary>
public sealed class NameAccessor : Accessor
{
    /// <summary>
    /// Gets the singleton instance of the name accessor.
    /// </summary>
    public static readonly NameAccessor Instance = new NameAccessor();
    private NameAccessor() { }
}

/// <summary>
/// Accesses the type annotation (tag).
/// </summary>
public sealed class TagAccessor : Accessor
{
    /// <summary>
    /// Gets the singleton instance of the tag accessor.
    /// </summary>
    public static readonly TagAccessor Instance = new TagAccessor();
    private TagAccessor() { }
}

/// <summary>
/// Accesses all values.
/// </summary>
public sealed class ValuesAccessor : Accessor
{
    /// <summary>
    /// Gets the singleton instance of the values accessor.
    /// </summary>
    public static readonly ValuesAccessor Instance = new ValuesAccessor();
    private ValuesAccessor() { }
}

/// <summary>
/// Accesses all properties.
/// </summary>
public sealed class PropsAccessor : Accessor
{
    /// <summary>
    /// Gets the singleton instance of the properties accessor.
    /// </summary>
    public static readonly PropsAccessor Instance = new PropsAccessor();
    private PropsAccessor() { }
}

