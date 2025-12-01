using System.Diagnostics.CodeAnalysis;

namespace KdlSharp;

/// <summary>
/// Represents a KDL document containing a collection of top-level nodes.
/// </summary>
/// <remarks>
/// <para>
/// A KDL document is a UTF-8 encoded text file containing zero or more nodes.
/// Documents may optionally specify a version using <c>/- kdl-version N</c>.
/// </para>
/// <para>
/// <b>Thread Safety</b>: Read operations on an immutable document are thread-safe.
/// However, the <see cref="Nodes"/> collection is mutable by default.
/// For truly thread-safe reads:
/// <list type="bullet">
///   <item><description>Do not modify the document after construction, OR</description></item>
///   <item><description>Use <see cref="ShallowCopy"/> to get an isolated copy, OR</description></item>
///   <item><description>Use external synchronization (locks) around modifications</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class KdlDocument
{
    /// <summary>
    /// Gets the collection of top-level nodes in this document.
    /// </summary>
    public IList<KdlNode> Nodes { get; }

    /// <summary>
    /// Gets the KDL version for this document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a document is parsed, this property reflects the version used during parsing:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>If <see cref="Settings.KdlParserSettings.TargetVersion"/> is set explicitly, that version is used.</description></item>
    ///   <item><description>Otherwise, <see cref="KdlVersion.V2"/> is used by default.</description></item>
    /// </list>
    /// <para>
    /// When a document is constructed programmatically via <see cref="KdlDocument(KdlVersion)"/>,
    /// the version defaults to <see cref="KdlVersion.V2"/> unless explicitly specified.
    /// </para>
    /// </remarks>
    public KdlVersion Version { get; }

    /// <summary>
    /// Initializes a new empty KDL document.
    /// </summary>
    /// <param name="version">The KDL version for this document. Defaults to <see cref="KdlVersion.V2"/>.</param>
    public KdlDocument(KdlVersion version = KdlVersion.V2)
    {
        Nodes = new List<KdlNode>();
        Version = version;
    }

    // ===== Static Factory Methods =====

    /// <summary>
    /// Parses a KDL document from a string.
    /// </summary>
    /// <param name="kdl">The KDL text to parse.</param>
    /// <param name="settings">Optional parser settings.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="Exceptions.KdlParseException">Thrown when the KDL text is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="kdl"/> is null.</exception>
    /// <example>
    /// <code>
    /// var doc = KdlDocument.Parse(@"
    ///     package ""my-app"" version=""1.0.0"" {
    ///         author ""Alice""
    ///     }
    /// ");
    /// var packageName = doc.Nodes[0].Arguments[0].AsString(); // "my-app"
    /// </code>
    /// </example>
    public static KdlDocument Parse(string kdl, Settings.KdlParserSettings? settings = null)
    {
        if (kdl == null)
        {
            throw new ArgumentNullException(nameof(kdl));
        }
        var parser = new Parsing.KdlParser(settings);
        return parser.Parse(kdl);
    }

    /// <summary>
    /// Attempts to parse a KDL document from a string.
    /// </summary>
    /// <param name="kdl">The KDL text to parse.</param>
    /// <param name="document">The parsed document, or null if parsing failed.</param>
    /// <param name="settings">Optional parser settings.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method follows the standard .NET TryParse pattern and never throws exceptions.
    /// It returns <c>false</c> for null input, malformed KDL, or any other error condition.
    /// </remarks>
    public static bool TryParse(string? kdl, [NotNullWhen(true)] out KdlDocument? document, Settings.KdlParserSettings? settings = null)
    {
        if (kdl == null)
        {
            document = null;
            return false;
        }

        try
        {
            document = Parse(kdl, settings);
            return true;
        }
        catch
        {
            document = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse a KDL document from a string with error details.
    /// </summary>
    /// <param name="kdl">The KDL text to parse.</param>
    /// <param name="document">The parsed document if successful, or null if parsing failed.</param>
    /// <param name="error">The parse exception if parsing failed, or null if successful or for non-parse errors (like null input).</param>
    /// <param name="settings">Optional parser settings.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method follows the standard .NET TryParse pattern and never throws exceptions.
    /// It returns <c>false</c> for null input, malformed KDL, or any other error condition.
    /// The <paramref name="error"/> parameter is populated only for parse errors; it will be null
    /// for other failures such as null input.
    /// </remarks>
    public static bool TryParse(
        string? kdl,
        [NotNullWhen(true)] out KdlDocument? document,
        out Exceptions.KdlParseException? error,
        Settings.KdlParserSettings? settings = null)
    {
        if (kdl == null)
        {
            document = null;
            error = null;
            return false;
        }

        try
        {
            document = Parse(kdl, settings);
            error = null;
            return true;
        }
        catch (Exceptions.KdlParseException ex)
        {
            document = null;
            error = ex;
            return false;
        }
        catch
        {
            document = null;
            error = null;
            return false;
        }
    }

    /// <summary>
    /// Parses a KDL document from a file.
    /// </summary>
    /// <param name="path">The path to the KDL file.</param>
    /// <param name="settings">Optional parser settings.</param>
    /// <returns>The parsed document.</returns>
    public static KdlDocument ParseFile(string path, Settings.KdlParserSettings? settings = null)
    {
        var kdl = File.ReadAllText(path);
        return Parse(kdl, settings);
    }

    /// <summary>
    /// Asynchronously parses a KDL document from a file.
    /// </summary>
    public static async Task<KdlDocument> ParseFileAsync(string path, Settings.KdlParserSettings? settings = null, CancellationToken cancellationToken = default)
    {
        var kdl = await File.ReadAllTextAsync(path, cancellationToken);
        return Parse(kdl, settings);
    }

    /// <summary>
    /// Parses a KDL document from a stream.
    /// </summary>
    public static KdlDocument ParseStream(Stream stream, Settings.KdlParserSettings? settings = null, bool leaveOpen = false)
    {
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096, leaveOpen);
        var kdl = reader.ReadToEnd();
        return Parse(kdl, settings);
    }

    /// <summary>
    /// Asynchronously parses a KDL document from a stream.
    /// </summary>
    public static async Task<KdlDocument> ParseStreamAsync(Stream stream, Settings.KdlParserSettings? settings = null, bool leaveOpen = false, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096, leaveOpen);
        var kdl = await reader.ReadToEndAsync();
        return Parse(kdl, settings);
    }

    // ===== Instance Methods =====

    /// <summary>
    /// Converts this document to a KDL string.
    /// </summary>
    public string ToKdlString(Settings.KdlFormatterSettings? settings = null)
    {
        var formatter = new Formatting.KdlFormatter(settings);
        return formatter.Serialize(this);
    }

    /// <summary>
    /// Saves this document to a file.
    /// </summary>
    public void Save(string path, Settings.KdlFormatterSettings? settings = null)
    {
        var kdl = ToKdlString(settings);
        File.WriteAllText(path, kdl);
    }

    /// <summary>
    /// Asynchronously saves this document to a file.
    /// </summary>
    public async Task SaveAsync(string path, Settings.KdlFormatterSettings? settings = null, CancellationToken cancellationToken = default)
    {
        var kdl = ToKdlString(settings);
        await File.WriteAllTextAsync(path, kdl, cancellationToken);
    }

    /// <summary>
    /// Writes this document to a stream.
    /// </summary>
    public void WriteTo(Stream stream, Settings.KdlFormatterSettings? settings = null, bool leaveOpen = false)
    {
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 4096, leaveOpen);
        writer.Write(ToKdlString(settings));
    }

    /// <summary>
    /// Asynchronously writes this document to a stream.
    /// </summary>
    public async Task WriteToAsync(Stream stream, Settings.KdlFormatterSettings? settings = null, bool leaveOpen = false, CancellationToken cancellationToken = default)
    {
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 4096, leaveOpen);
        await writer.WriteAsync(ToKdlString(settings));
    }

    /// <summary>
    /// Validates this document against a schema.
    /// </summary>
    public ValidationResult ValidateAgainstSchema(Schema.SchemaDocument schema)
    {
        return Schema.KdlSchema.Validate(this, schema);
    }

    /// <summary>
    /// Executes a KDL query against this document.
    /// </summary>
    /// <param name="query">The query string (CSS-selector-like syntax).</param>
    /// <returns>The nodes matching the query.</returns>
    /// <exception cref="Exceptions.KdlQueryException">Thrown when the query is invalid.</exception>
    public IEnumerable<KdlNode> Query(string query)
    {
        return KdlSharp.Query.KdlQuery.Execute(this, query);
    }

    /// <summary>
    /// Creates a deep clone of this document.
    /// </summary>
    public KdlDocument Clone()
    {
        var clone = new KdlDocument(Version);
        foreach (var node in Nodes)
        {
            clone.Nodes.Add(node.Clone());
        }
        return clone;
    }

    /// <summary>
    /// Creates a shallow copy of this document.
    /// </summary>
    /// <remarks>
    /// The returned document is a shallow copy with the same nodes.
    /// Modifications to the document's Nodes collection will not affect the original,
    /// but modifications to individual nodes will be reflected in both.
    /// For true immutability, clone the document first.
    /// </remarks>
    public KdlDocument ShallowCopy()
    {
        var copy = new KdlDocument(Version);
        foreach (var node in Nodes)
        {
            copy.Nodes.Add(node);
        }
        return copy;
    }
}

