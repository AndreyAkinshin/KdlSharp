namespace KdlSharp.Schema;

/// <summary>
/// Provides schema-related operations for KDL documents.
/// </summary>
public static class KdlSchema
{
    /// <summary>
    /// Parses a schema from a KDL string.
    /// </summary>
    public static SchemaDocument Parse(string kdl)
    {
        if (kdl == null)
            throw new ArgumentNullException(nameof(kdl));

        return SchemaParser.Parse(kdl);
    }

    /// <summary>
    /// Parses a schema from a file.
    /// </summary>
    public static SchemaDocument ParseFile(string path)
    {
        var kdl = File.ReadAllText(path);
        return Parse(kdl);
    }

    /// <summary>
    /// Validates a document against a schema.
    /// </summary>
    public static ValidationResult Validate(KdlDocument document, SchemaDocument schema)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        var validator = new SchemaValidator(schema);
        return validator.Validate(document);
    }

    /// <summary>
    /// Creates a basic permissive schema that allows any structure.
    /// </summary>
    public static SchemaDocument CreatePermissiveSchema()
    {
        var info = new SchemaInfo { Title = "Permissive Schema", Description = "Allows any KDL structure" };
        return new SchemaDocument(info, otherNodesAllowed: true, otherTagsAllowed: true);
    }
}

