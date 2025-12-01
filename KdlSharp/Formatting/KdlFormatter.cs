using KdlSharp.Settings;
using KdlSharp.Values;

namespace KdlSharp.Formatting;

/// <summary>
/// Provides methods for serializing KDL documents to strings.
/// </summary>
public sealed class KdlFormatter
{
    private readonly KdlFormatterSettings settings;

    /// <summary>
    /// Initializes a new formatter with optional settings.
    /// </summary>
    public KdlFormatter(KdlFormatterSettings? settings = null)
    {
        this.settings = settings ?? new KdlFormatterSettings();
    }

    /// <summary>
    /// Serializes a document to a KDL string.
    /// </summary>
    public string Serialize(KdlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var writer = new StringWriter();
        using var kdlWriter = new KdlWriter(writer, settings);

        // Write version marker if enabled
        if (settings.IncludeVersionMarker)
        {
            kdlWriter.WriteVersionMarker();
        }

        foreach (var node in document.Nodes)
        {
            kdlWriter.WriteNode(node);
        }

        return writer.ToString();
    }

    /// <summary>
    /// Serializes a document to a file.
    /// </summary>
    public void SerializeToFile(KdlDocument document, string path)
    {
        var kdl = Serialize(document);
        File.WriteAllText(path, kdl);
    }

    /// <summary>
    /// Asynchronously serializes a document to a file.
    /// </summary>
    public async Task SerializeToFileAsync(KdlDocument document, string path, CancellationToken cancellationToken = default)
    {
        var kdl = Serialize(document);
        await File.WriteAllTextAsync(path, kdl, cancellationToken);
    }

    /// <summary>
    /// Serializes a document to a stream.
    /// </summary>
    public void SerializeToStream(KdlDocument document, Stream stream, bool leaveOpen = false)
    {
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 4096, leaveOpen);
        writer.Write(Serialize(document));
    }

    /// <summary>
    /// Asynchronously serializes a document to a stream.
    /// </summary>
    public async Task SerializeToStreamAsync(KdlDocument document, Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
    {
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 4096, leaveOpen);
        await writer.WriteAsync(Serialize(document));
    }
}

