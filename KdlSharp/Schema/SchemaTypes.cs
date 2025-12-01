namespace KdlSharp.Schema;

/// <summary>
/// Represents a complete KDL schema document.
/// </summary>
public sealed class SchemaDocument
{
    /// <summary>
    /// Schema metadata (title, description, version, etc.).
    /// </summary>
    public SchemaInfo Info { get; }

    /// <summary>
    /// Top-level node definitions.
    /// </summary>
    public IReadOnlyList<SchemaNode> Nodes { get; }

    /// <summary>
    /// Reusable definition fragments.
    /// </summary>
    public SchemaDefinitions? Definitions { get; }

    /// <summary>
    /// Validations to apply to node names.
    /// </summary>
    public IReadOnlyList<ValidationRule> NodeNameValidations { get; }

    /// <summary>
    /// Whether to allow nodes other than explicitly listed ones.
    /// </summary>
    public bool OtherNodesAllowed { get; }

    /// <summary>
    /// Tag-scoped validation rules.
    /// </summary>
    public IReadOnlyList<SchemaTag> Tags { get; }

    /// <summary>
    /// Validations to apply to tag names.
    /// </summary>
    public IReadOnlyList<ValidationRule> TagNameValidations { get; }

    /// <summary>
    /// Whether to allow tags other than explicitly listed ones.
    /// </summary>
    public bool OtherTagsAllowed { get; }

    /// <summary>
    /// Initializes a new schema document.
    /// </summary>
    /// <param name="info">The schema metadata.</param>
    /// <param name="nodes">The top-level node definitions.</param>
    /// <param name="definitions">The reusable definition fragments.</param>
    /// <param name="nodeNameValidations">The node name validation rules.</param>
    /// <param name="otherNodesAllowed">Whether to allow unlisted nodes.</param>
    /// <param name="tags">The tag-scoped validation rules.</param>
    /// <param name="tagNameValidations">The tag name validation rules.</param>
    /// <param name="otherTagsAllowed">Whether to allow unlisted tags.</param>
    /// <exception cref="ArgumentNullException"><paramref name="info"/> is null.</exception>
    public SchemaDocument(
        SchemaInfo info,
        IReadOnlyList<SchemaNode>? nodes = null,
        SchemaDefinitions? definitions = null,
        IReadOnlyList<ValidationRule>? nodeNameValidations = null,
        bool otherNodesAllowed = false,
        IReadOnlyList<SchemaTag>? tags = null,
        IReadOnlyList<ValidationRule>? tagNameValidations = null,
        bool otherTagsAllowed = false)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));
        Nodes = nodes ?? Array.Empty<SchemaNode>();
        Definitions = definitions;
        NodeNameValidations = nodeNameValidations ?? Array.Empty<ValidationRule>();
        OtherNodesAllowed = otherNodesAllowed;
        Tags = tags ?? Array.Empty<SchemaTag>();
        TagNameValidations = tagNameValidations ?? Array.Empty<ValidationRule>();
        OtherTagsAllowed = otherTagsAllowed;
    }
}

/// <summary>
/// Schema metadata information.
/// </summary>
public sealed class SchemaInfo
{
    /// <summary>
    /// Gets or sets the schema title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the schema description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of authors.
    /// </summary>
    public IReadOnlyList<string> Authors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the list of contributors.
    /// </summary>
    public IReadOnlyList<string> Contributors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the list of documentation links.
    /// </summary>
    public IReadOnlyList<SchemaLink> Links { get; set; } = Array.Empty<SchemaLink>();

    /// <summary>
    /// Gets or sets the license.
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the publication date.
    /// </summary>
    public DateTime? Published { get; set; }

    /// <summary>
    /// Gets or sets the last modification date.
    /// </summary>
    public DateTime? Modified { get; set; }
}

/// <summary>
/// Schema link (for documentation, etc.).
/// </summary>
public sealed class SchemaLink
{
    /// <summary>
    /// Gets the URL.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the link relationship type.
    /// </summary>
    public string? Rel { get; }

    /// <summary>
    /// Gets the language code.
    /// </summary>
    public string? Lang { get; }

    /// <summary>
    /// Initializes a new schema link.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="rel">The link relationship type.</param>
    /// <param name="lang">The language code.</param>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is null.</exception>
    public SchemaLink(string url, string? rel = null, string? lang = null)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Rel = rel;
        Lang = lang;
    }
}

/// <summary>
/// Represents a schema node definition.
/// </summary>
public sealed class SchemaNode
{
    /// <summary>
    /// Node name (if specified).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Description of this node.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Identifier for referencing this definition.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// Value definitions.
    /// </summary>
    public SchemaValue? Values { get; }

    /// <summary>
    /// Property definitions.
    /// </summary>
    public IReadOnlyList<SchemaProperty> Properties { get; }

    /// <summary>
    /// Child node definitions.
    /// </summary>
    public SchemaChildren? Children { get; }

    /// <summary>
    /// Validation rules for this node.
    /// </summary>
    public IReadOnlyList<ValidationRule> ValidationRules { get; }

    /// <summary>
    /// Initializes a new schema node.
    /// </summary>
    /// <param name="name">The node name.</param>
    /// <param name="description">The node description.</param>
    /// <param name="id">The identifier for referencing.</param>
    /// <param name="values">The value definitions.</param>
    /// <param name="properties">The property definitions.</param>
    /// <param name="children">The child node definitions.</param>
    /// <param name="validationRules">The validation rules.</param>
    public SchemaNode(
        string? name = null,
        string? description = null,
        string? id = null,
        SchemaValue? values = null,
        IReadOnlyList<SchemaProperty>? properties = null,
        SchemaChildren? children = null,
        IReadOnlyList<ValidationRule>? validationRules = null)
    {
        Name = name;
        Description = description;
        Id = id;
        Values = values;
        Properties = properties ?? Array.Empty<SchemaProperty>();
        Children = children;
        ValidationRules = validationRules ?? Array.Empty<ValidationRule>();
    }
}

/// <summary>
/// Represents a schema property definition.
/// </summary>
public sealed class SchemaProperty
{
    /// <summary>
    /// Property key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Whether this property is required.
    /// </summary>
    public bool Required { get; }

    /// <summary>
    /// Description of this property.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Validation rules for this property.
    /// </summary>
    public IReadOnlyList<ValidationRule> ValidationRules { get; }

    /// <summary>
    /// Initializes a new schema property.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="required">Whether the property is required.</param>
    /// <param name="description">The property description.</param>
    /// <param name="validationRules">The validation rules.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    public SchemaProperty(
        string key,
        bool required = false,
        string? description = null,
        IReadOnlyList<ValidationRule>? validationRules = null)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Required = required;
        Description = description;
        ValidationRules = validationRules ?? Array.Empty<ValidationRule>();
    }
}

/// <summary>
/// Represents a schema value definition.
/// </summary>
public sealed class SchemaValue
{
    /// <summary>
    /// Minimum number of values.
    /// </summary>
    public int? Min { get; }

    /// <summary>
    /// Maximum number of values.
    /// </summary>
    public int? Max { get; }

    /// <summary>
    /// Description of values.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Validation rules for values.
    /// </summary>
    public IReadOnlyList<ValidationRule> ValidationRules { get; }

    /// <summary>
    /// Initializes a new schema value definition.
    /// </summary>
    /// <param name="min">The minimum number of values.</param>
    /// <param name="max">The maximum number of values.</param>
    /// <param name="description">The value description.</param>
    /// <param name="validationRules">The validation rules.</param>
    public SchemaValue(
        int? min = null,
        int? max = null,
        string? description = null,
        IReadOnlyList<ValidationRule>? validationRules = null)
    {
        Min = min;
        Max = max;
        Description = description;
        ValidationRules = validationRules ?? Array.Empty<ValidationRule>();
    }
}

/// <summary>
/// Represents schema children definitions.
/// </summary>
public sealed class SchemaChildren
{
    /// <summary>
    /// Child node definitions.
    /// </summary>
    public IReadOnlyList<SchemaNode> Nodes { get; }

    /// <summary>
    /// Validations for child node names.
    /// </summary>
    public IReadOnlyList<ValidationRule> NodeNameValidations { get; }

    /// <summary>
    /// Whether to allow child nodes other than explicitly listed ones.
    /// </summary>
    public bool OtherNodesAllowed { get; }

    /// <summary>
    /// Initializes a new schema children definition.
    /// </summary>
    /// <param name="nodes">The child node definitions.</param>
    /// <param name="nodeNameValidations">The validation rules for child node names.</param>
    /// <param name="otherNodesAllowed">Whether to allow unlisted child nodes.</param>
    public SchemaChildren(
        IReadOnlyList<SchemaNode>? nodes = null,
        IReadOnlyList<ValidationRule>? nodeNameValidations = null,
        bool otherNodesAllowed = false)
    {
        Nodes = nodes ?? Array.Empty<SchemaNode>();
        NodeNameValidations = nodeNameValidations ?? Array.Empty<ValidationRule>();
        OtherNodesAllowed = otherNodesAllowed;
    }
}

/// <summary>
/// Reusable definition fragments.
/// </summary>
public sealed class SchemaDefinitions
{
    private readonly Dictionary<string, SchemaNode> nodeDefinitions = new();
    private readonly Dictionary<string, SchemaProperty> propertyDefinitions = new();
    private readonly Dictionary<string, SchemaValue> valueDefinitions = new();
    private readonly Dictionary<string, SchemaChildren> childrenDefinitions = new();

    /// <summary>
    /// Gets the dictionary of node definitions.
    /// </summary>
    public IReadOnlyDictionary<string, SchemaNode> NodeDefinitions => nodeDefinitions;

    /// <summary>
    /// Gets the dictionary of property definitions.
    /// </summary>
    public IReadOnlyDictionary<string, SchemaProperty> PropertyDefinitions => propertyDefinitions;

    /// <summary>
    /// Gets the dictionary of value definitions.
    /// </summary>
    public IReadOnlyDictionary<string, SchemaValue> ValueDefinitions => valueDefinitions;

    /// <summary>
    /// Gets the dictionary of children definitions.
    /// </summary>
    public IReadOnlyDictionary<string, SchemaChildren> ChildrenDefinitions => childrenDefinitions;

    /// <summary>
    /// Adds a node definition with the specified ID.
    /// </summary>
    /// <param name="id">The definition ID.</param>
    /// <param name="node">The node definition.</param>
    public void AddNodeDefinition(string id, SchemaNode node)
    {
        nodeDefinitions[id] = node;
    }

    /// <summary>
    /// Adds a property definition with the specified ID.
    /// </summary>
    /// <param name="id">The definition ID.</param>
    /// <param name="property">The property definition.</param>
    public void AddPropertyDefinition(string id, SchemaProperty property)
    {
        propertyDefinitions[id] = property;
    }

    /// <summary>
    /// Adds a value definition with the specified ID.
    /// </summary>
    /// <param name="id">The definition ID.</param>
    /// <param name="value">The value definition.</param>
    public void AddValueDefinition(string id, SchemaValue value)
    {
        valueDefinitions[id] = value;
    }

    /// <summary>
    /// Adds a children definition with the specified ID.
    /// </summary>
    /// <param name="id">The definition ID.</param>
    /// <param name="children">The children definition.</param>
    public void AddChildrenDefinition(string id, SchemaChildren children)
    {
        childrenDefinitions[id] = children;
    }
}

/// <summary>
/// Tag-scoped validation rules.
/// </summary>
public sealed class SchemaTag
{
    /// <summary>
    /// Tag name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Node definition for tagged nodes.
    /// </summary>
    public SchemaNode NodeDefinition { get; }

    /// <summary>
    /// Initializes a new schema tag.
    /// </summary>
    /// <param name="name">The tag name.</param>
    /// <param name="nodeDefinition">The node definition for tagged nodes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="nodeDefinition"/> is null.</exception>
    public SchemaTag(string name, SchemaNode nodeDefinition)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        NodeDefinition = nodeDefinition ?? throw new ArgumentNullException(nameof(nodeDefinition));
    }
}

