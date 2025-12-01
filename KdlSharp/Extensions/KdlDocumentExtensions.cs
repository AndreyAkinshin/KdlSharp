namespace KdlSharp.Extensions;

/// <summary>
/// Extension methods for <see cref="KdlDocument"/>.
/// </summary>
public static class KdlDocumentExtensions
{
    /// <summary>
    /// Finds all top-level nodes with the specified name.
    /// </summary>
    public static IEnumerable<KdlNode> FindNodes(this KdlDocument document, string name)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (name == null) throw new ArgumentNullException(nameof(name));

        return document.Nodes.Where(n => n.Name == name);
    }

    /// <summary>
    /// Finds the first top-level node with the specified name.
    /// </summary>
    public static KdlNode? FindNode(this KdlDocument document, string name)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (name == null) throw new ArgumentNullException(nameof(name));

        return document.Nodes.FirstOrDefault(n => n.Name == name);
    }

    /// <summary>
    /// Recursively enumerates all nodes in the document.
    /// </summary>
    public static IEnumerable<KdlNode> AllNodes(this KdlDocument document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));

        foreach (var node in document.Nodes)
        {
            yield return node;
            foreach (var descendant in node.Descendants())
            {
                yield return descendant;
            }
        }
    }
}

