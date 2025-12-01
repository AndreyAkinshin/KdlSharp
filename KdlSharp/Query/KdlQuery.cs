namespace KdlSharp.Query;

/// <summary>
/// Provides KDL query functionality (CSS-selector-like syntax).
/// </summary>
public static class KdlQuery
{
    /// <summary>
    /// Executes a query against a document.
    /// </summary>
    public static IEnumerable<KdlNode> Execute(KdlDocument document, string query)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var parsedQuery = QueryParser.Parse(query);
        var evaluator = new QueryEvaluator(document);
        return evaluator.Execute(parsedQuery);
    }

    /// <summary>
    /// Parses a query string into a query object (for reuse).
    /// </summary>
    public static CompiledQuery Compile(string query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var parsedQuery = QueryParser.Parse(query);
        return new CompiledQuery(parsedQuery);
    }
}

/// <summary>
/// Represents a compiled query that can be executed multiple times.
/// </summary>
public sealed class CompiledQuery
{
    private readonly Query query;

    internal CompiledQuery(Query query)
    {
        this.query = query ?? throw new ArgumentNullException(nameof(query));
    }

    /// <summary>
    /// Executes this query against a document.
    /// </summary>
    public IEnumerable<KdlNode> Execute(KdlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var evaluator = new QueryEvaluator(document);
        return evaluator.Execute(query);
    }
}

