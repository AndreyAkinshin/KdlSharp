namespace KdlSharp;

/// <summary>
/// Represents a KDL node with a name, optional type annotation, arguments, properties, and children.
/// </summary>
/// <remarks>
/// <para>
/// A node consists of:
/// <list type="bullet">
///   <item><description>Name (required): An identifier or string</description></item>
///   <item><description>Type annotation (optional): A type hint like <c>(string)</c></description></item>
///   <item><description>Arguments (optional): Ordered positional values</description></item>
///   <item><description>Properties (optional): Ordered key-value pairs</description></item>
///   <item><description>Children (optional): Nested nodes in a block</description></item>
/// </list>
/// </para>
/// <para>
/// This class is not thread-safe for modifications.
/// </para>
/// </remarks>
public sealed class KdlNode
{
    private string name;

    /// <summary>
    /// Gets or sets the name of this node.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when setting a null name.</exception>
    public string Name
    {
        get => name;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            name = value;
        }
    }

    /// <summary>
    /// Gets or sets the type annotation for this node, or <c>null</c> if none.
    /// </summary>
    public KdlAnnotation? TypeAnnotation { get; set; }

    /// <summary>
    /// Gets the ordered collection of argument values.
    /// </summary>
    public IList<KdlValue> Arguments { get; }

    /// <summary>
    /// Gets the ordered collection of properties (key-value pairs).
    /// </summary>
    /// <remarks>
    /// Per KDL specification, duplicate keys are allowed, and insertion order is preserved.
    /// Use <see cref="GetProperty"/> to retrieve a specific property by key (returns last matching value).
    /// </remarks>
    public IList<KdlProperty> Properties { get; }

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    public IList<KdlNode> Children { get; }

    /// <summary>
    /// Gets whether this node has any children.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Gets the parent node, or <c>null</c> if this is a root node.
    /// </summary>
    /// <remarks>
    /// This property is set automatically when a node is added to another node's <see cref="Children"/> collection.
    /// </remarks>
    public KdlNode? Parent { get; internal set; }

    /// <summary>
    /// Gets the source position where this node was parsed, or <c>null</c> if constructed programmatically.
    /// </summary>
    public SourcePosition? SourcePosition { get; internal set; }

    /// <summary>
    /// Initializes a new node with the specified name.
    /// </summary>
    /// <param name="name">The node name.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Create a simple node
    /// var node = new KdlNode("server")
    ///     .AddProperty("host", "localhost")
    ///     .AddProperty("port", 8080)
    ///     .AddChild(new KdlNode("database")
    ///         .AddProperty("name", "mydb"));
    /// </code>
    /// </example>
    public KdlNode(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }
        this.name = name;
        Arguments = new List<KdlValue>();
        Properties = new List<KdlProperty>();
        Children = new List<KdlNode>();
    }

    /// <summary>
    /// Initializes a new node with a name and type annotation.
    /// </summary>
    public KdlNode(string name, KdlAnnotation typeAnnotation) : this(name)
    {
        TypeAnnotation = typeAnnotation;
    }

    // ===== Fluent API Methods =====

    /// <summary>
    /// Adds an argument to this node.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>This node, for method chaining.</returns>
    public KdlNode AddArgument(KdlValue value)
    {
        Arguments.Add(value ?? throw new ArgumentNullException(nameof(value)));
        return this;
    }

    /// <summary>
    /// Adds multiple arguments to this node.
    /// </summary>
    public KdlNode AddArguments(params KdlValue[] values)
    {
        foreach (var value in values)
        {
            AddArgument(value);
        }
        return this;
    }

    /// <summary>
    /// Adds a property to this node.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This node, for method chaining.</returns>
    /// <remarks>
    /// If the key already exists, this adds a duplicate property.
    /// Use <see cref="SetProperty"/> to replace an existing property.
    /// </remarks>
    public KdlNode AddProperty(string key, KdlValue value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        Properties.Add(new KdlProperty(key, value ?? throw new ArgumentNullException(nameof(value))));
        return this;
    }

    /// <summary>
    /// Sets a property value, replacing any existing property with the same key.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This node, for method chaining.</returns>
    public KdlNode SetProperty(string key, KdlValue value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        // Remove all existing properties with this key
        for (int i = Properties.Count - 1; i >= 0; i--)
        {
            if (Properties[i].Key == key)
            {
                Properties.RemoveAt(i);
            }
        }

        Properties.Add(new KdlProperty(key, value));
        return this;
    }

    /// <summary>
    /// Gets the value of a property by key (returns the last matching property).
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or null if not found.</returns>
    public KdlValue? GetProperty(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // Return the last matching property (KDL spec: rightmost takes precedence)
        for (int i = Properties.Count - 1; i >= 0; i--)
        {
            if (Properties[i].Key == key)
            {
                return Properties[i].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all properties with the specified key.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>An enumerable of all matching properties.</returns>
    public IEnumerable<KdlValue> GetAllProperties(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return Properties.Where(p => p.Key == key).Select(p => p.Value);
    }

    /// <summary>
    /// Checks if a property with the specified key exists.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>True if the property exists, false otherwise.</returns>
    public bool HasProperty(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return Properties.Any(p => p.Key == key);
    }

    /// <summary>
    /// Adds a child node. If the child already belongs to another parent, it is removed from
    /// that parent first.
    /// </summary>
    /// <param name="child">The child node to add.</param>
    /// <returns>This node, for method chaining.</returns>
    public KdlNode AddChild(KdlNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }
        if (child == this)
        {
            throw new InvalidOperationException("A node cannot be added as its own child.");
        }
        child.Parent?.Children.Remove(child);
        child.Parent = this;
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// Removes a child node by reference.
    /// </summary>
    /// <param name="child">The child node to remove.</param>
    /// <returns><c>true</c> if the child was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveChild(KdlNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }
        if (Children.Remove(child))
        {
            child.Parent = null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes all properties with the specified key.
    /// </summary>
    /// <param name="key">The property key to remove.</param>
    /// <returns><c>true</c> if any properties were removed; otherwise, <c>false</c>.</returns>
    public bool RemoveProperty(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        bool removed = false;
        for (int i = Properties.Count - 1; i >= 0; i--)
        {
            if (Properties[i].Key == key)
            {
                Properties.RemoveAt(i);
                removed = true;
            }
        }
        return removed;
    }

    /// <summary>
    /// Removes a specific property by reference.
    /// </summary>
    /// <param name="property">The property to remove.</param>
    /// <returns><c>true</c> if the property was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveProperty(KdlProperty property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }
        return Properties.Remove(property);
    }

    /// <summary>
    /// Removes an argument by reference.
    /// </summary>
    /// <param name="argument">The argument value to remove.</param>
    /// <returns><c>true</c> if the argument was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveArgument(KdlValue argument)
    {
        if (argument == null)
        {
            throw new ArgumentNullException(nameof(argument));
        }
        return Arguments.Remove(argument);
    }

    /// <summary>
    /// Adds multiple child nodes.
    /// </summary>
    public KdlNode AddChildren(params KdlNode[] children)
    {
        foreach (var child in children)
        {
            AddChild(child);
        }
        return this;
    }

    // ===== Utility Methods =====

    /// <summary>
    /// Converts this node to a KDL string.
    /// </summary>
    /// <param name="settings">Optional serializer settings.</param>
    /// <returns>The KDL string representation of this node.</returns>
    public string ToKdlString(Settings.KdlFormatterSettings? settings = null)
    {
        var formatter = new Formatting.KdlFormatter(settings);
        var doc = new KdlDocument();
        doc.Nodes.Add(this);
        return formatter.Serialize(doc);
    }

    /// <summary>
    /// Creates a deep clone of this node and all its children.
    /// </summary>
    public KdlNode Clone()
    {
        var clone = new KdlNode(Name)
        {
            TypeAnnotation = TypeAnnotation
        };

        foreach (var arg in Arguments)
        {
            clone.Arguments.Add(arg.Clone());
        }

        foreach (var prop in Properties)
        {
            clone.Properties.Add(new KdlProperty(prop.Key, prop.Value.Clone()));
        }

        foreach (var child in Children)
        {
            clone.AddChild(child.Clone());
        }

        return clone;
    }

    /// <summary>
    /// Returns a string representation of this node (for debugging).
    /// </summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        if (TypeAnnotation != null)
        {
            sb.Append(TypeAnnotation);
        }
        sb.Append(Name);
        if (Arguments.Count > 0 || Properties.Count > 0)
        {
            sb.Append(" ...");
        }
        if (HasChildren)
        {
            sb.Append(" {...}");
        }
        return sb.ToString();
    }
}

