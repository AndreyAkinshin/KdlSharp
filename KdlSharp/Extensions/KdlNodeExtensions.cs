namespace KdlSharp.Extensions;

/// <summary>
/// Extension methods for <see cref="KdlNode"/>.
/// </summary>
public static class KdlNodeExtensions
{
    /// <summary>
    /// Finds all descendant nodes with the specified name.
    /// </summary>
    public static IEnumerable<KdlNode> FindNodes(this KdlNode node, string name)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (name == null) throw new ArgumentNullException(nameof(name));

        return node.Descendants().Where(n => n.Name == name);
    }

    /// <summary>
    /// Finds the first descendant node with the specified name.
    /// </summary>
    public static KdlNode? FindNode(this KdlNode node, string name)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (name == null) throw new ArgumentNullException(nameof(name));

        return node.Descendants().FirstOrDefault(n => n.Name == name);
    }

    /// <summary>
    /// Gets the value of a property, or a default value if not found.
    /// </summary>
    /// <remarks>
    /// If duplicate keys exist, returns the last matching property (rightmost takes precedence).
    /// </remarks>
    public static T? GetPropertyValue<T>(this KdlNode node, string key, T? defaultValue = default)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (key == null) throw new ArgumentNullException(nameof(key));

        var value = node.GetProperty(key);
        if (value == null)
            return defaultValue;

        return ConvertValue<T>(value) ?? defaultValue;
    }

    /// <summary>
    /// Checks whether the node has a property with the specified key.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <param name="key">The property key to look for.</param>
    /// <returns><c>true</c> if the node has a property with the specified key; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> or <paramref name="key"/> is null.</exception>
    /// <example>
    /// <code>
    /// if (node.HasProperty("enabled"))
    /// {
    ///     // Property exists
    /// }
    /// </code>
    /// </example>
    public static bool HasProperty(this KdlNode node, string key)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (key == null) throw new ArgumentNullException(nameof(key));

        return node.GetProperty(key) != null;
    }

    /// <summary>
    /// Gets the first argument value, or a default value if no arguments.
    /// </summary>
    public static T? GetArgumentValue<T>(this KdlNode node, int index = 0, T? defaultValue = default)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        if (index < 0 || index >= node.Arguments.Count)
            return defaultValue;

        return ConvertValue<T>(node.Arguments[index]) ?? defaultValue;
    }

    /// <summary>
    /// Recursively enumerates all descendant nodes.
    /// </summary>
    public static IEnumerable<KdlNode> Descendants(this KdlNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var descendant in child.Descendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Enumerates ancestor nodes up to the root.
    /// </summary>
    public static IEnumerable<KdlNode> Ancestors(this KdlNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        var current = node.Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    private static T? ConvertValue<T>(KdlValue value)
    {
        if (value.IsNull())
            return default;

        var targetType = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle strings
        if (value.ValueType == KdlValueType.String)
        {
            var str = value.AsString();
            if (underlyingType == typeof(string))
                return (T)(object)str!;

            // Try to parse other types from string
            if (underlyingType == typeof(int) && int.TryParse(str, out var intVal))
                return (T)(object)intVal;
            if (underlyingType == typeof(long) && long.TryParse(str, out var longVal))
                return (T)(object)longVal;
            if (underlyingType == typeof(bool) && bool.TryParse(str, out var boolVal))
                return (T)(object)boolVal;
        }

        // Handle numbers
        if (value.ValueType == KdlValueType.Number)
        {
            var kdlNumber = value as Values.KdlNumber;
            if (kdlNumber != null && kdlNumber.IsSpecial)
            {
                // Handle special values (infinity and NaN) - only valid for double/float
                var doubleValue = kdlNumber.AsDoubleValue();
                if (doubleValue.HasValue)
                {
                    if (underlyingType == typeof(double))
                        return (T)(object)doubleValue.Value;
                    if (underlyingType == typeof(float))
                        return (T)(object)(float)doubleValue.Value;
                }
                // Special numbers cannot be converted to int, long, or decimal - return default
                return default;
            }

            var num = value.AsNumber()!.Value;
            if (underlyingType == typeof(int))
                return (T)(object)(int)num;
            if (underlyingType == typeof(long))
                return (T)(object)(long)num;
            if (underlyingType == typeof(decimal))
                return (T)(object)num;
            if (underlyingType == typeof(double))
                return (T)(object)(double)num;
            if (underlyingType == typeof(float))
                return (T)(object)(float)num;
        }

        // Handle booleans
        if (value.ValueType == KdlValueType.Boolean)
        {
            if (underlyingType == typeof(bool))
                return (T)(object)value.AsBoolean()!.Value;
        }

        return default;
    }
}

