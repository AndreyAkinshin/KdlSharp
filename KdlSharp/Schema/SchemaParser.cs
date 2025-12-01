using KdlSharp.Exceptions;
using KdlSharp.Query;
using KdlSharp.Schema.Rules;

namespace KdlSharp.Schema;

/// <summary>
/// Parses KDL schema documents into SchemaDocument objects.
/// </summary>
internal sealed class SchemaParser
{
    private readonly KdlDocument sourceDocument;
    private readonly HashSet<string> resolutionStack = new();
    private readonly Dictionary<SchemaNode, KdlNode> nodeToSource = new();
    private readonly Dictionary<SchemaProperty, KdlNode> propertyToSource = new();
    private readonly Dictionary<SchemaValue, KdlNode> valueToSource = new();
    private readonly Dictionary<SchemaChildren, KdlNode> childrenToSource = new();

    private SchemaParser(KdlDocument sourceDocument)
    {
        this.sourceDocument = sourceDocument;
    }

    /// <summary>
    /// Parses a KDL schema document from a string.
    /// </summary>
    public static SchemaDocument Parse(string kdl)
    {
        var doc = KdlDocument.Parse(kdl);
        var parser = new SchemaParser(doc);
        return parser.ParseDocument(doc);
    }

    private SchemaDocument ParseDocument(KdlDocument doc)
    {
        // Find the 'document' root node
        var documentNode = doc.Nodes.FirstOrDefault(n => n.Name == "document");
        if (documentNode == null)
        {
            throw new KdlSchemaException(
                "Schema must have a top-level 'document' node");
        }

        // Parse info node
        var infoNode = documentNode.Children.FirstOrDefault(n => n.Name == "info");
        var info = infoNode != null ? ParseInfo(infoNode) : new SchemaInfo();

        // Parse node definitions
        var nodeDefinitions = documentNode.Children
            .Where(n => n.Name == "node")
            .Select(ParseSchemaNode)
            .ToList();

        // Parse definitions
        var definitionsNode = documentNode.Children.FirstOrDefault(n => n.Name == "definitions");
        var definitions = definitionsNode != null ? ParseDefinitions(definitionsNode) : null;

        // Parse other-nodes-allowed
        var otherNodesAllowed = documentNode.Children
            .Where(n => n.Name == "other-nodes-allowed")
            .FirstOrDefault()?.Arguments.FirstOrDefault()?.AsBoolean() ?? false;

        // Parse other-tags-allowed
        var otherTagsAllowed = documentNode.Children
            .Where(n => n.Name == "other-tags-allowed")
            .FirstOrDefault()?.Arguments.FirstOrDefault()?.AsBoolean() ?? false;

        // Parse tag definitions
        var tags = documentNode.Children
            .Where(n => n.Name == "tag")
            .Select(ParseTag)
            .ToList();

        // Parse node-names validations
        var nodeNamesNode = documentNode.Children.FirstOrDefault(n => n.Name == "node-names");
        var nodeNameValidations = nodeNamesNode != null
            ? ParseValidationRules((IReadOnlyList<KdlNode>)nodeNamesNode.Children)
            : Array.Empty<ValidationRule>();

        // Parse tag-names validations
        var tagNamesNode = documentNode.Children.FirstOrDefault(n => n.Name == "tag-names");
        var tagNameValidations = tagNamesNode != null
            ? ParseValidationRules((IReadOnlyList<KdlNode>)tagNamesNode.Children)
            : Array.Empty<ValidationRule>();

        var schema = new SchemaDocument(
            info,
            nodeDefinitions,
            definitions,
            nodeNameValidations,
            otherNodesAllowed,
            tags,
            tagNameValidations,
            otherTagsAllowed);

        // Resolve all references
        ResolveReferences(schema, documentNode);

        return schema;
    }

    /// <summary>
    /// Resolves all 'ref' attributes in the schema by executing KDL queries
    /// and merging the referenced definitions.
    /// </summary>
    private void ResolveReferences(SchemaDocument schema, KdlNode documentNode)
    {
        // Resolve references in node definitions
        foreach (var node in schema.Nodes)
        {
            ResolveNodeReferences(node, schema);
        }

        // Resolve references in tags
        foreach (var tag in schema.Tags)
        {
            ResolveNodeReferences(tag.NodeDefinition, schema);
        }
    }

    private void ResolveNodeReferences(SchemaNode node, SchemaDocument schema)
    {
        // Check for ref in the node's source
        if (nodeToSource.TryGetValue(node, out var sourceNode))
        {
            var refQuery = sourceNode.GetProperty("ref")?.AsString();
            if (refQuery != null)
            {
                // Detect circular references
                if (resolutionStack.Contains(refQuery))
                {
                    throw new KdlSchemaException(
                        $"Circular reference detected: {refQuery}");
                }

                resolutionStack.Add(refQuery);

                try
                {
                    // Execute the query to find the referenced node in the schema document
                    var referencedNodes = KdlQuery.Execute(sourceDocument, refQuery);
                    var referencedKdlNode = referencedNodes.FirstOrDefault();

                    if (referencedKdlNode != null)
                    {
                        // Parse the referenced node as a schema node and merge
                        var referencedSchemaNode = ParseSchemaNode(referencedKdlNode);

                        // Recursively resolve references in the referenced node first
                        ResolveNodeReferences(referencedSchemaNode, schema);

                        // Merge the referenced node into the current node
                        MergeSchemaNode(node, referencedSchemaNode);
                    }
                    else
                    {
                        // Try to find in definitions by id
                        var targetId = ExtractIdFromQuery(refQuery);
                        if (targetId != null && schema.Definitions != null)
                        {
                            if (schema.Definitions.NodeDefinitions.TryGetValue(targetId, out var defNode))
                            {
                                ResolveNodeReferences(defNode, schema);
                                MergeSchemaNode(node, defNode);
                            }
                        }
                    }
                }
                finally
                {
                    resolutionStack.Remove(refQuery);
                }
            }
        }

        // Recursively resolve references in children
        if (node.Children != null)
        {
            ResolveChildrenReferences(node.Children, schema);
            foreach (var childNode in node.Children.Nodes)
            {
                ResolveNodeReferences(childNode, schema);
            }
        }

        // Resolve references in properties
        foreach (var prop in node.Properties)
        {
            ResolvePropertyReferences(prop, schema);
        }

        // Resolve references in values
        if (node.Values != null)
        {
            ResolveValueReferences(node.Values, schema);
        }
    }

    private void ResolvePropertyReferences(SchemaProperty prop, SchemaDocument schema)
    {
        if (!propertyToSource.TryGetValue(prop, out var sourceNode))
            return;

        var refQuery = sourceNode.GetProperty("ref")?.AsString();
        if (refQuery == null)
            return;

        if (resolutionStack.Contains(refQuery))
        {
            throw new KdlSchemaException($"Circular reference detected: {refQuery}");
        }

        resolutionStack.Add(refQuery);

        try
        {
            var referencedNodes = KdlQuery.Execute(sourceDocument, refQuery);
            var referencedKdlNode = referencedNodes.FirstOrDefault();

            if (referencedKdlNode != null)
            {
                var referencedProp = ParseSchemaProperty(referencedKdlNode);
                ResolvePropertyReferences(referencedProp, schema);
                MergeSchemaProperty(prop, referencedProp);
            }
            else
            {
                var targetId = ExtractIdFromQuery(refQuery);
                if (targetId != null && schema.Definitions?.PropertyDefinitions.TryGetValue(targetId, out var defProp) == true)
                {
                    ResolvePropertyReferences(defProp, schema);
                    MergeSchemaProperty(prop, defProp);
                }
            }
        }
        finally
        {
            resolutionStack.Remove(refQuery);
        }
    }

    private void ResolveValueReferences(SchemaValue value, SchemaDocument schema)
    {
        if (!valueToSource.TryGetValue(value, out var sourceNode))
            return;

        var refQuery = sourceNode.GetProperty("ref")?.AsString();
        if (refQuery == null)
            return;

        if (resolutionStack.Contains(refQuery))
        {
            throw new KdlSchemaException($"Circular reference detected: {refQuery}");
        }

        resolutionStack.Add(refQuery);

        try
        {
            var referencedNodes = KdlQuery.Execute(sourceDocument, refQuery);
            var referencedKdlNode = referencedNodes.FirstOrDefault();

            if (referencedKdlNode != null)
            {
                var referencedValue = ParseSchemaValue(referencedKdlNode);
                ResolveValueReferences(referencedValue, schema);
                MergeSchemaValue(value, referencedValue);
            }
            else
            {
                var targetId = ExtractIdFromQuery(refQuery);
                if (targetId != null && schema.Definitions?.ValueDefinitions.TryGetValue(targetId, out var defValue) == true)
                {
                    ResolveValueReferences(defValue, schema);
                    MergeSchemaValue(value, defValue);
                }
            }
        }
        finally
        {
            resolutionStack.Remove(refQuery);
        }
    }

    private void ResolveChildrenReferences(SchemaChildren children, SchemaDocument schema)
    {
        if (!childrenToSource.TryGetValue(children, out var sourceNode))
            return;

        var refQuery = sourceNode.GetProperty("ref")?.AsString();
        if (refQuery == null)
            return;

        if (resolutionStack.Contains(refQuery))
        {
            throw new KdlSchemaException($"Circular reference detected: {refQuery}");
        }

        resolutionStack.Add(refQuery);

        try
        {
            var referencedNodes = KdlQuery.Execute(sourceDocument, refQuery);
            var referencedKdlNode = referencedNodes.FirstOrDefault();

            if (referencedKdlNode != null)
            {
                var referencedChildren = ParseSchemaChildren(referencedKdlNode);
                ResolveChildrenReferences(referencedChildren, schema);
                MergeSchemaChildren(children, referencedChildren);
            }
            else
            {
                var targetId = ExtractIdFromQuery(refQuery);
                if (targetId != null && schema.Definitions?.ChildrenDefinitions.TryGetValue(targetId, out var defChildren) == true)
                {
                    ResolveChildrenReferences(defChildren, schema);
                    MergeSchemaChildren(children, defChildren);
                }
            }
        }
        finally
        {
            resolutionStack.Remove(refQuery);
        }
    }

    private static string? ExtractIdFromQuery(string query)
    {
        // Try to extract ID from common query patterns like [id="some-id"]
        // This is a simplified approach - full query parsing would be more robust
        var idMatch = System.Text.RegularExpressions.Regex.Match(query, @"\[id=""([^""]+)""\]");
        return idMatch.Success ? idMatch.Groups[1].Value : null;
    }

    private static void MergeSchemaNode(SchemaNode target, SchemaNode source)
    {
        // Merge properties from source that don't exist in target
        var targetPropKeys = new HashSet<string>(target.Properties.Select(p => p.Key));
        var mergedProperties = new List<SchemaProperty>(target.Properties);
        foreach (var sourceProp in source.Properties)
        {
            if (!targetPropKeys.Contains(sourceProp.Key))
            {
                mergedProperties.Add(sourceProp);
            }
        }
        SetReadOnlyList(target, nameof(SchemaNode.Properties), mergedProperties);

        // Merge validation rules
        var mergedRules = new List<ValidationRule>(target.ValidationRules);
        var targetRuleNames = new HashSet<string>(target.ValidationRules.Select(r => r.RuleName));
        foreach (var rule in source.ValidationRules)
        {
            if (!targetRuleNames.Contains(rule.RuleName))
            {
                mergedRules.Add(rule);
            }
        }
        SetReadOnlyList(target, nameof(SchemaNode.ValidationRules), mergedRules);

        // Merge children if target doesn't have any
        if (target.Children == null && source.Children != null)
        {
            SetProperty(target, nameof(SchemaNode.Children), source.Children);
        }

        // Merge values if target doesn't have any
        if (target.Values == null && source.Values != null)
        {
            SetProperty(target, nameof(SchemaNode.Values), source.Values);
        }
    }

    private static void MergeSchemaProperty(SchemaProperty target, SchemaProperty source)
    {
        // Merge validation rules from source that don't exist in target
        var mergedRules = new List<ValidationRule>(target.ValidationRules);
        var targetRuleNames = new HashSet<string>(target.ValidationRules.Select(r => r.RuleName));
        foreach (var rule in source.ValidationRules)
        {
            if (!targetRuleNames.Contains(rule.RuleName))
            {
                mergedRules.Add(rule);
            }
        }
        SetReadOnlyList(target, nameof(SchemaProperty.ValidationRules), mergedRules);
    }

    private static void MergeSchemaValue(SchemaValue target, SchemaValue source)
    {
        // Merge validation rules from source that don't exist in target
        var mergedRules = new List<ValidationRule>(target.ValidationRules);
        var targetRuleNames = new HashSet<string>(target.ValidationRules.Select(r => r.RuleName));
        foreach (var rule in source.ValidationRules)
        {
            if (!targetRuleNames.Contains(rule.RuleName))
            {
                mergedRules.Add(rule);
            }
        }
        SetReadOnlyList(target, nameof(SchemaValue.ValidationRules), mergedRules);

        // Merge min/max if not set in target
        if (target.Min == null && source.Min != null)
        {
            SetProperty(target, nameof(SchemaValue.Min), source.Min);
        }
        if (target.Max == null && source.Max != null)
        {
            SetProperty(target, nameof(SchemaValue.Max), source.Max);
        }
    }

    private static void MergeSchemaChildren(SchemaChildren target, SchemaChildren source)
    {
        // Merge nodes from source that don't exist in target (by name)
        var targetNodeNames = new HashSet<string?>(target.Nodes.Select(n => n.Name));
        var mergedNodes = new List<SchemaNode>(target.Nodes);
        foreach (var sourceNode in source.Nodes)
        {
            if (!targetNodeNames.Contains(sourceNode.Name))
            {
                mergedNodes.Add(sourceNode);
            }
        }
        SetReadOnlyList(target, nameof(SchemaChildren.Nodes), mergedNodes);

        // Merge node name validations
        var mergedValidations = new List<ValidationRule>(target.NodeNameValidations);
        var targetValidationNames = new HashSet<string>(target.NodeNameValidations.Select(r => r.RuleName));
        foreach (var rule in source.NodeNameValidations)
        {
            if (!targetValidationNames.Contains(rule.RuleName))
            {
                mergedValidations.Add(rule);
            }
        }
        SetReadOnlyList(target, nameof(SchemaChildren.NodeNameValidations), mergedValidations);
    }

    private static void SetReadOnlyList<T>(object target, string propertyName, List<T> value)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, (IReadOnlyList<T>)value);
    }

    private static void SetProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }

    private static SchemaInfo ParseInfo(KdlNode infoNode)
    {
        var info = new SchemaInfo();

        foreach (var child in infoNode.Children)
        {
            switch (child.Name)
            {
                case "title":
                    info.Title = child.Arguments.FirstOrDefault()?.AsString();
                    break;

                case "description":
                    info.Description = child.Arguments.FirstOrDefault()?.AsString();
                    break;

                case "author":
                    var authors = new List<string>(info.Authors);
                    var authorName = child.Arguments.FirstOrDefault()?.AsString();
                    if (authorName != null)
                        authors.Add(authorName);
                    info.Authors = authors;
                    break;

                case "contributor":
                    var contributors = new List<string>(info.Contributors);
                    var contributorName = child.Arguments.FirstOrDefault()?.AsString();
                    if (contributorName != null)
                        contributors.Add(contributorName);
                    info.Contributors = contributors;
                    break;

                case "link":
                    var links = new List<SchemaLink>(info.Links);
                    var url = child.Arguments.FirstOrDefault()?.AsString();
                    if (url != null)
                    {
                        var rel = child.GetProperty("rel")?.AsString();
                        var lang = child.GetProperty("lang")?.AsString();
                        links.Add(new SchemaLink(url, rel, lang));
                    }
                    info.Links = links;
                    break;

                case "license":
                    info.License = child.Arguments.FirstOrDefault()?.AsString();
                    break;

                case "version":
                    info.Version = child.Arguments.FirstOrDefault()?.AsString();
                    break;

                case "published":
                    var publishedStr = child.Arguments.FirstOrDefault()?.AsString();
                    if (publishedStr != null && DateTime.TryParse(publishedStr, out var published))
                        info.Published = published;
                    break;

                case "modified":
                    var modifiedStr = child.Arguments.FirstOrDefault()?.AsString();
                    if (modifiedStr != null && DateTime.TryParse(modifiedStr, out var modified))
                        info.Modified = modified;
                    break;
            }
        }

        return info;
    }

    private SchemaNode ParseSchemaNode(KdlNode node)
    {
        // Node name is the first argument (optional)
        var name = node.Arguments.FirstOrDefault()?.AsString();

        // Description from properties
        var description = node.GetProperty("description")?.AsString();

        // ID from properties
        var id = node.GetProperty("id")?.AsString();

        // Parse children to find value, prop, children, and validation rules
        SchemaValue? values = null;
        var properties = new List<SchemaProperty>();
        SchemaChildren? children = null;
        var validationRules = new List<ValidationRule>();

        foreach (var child in node.Children)
        {
            switch (child.Name)
            {
                case "value":
                    values = ParseSchemaValue(child);
                    break;

                case "prop":
                    properties.Add(ParseSchemaProperty(child));
                    break;

                case "children":
                    children = ParseSchemaChildren(child);
                    break;

                default:
                    // Check if it's a validation rule
                    var rule = TryParseValidationRule(child);
                    if (rule != null)
                        validationRules.Add(rule);
                    break;
            }
        }

        var schemaNode = new SchemaNode(
            name,
            description,
            id,
            values,
            properties,
            children,
            validationRules);

        // Track source node for ref resolution
        nodeToSource[schemaNode] = node;

        return schemaNode;
    }

    private SchemaProperty ParseSchemaProperty(KdlNode node)
    {
        var key = node.Arguments.FirstOrDefault()?.AsString()
            ?? throw new KdlSchemaException("Property must have a key as first argument");

        var description = node.GetProperty("description")?.AsString();

        // Check for 'required' child node
        var requiredNode = node.Children.FirstOrDefault(c => c.Name == "required");
        var required = requiredNode?.Arguments.FirstOrDefault()?.AsBoolean() ?? false;

        // Parse validation rules from children
        var validationRules = ParseValidationRules((IReadOnlyList<KdlNode>)node.Children);

        var schemaProperty = new SchemaProperty(key, required, description, validationRules);

        // Track source node for ref resolution
        propertyToSource[schemaProperty] = node;

        return schemaProperty;
    }

    private SchemaValue ParseSchemaValue(KdlNode node)
    {
        var description = node.GetProperty("description")?.AsString();

        // Parse min and max from children
        int? min = null;
        int? max = null;

        var minNode = node.Children.FirstOrDefault(c => c.Name == "min");
        if (minNode != null)
        {
            var minValue = minNode.Arguments.FirstOrDefault();
            if (minValue != null && minValue.AsNumber().HasValue)
                min = (int)minValue.AsNumber()!.Value;
        }

        var maxNode = node.Children.FirstOrDefault(c => c.Name == "max");
        if (maxNode != null)
        {
            var maxValue = maxNode.Arguments.FirstOrDefault();
            if (maxValue != null && maxValue.AsNumber().HasValue)
                max = (int)maxValue.AsNumber()!.Value;
        }

        // Parse validation rules
        var validationRules = ParseValidationRules((IReadOnlyList<KdlNode>)node.Children);

        var schemaValue = new SchemaValue(min, max, description, validationRules);

        // Track source node for ref resolution
        valueToSource[schemaValue] = node;

        return schemaValue;
    }

    private SchemaChildren ParseSchemaChildren(KdlNode node)
    {
        var description = node.GetProperty("description")?.AsString();

        // Parse child nodes
        var childNodes = node.Children
            .Where(c => c.Name == "node")
            .Select(ParseSchemaNode)
            .ToList();

        // Parse other-nodes-allowed
        var otherNodesAllowed = node.Children
            .Where(c => c.Name == "other-nodes-allowed")
            .FirstOrDefault()?.Arguments.FirstOrDefault()?.AsBoolean() ?? false;

        // Parse node-names validations
        var nodeNamesNode = node.Children.FirstOrDefault(c => c.Name == "node-names");
        var nodeNameValidations = nodeNamesNode != null
            ? ParseValidationRules((IReadOnlyList<KdlNode>)nodeNamesNode.Children)
            : Array.Empty<ValidationRule>();

        var schemaChildren = new SchemaChildren(childNodes, nodeNameValidations, otherNodesAllowed);

        // Track source node for ref resolution
        childrenToSource[schemaChildren] = node;

        return schemaChildren;
    }

    private SchemaTag ParseTag(KdlNode node)
    {
        var name = node.Arguments.FirstOrDefault()?.AsString()
            ?? throw new KdlSchemaException("Tag must have a name as first argument");

        // Parse the tag's node definition from children
        // Tags can have 'node' children that define what nodes can have this tag
        var nodeChildren = node.Children.Where(c => c.Name == "node").ToList();

        // For simplicity, create a composite node definition
        // In a real implementation, this might need more sophisticated handling
        var nodeDefinition = nodeChildren.Any()
            ? ParseSchemaNode(nodeChildren.First())
            : new SchemaNode(name: name);

        return new SchemaTag(name, nodeDefinition);
    }

    private SchemaDefinitions? ParseDefinitions(KdlNode node)
    {
        var definitions = new SchemaDefinitions();
        var hasDefinitions = false;

        foreach (var child in node.Children)
        {
            var id = child.GetProperty("id")?.AsString();

            if (id == null)
                continue;

            switch (child.Name)
            {
                case "node":
                    definitions.AddNodeDefinition(id, ParseSchemaNode(child));
                    hasDefinitions = true;
                    break;

                case "prop":
                    definitions.AddPropertyDefinition(id, ParseSchemaProperty(child));
                    hasDefinitions = true;
                    break;

                case "value":
                    definitions.AddValueDefinition(id, ParseSchemaValue(child));
                    hasDefinitions = true;
                    break;

                case "children":
                    definitions.AddChildrenDefinition(id, ParseSchemaChildren(child));
                    hasDefinitions = true;
                    break;
            }
        }

        return hasDefinitions ? definitions : null;
    }

    private static IReadOnlyList<ValidationRule> ParseValidationRules(IReadOnlyList<KdlNode> nodes)
    {
        var rules = new List<ValidationRule>();

        foreach (var node in nodes)
        {
            var rule = TryParseValidationRule(node);
            if (rule != null)
                rules.Add(rule);
        }

        return rules;
    }

    private static ValidationRule? TryParseValidationRule(KdlNode node)
    {
        return node.Name switch
        {
            // String validation rules
            "pattern" => new PatternRule(node.Arguments.FirstOrDefault()?.AsString() ?? ""),
            "min-length" => new MinLengthRule((int)(node.Arguments.FirstOrDefault()?.AsNumber() ?? 0)),
            "max-length" => new MaxLengthRule((int)(node.Arguments.FirstOrDefault()?.AsNumber() ?? int.MaxValue)),
            "format" => new FormatRule(node.Arguments.FirstOrDefault()?.AsString() ?? ""),

            // Number validation rules
            ">" => new GreaterThanRule(node.Arguments.FirstOrDefault()?.AsNumber() ?? 0),
            ">=" => new GreaterOrEqualRule(node.Arguments.FirstOrDefault()?.AsNumber() ?? 0),
            "<" => new LessThanRule(node.Arguments.FirstOrDefault()?.AsNumber() ?? 0),
            "<=" => new LessOrEqualRule(node.Arguments.FirstOrDefault()?.AsNumber() ?? 0),
            "%" => new ModuloRule(node.Arguments.FirstOrDefault()?.AsNumber() ?? 1),

            // Structural validation rules
            "required" => new RequiredRule(),
            "type" => new TypeRule(node.Arguments.FirstOrDefault()?.AsString() ?? ""),
            "enum" => new EnumRule(node.Arguments.Select(v => v.AsString() ?? "").ToArray()),

            // Multiplicity rules
            "min" => new MinRule((int)(node.Arguments.FirstOrDefault()?.AsNumber() ?? 0)),
            "max" => new MaxRule((int)(node.Arguments.FirstOrDefault()?.AsNumber() ?? int.MaxValue)),

            // Unknown rule
            _ => null
        };
    }
}

