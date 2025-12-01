using KdlSharp.Values;

namespace KdlSharp.Query;

/// <summary>
/// Evaluates KDL queries against KDL documents.
/// </summary>
public sealed class QueryEvaluator
{
    private readonly KdlDocument document;

    /// <summary>
    /// Initializes a new query evaluator for the specified document.
    /// </summary>
    /// <param name="document">The document to query.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public QueryEvaluator(KdlDocument document)
    {
        this.document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>
    /// Executes a query and returns matching nodes.
    /// </summary>
    /// <remarks>
    /// Results are returned in document order, preserving duplicates from multiple selectors.
    /// </remarks>
    public IEnumerable<KdlNode> Execute(Query query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        // Use a list to preserve ordering and allow duplicates
        // Per KDL query spec, results should maintain document order
        var results = new List<KdlNode>();

        foreach (var selector in query.Selectors)
        {
            foreach (var node in ExecuteSelector(selector))
            {
                results.Add(node);
            }
        }

        return results;
    }

    private IEnumerable<KdlNode> ExecuteSelector(Selector selector)
    {
        IEnumerable<KdlNode> currentNodes;

        // Handle first segment
        var firstSegment = selector.Segments[0];
        currentNodes = ApplyFilter(firstSegment.Filter, GetInitialNodes(firstSegment.Filter));

        // Handle remaining segments
        for (int i = 1; i < selector.Segments.Count; i++)
        {
            var segment = selector.Segments[i];
            var prevSegment = selector.Segments[i - 1];

            // Apply the operator from the previous segment
            if (prevSegment.Operator.HasValue)
            {
                currentNodes = ApplyOperator(currentNodes, prevSegment.Operator.Value, segment.Filter);
            }
        }

        return currentNodes;
    }

    private IEnumerable<KdlNode> GetInitialNodes(Filter filter)
    {
        if (filter is TopFilter)
        {
            // top() only returns top-level nodes
            return document.Nodes;
        }

        // All other filters start from the document root
        // and can match at any level (they do a deep search)
        return GetAllNodes(document);
    }

    private IEnumerable<KdlNode> GetAllNodes(KdlDocument document)
    {
        foreach (var node in document.Nodes)
        {
            yield return node;
            foreach (var descendant in GetAllDescendants(node))
            {
                yield return descendant;
            }
        }
    }

    private IEnumerable<KdlNode> GetAllDescendants(KdlNode node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var descendant in GetAllDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    private IEnumerable<KdlNode> ApplyFilter(Filter filter, IEnumerable<KdlNode> nodes)
    {
        if (filter is TopFilter)
        {
            return nodes;
        }

        if (filter is MatchersFilter matchersFilter)
        {
            return nodes.Where(node => MatchesFilter(node, matchersFilter));
        }

        return Array.Empty<KdlNode>();
    }

    private IEnumerable<KdlNode> ApplyOperator(
        IEnumerable<KdlNode> nodes,
        SelectorOperator op,
        Filter filter)
    {
        var results = new List<KdlNode>();

        foreach (var node in nodes)
        {
            IEnumerable<KdlNode> candidates = op switch
            {
                SelectorOperator.Child => node.Children,
                SelectorOperator.Descendant => GetAllDescendants(node),
                SelectorOperator.NextSibling => GetNextSibling(node),
                SelectorOperator.FollowingSibling => GetFollowingSiblings(node),
                _ => Array.Empty<KdlNode>()
            };

            results.AddRange(ApplyFilter(filter, candidates));
        }

        return results;
    }

    private IEnumerable<KdlNode> GetNextSibling(KdlNode node)
    {
        var parent = node.Parent;
        if (parent == null)
        {
            // Top-level node - check document
            var index = document.Nodes.IndexOf(node);
            if (index >= 0 && index < document.Nodes.Count - 1)
            {
                yield return document.Nodes[index + 1];
            }
        }
        else
        {
            var index = parent.Children.IndexOf(node);
            if (index >= 0 && index < parent.Children.Count - 1)
            {
                yield return parent.Children[index + 1];
            }
        }
    }

    private IEnumerable<KdlNode> GetFollowingSiblings(KdlNode node)
    {
        var parent = node.Parent;
        if (parent == null)
        {
            // Top-level node - check document
            var index = document.Nodes.IndexOf(node);
            if (index >= 0)
            {
                for (int i = index + 1; i < document.Nodes.Count; i++)
                {
                    yield return document.Nodes[i];
                }
            }
        }
        else
        {
            var index = parent.Children.IndexOf(node);
            if (index >= 0)
            {
                for (int i = index + 1; i < parent.Children.Count; i++)
                {
                    yield return parent.Children[i];
                }
            }
        }
    }

    private bool MatchesFilter(KdlNode node, MatchersFilter filter)
    {
        // Check type matcher
        if (filter.TypeMatcher != null)
        {
            if (!MatchesType(node, filter.TypeMatcher))
            {
                return false;
            }
        }

        // Check node name
        if (filter.NodeName != null)
        {
            if (node.Name != filter.NodeName)
            {
                return false;
            }
        }

        // Check accessor matchers
        foreach (var matcher in filter.AccessorMatchers)
        {
            if (!MatchesAccessorMatcher(node, matcher))
            {
                return false;
            }
        }

        return true;
    }

    private bool MatchesType(KdlNode node, TypeMatcher typeMatcher)
    {
        if (typeMatcher.IsAnyType)
        {
            return node.TypeAnnotation != null;
        }

        return node.TypeAnnotation?.TypeName == typeMatcher.TypeName;
    }

    private bool MatchesAccessorMatcher(KdlNode node, AccessorMatcher matcher)
    {
        // Empty matcher [] always matches
        if (matcher.IsEmpty)
        {
            return true;
        }

        // If only accessor (no comparison), check if the accessed value exists
        if (matcher.Accessor != null && matcher.Comparison == null)
        {
            return AccessorExists(node, matcher.Accessor);
        }

        // If comparison, evaluate it
        if (matcher.Comparison != null)
        {
            return MatchesComparison(node, matcher.Comparison);
        }

        return true;
    }

    private bool AccessorExists(KdlNode node, Accessor accessor)
    {
        return accessor switch
        {
            ValAccessor val => node.Arguments.Count > val.Index,
            PropAccessor prop => node.HasProperty(prop.PropertyName),
            NameAccessor => true,
            TagAccessor => node.TypeAnnotation != null,
            ValuesAccessor => node.Arguments.Count > 0,
            PropsAccessor => node.Properties.Count > 0,
            _ => false
        };
    }

    private bool MatchesComparison(KdlNode node, Comparison comparison)
    {
        // Try to get the raw KdlValue first (for type annotation support)
        var kdlValue = GetAccessorKdlValue(node, comparison.Left);

        // For TypeMatcher comparisons, check type annotation directly
        if (comparison.Right is TypeMatcher typeMatcher)
        {
            if (kdlValue == null)
            {
                return false;
            }
            return comparison.Operator == MatcherOperator.Equal &&
                   kdlValue.TypeAnnotation?.TypeName == typeMatcher.TypeName;
        }

        // For other comparisons, use GetAccessorValue which handles all accessor types
        var leftValue = kdlValue != null ? GetKdlValueAsObject(kdlValue) : GetAccessorValue(node, comparison.Left);
        if (leftValue == null)
        {
            return false;
        }

        var rightValue = comparison.Right;

        return comparison.Operator switch
        {
            MatcherOperator.Equal => ValuesEqual(leftValue, rightValue),
            MatcherOperator.NotEqual => !ValuesEqual(leftValue, rightValue),
            MatcherOperator.GreaterThan => CompareValues(leftValue, rightValue) > 0,
            MatcherOperator.LessThan => CompareValues(leftValue, rightValue) < 0,
            MatcherOperator.GreaterThanOrEqual => CompareValues(leftValue, rightValue) >= 0,
            MatcherOperator.LessThanOrEqual => CompareValues(leftValue, rightValue) <= 0,
            MatcherOperator.StartsWith => StringOp(leftValue, rightValue, (s, p) => s.StartsWith(p)),
            MatcherOperator.EndsWith => StringOp(leftValue, rightValue, (s, p) => s.EndsWith(p)),
            MatcherOperator.Contains => StringOp(leftValue, rightValue, (s, p) => s.Contains(p)),
            _ => false
        };
    }

    private KdlValue? GetAccessorKdlValue(KdlNode node, Accessor accessor)
    {
        return accessor switch
        {
            ValAccessor val => node.Arguments.Count > val.Index ? node.Arguments[val.Index] : null,
            PropAccessor prop => node.GetProperty(prop.PropertyName),
            _ => null // Other accessors don't return single KdlValue
        };
    }

    private object? GetAccessorValue(KdlNode node, Accessor accessor)
    {
        return accessor switch
        {
            ValAccessor val => node.Arguments.Count > val.Index ? GetKdlValueAsObject(node.Arguments[val.Index]) : null,
            PropAccessor prop => node.GetProperty(prop.PropertyName) is KdlValue value ? GetKdlValueAsObject(value) : null,
            NameAccessor => node.Name,
            TagAccessor => node.TypeAnnotation?.TypeName,
            ValuesAccessor => node.Arguments.Select(GetKdlValueAsObject).ToList(),
            PropsAccessor => node.Properties.ToDictionary(p => p.Key, p => GetKdlValueAsObject(p.Value)),
            _ => null
        };
    }

    private object? GetKdlValueAsObject(KdlValue value)
    {
        return value.ValueType switch
        {
            KdlValueType.String => value.AsString(),
            KdlValueType.Number => value.AsNumber(),
            KdlValueType.Boolean => value.AsBoolean(),
            KdlValueType.Null => null,
            _ => null
        };
    }

    private bool ValuesEqual(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        // TypeMatcher comparisons are handled in MatchesComparison now
        if (right is TypeMatcher)
        {
            return false;
        }

        // String comparison
        if (left is string ls && right is string rs)
        {
            return ls == rs;
        }

        // Number comparison
        if (IsNumber(left) && IsNumber(right))
        {
            return ToDecimal(left) == ToDecimal(right);
        }

        // Boolean comparison
        if (left is bool lb && right is bool rb)
        {
            return lb == rb;
        }

        return false;
    }

    private int CompareValues(object? left, object? right)
    {
        if (!IsNumber(left) || !IsNumber(right))
        {
            return 0; // Only numbers can be compared
        }

        var leftNum = ToDecimal(left);
        var rightNum = ToDecimal(right);

        return leftNum.CompareTo(rightNum);
    }

    private bool StringOp(object? left, object? right, Func<string, string, bool> op)
    {
        if (left is not string ls || right is not string rs)
        {
            return false;
        }

        return op(ls, rs);
    }

    private bool IsNumber(object? value)
    {
        return value is decimal or int or long or double or float;
    }

    private decimal ToDecimal(object? value)
    {
        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double db => (decimal)db,
            float f => (decimal)f,
            _ => 0
        };
    }
}

