using KdlSharp.Schema.Rules;
using KdlSharp.Values;

namespace KdlSharp.Schema;

/// <summary>
/// Validates KDL documents against schemas.
/// </summary>
public sealed class SchemaValidator
{
    private readonly SchemaDocument schema;

    /// <summary>
    /// Initializes a new schema validator.
    /// </summary>
    /// <param name="schema">The schema document to validate against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is null.</exception>
    public SchemaValidator(SchemaDocument schema)
    {
        this.schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>
    /// Validates a document against the schema.
    /// </summary>
    public ValidationResult Validate(KdlDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        var context = new ValidationContext();
        context.PushPath("document");

        // Validate top-level nodes
        ValidateNodes(document.Nodes, schema.Nodes, context, schema.OtherNodesAllowed);

        context.PopPath();

        return context.Errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(context.Errors.ToArray());
    }

    private void ValidateNodes(
        IList<KdlNode> actualNodes,
        IReadOnlyList<SchemaNode> schemaNodes,
        ValidationContext context,
        bool otherNodesAllowed)
    {
        // Build a map of schema nodes by name
        var schemaNodeMap = schemaNodes
            .Where(n => n.Name != null)
            .ToDictionary(n => n.Name!, n => n);

        // Track which actual nodes have been validated
        var validatedNodes = new HashSet<KdlNode>();

        // Validate each schema node
        foreach (var schemaNode in schemaNodes)
        {
            if (schemaNode.Name == null)
                continue;

            var matchingNodes = actualNodes
                .Where(n => n.Name == schemaNode.Name)
                .ToList();

            foreach (var actualNode in matchingNodes)
            {
                ValidateNode(actualNode, schemaNode, context);
                validatedNodes.Add(actualNode);
            }
        }

        // Check for unexpected nodes
        if (!otherNodesAllowed)
        {
            foreach (var node in actualNodes)
            {
                if (!validatedNodes.Contains(node))
                {
                    context.AddError("unexpected-node", $"Unexpected node '{node.Name}'");
                }
            }
        }
    }

    private void ValidateNode(KdlNode node, SchemaNode schemaNode, ValidationContext context)
    {
        context.PushPath(node.Name);

        // Validate values
        if (schemaNode.Values != null)
        {
            ValidateValues(node.Arguments, schemaNode.Values, context);
        }

        // Validate properties
        ValidateProperties(node, schemaNode.Properties, context);

        // Validate children
        if (schemaNode.Children != null)
        {
            ValidateNodes(node.Children, schemaNode.Children.Nodes, context, schemaNode.Children.OtherNodesAllowed);
        }

        // Apply validation rules
        foreach (var rule in schemaNode.ValidationRules)
        {
            ApplyValidationRule(node, rule, context);
        }

        context.PopPath();
    }

    private void ValidateValues(
        IList<KdlValue> actualValues,
        SchemaValue schemaValue,
        ValidationContext context)
    {
        // Check min/max counts
        if (schemaValue.Min.HasValue && actualValues.Count < schemaValue.Min.Value)
        {
            context.AddError("min-values", $"Expected at least {schemaValue.Min.Value} values, got {actualValues.Count}");
        }

        if (schemaValue.Max.HasValue && actualValues.Count > schemaValue.Max.Value)
        {
            context.AddError("max-values", $"Expected at most {schemaValue.Max.Value} values, got {actualValues.Count}");
        }

        // Validate each value
        for (int i = 0; i < actualValues.Count; i++)
        {
            context.PushPath($"value[{i}]");

            foreach (var rule in schemaValue.ValidationRules)
            {
                if (!rule.Validate(actualValues[i], context))
                {
                    context.AddError(rule.RuleName, rule.GetErrorMessage(actualValues[i]));
                }
            }

            context.PopPath();
        }
    }

    private void ValidateProperties(
        KdlNode node,
        IReadOnlyList<SchemaProperty> schemaProperties,
        ValidationContext context)
    {
        foreach (var schemaProp in schemaProperties)
        {
            if (schemaProp.Required && !node.HasProperty(schemaProp.Key))
            {
                context.AddError("required-property", $"Required property '{schemaProp.Key}' is missing");
                continue;
            }

            var value = node.GetProperty(schemaProp.Key);
            if (value != null)
            {
                context.PushPath($"property[{schemaProp.Key}]");

                foreach (var rule in schemaProp.ValidationRules)
                {
                    if (!rule.Validate(value, context))
                    {
                        context.AddError(rule.RuleName, rule.GetErrorMessage(value));
                    }
                }

                context.PopPath();
            }
        }
    }

    private void ApplyValidationRule(KdlNode node, ValidationRule rule, ValidationContext context)
    {
        // RefRule is a no-op at validation time since references are resolved at parse time
        if (rule is RefRule)
        {
            return;
        }

        // For other rules, validate the node
        if (!rule.Validate(node, context))
        {
            context.AddError(rule.RuleName, rule.GetErrorMessage(node));
        }
    }
}

