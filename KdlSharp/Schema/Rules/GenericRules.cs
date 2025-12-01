using System.Text.RegularExpressions;
using KdlSharp.Values;

namespace KdlSharp.Schema.Rules;

/// <summary>
/// Generic validation rules for KDL schema validation.
/// </summary>
/// <remarks>
/// <para>
/// This file contains generic validation rules that apply to both values and nodes:
/// <list type="bullet">
///   <item><description><strong>TypeRule</strong>: Validates type annotations (fully implemented)</description></item>
///   <item><description><strong>TagRule</strong>: Validates tag annotations on nodes and values (fully implemented)</description></item>
///   <item><description><strong>EnumRule</strong>: Validates values against allowed enumeration (fully implemented)</description></item>
///   <item><description><strong>RequiredRule</strong>: Validates that a value is present (fully implemented)</description></item>
///   <item><description><strong>DescriptionRule</strong>: Non-validating documentation rule (fully implemented)</description></item>
///   <item><description><strong>IdRule</strong>: Non-validating identifier for references (fully implemented)</description></item>
///   <item><description><strong>RefRule</strong>: Reference to a definition (resolved at parse time, no-op at validation)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Note on RefRule</strong>: Schema references are resolved at parse time by the SchemaParser
/// using the KDL Query Language. The RefRule class exists as a marker but performs no validation
/// since all references are already resolved before validation begins.
/// </para>
/// <para>
/// For string-specific rules, see <c>StringRules.cs</c>. For number-specific rules, see <c>NumberRules.cs</c>.
/// For structural rules, see <c>StructuralRules.cs</c>.
/// </para>
/// </remarks>

/// <summary>
/// Validates type annotation.
/// </summary>
public sealed class TypeRule : ValidationRule
{
    private readonly string expectedType;

    /// <summary>
    /// Gets the rule name ("type").
    /// </summary>
    public override string RuleName => "type";

    /// <summary>
    /// Initializes a new type rule.
    /// </summary>
    /// <param name="expectedType">The expected type annotation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="expectedType"/> is null.</exception>
    public TypeRule(string expectedType)
    {
        this.expectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
    }

    /// <summary>
    /// Validates that the value has the expected type annotation.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        if (value is KdlValue kdlValue)
        {
            // Check if the value has a type annotation
            if (kdlValue.TypeAnnotation == null)
            {
                // No type annotation present, validation fails
                return false;
            }

            // Compare the type annotation with the expected type
            return kdlValue.TypeAnnotation.TypeName == expectedType;
        }

        // Not a KdlValue, cannot validate type annotation
        return false;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Expected type annotation '{expectedType}'";
    }
}

/// <summary>
/// Validates value against allowed enumeration.
/// </summary>
public sealed class EnumRule : ValidationRule
{
    private readonly HashSet<object> allowedValues;

    /// <summary>
    /// Gets the rule name ("enum").
    /// </summary>
    public override string RuleName => "enum";

    /// <summary>
    /// Initializes a new enumeration rule.
    /// </summary>
    /// <param name="allowedValues">The set of allowed values.</param>
    /// <exception cref="ArgumentNullException"><paramref name="allowedValues"/> is null.</exception>
    public EnumRule(IEnumerable<object> allowedValues)
    {
        this.allowedValues = new HashSet<object>(allowedValues ?? throw new ArgumentNullException(nameof(allowedValues)));
    }

    /// <summary>
    /// Validates that the value is in the allowed enumeration.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        if (value is KdlValue kdlValue)
        {
            var actualValue = GetKdlValueAsObject(kdlValue);
            return allowedValues.Contains(actualValue!);
        }
        return allowedValues.Contains(value!);
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Value '{value}' is not in allowed enumeration: [{string.Join(", ", allowedValues)}]";
    }

    private static object? GetKdlValueAsObject(KdlValue value)
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
}

/// <summary>
/// Marks a field as required.
/// </summary>
public sealed class RequiredRule : ValidationRule
{
    /// <summary>
    /// Gets the rule name ("required").
    /// </summary>
    public override string RuleName => "required";

    /// <summary>
    /// Validates that the value is present (not null).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        return value != null;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return "Required value is missing";
    }
}

/// <summary>
/// Documentation rule (non-validating).
/// </summary>
public sealed class DescriptionRule : ValidationRule
{
    private readonly string description;

    /// <summary>
    /// Gets the rule name ("description").
    /// </summary>
    public override string RuleName => "description";

    /// <summary>
    /// Initializes a new description rule.
    /// </summary>
    /// <param name="description">The description text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="description"/> is null.</exception>
    public DescriptionRule(string description)
    {
        this.description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>
    /// Always passes validation (non-validating rule).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>Always true.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        // Description is non-validating, always passes
        return true;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>An empty string.</returns>
    public override string GetErrorMessage(object? value)
    {
        return "";
    }
}

/// <summary>
/// Identifier for references (non-validating).
/// </summary>
public sealed class IdRule : ValidationRule
{
    private readonly string id;

    /// <summary>
    /// Gets the rule name ("id").
    /// </summary>
    public override string RuleName => "id";

    /// <summary>
    /// Initializes a new ID rule.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is null.</exception>
    public IdRule(string id)
    {
        this.id = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>
    /// Gets the identifier value.
    /// </summary>
    public string Id => id;

    /// <summary>
    /// Always passes validation (non-validating rule).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>Always true.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        // ID is non-validating, always passes
        return true;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>An empty string.</returns>
    public override string GetErrorMessage(object? value)
    {
        return "";
    }
}

/// <summary>
/// Reference to a definition (resolved at parse time by SchemaParser).
/// </summary>
/// <remarks>
/// Schema references are resolved at parse time by the SchemaParser using the KDL Query Language.
/// The referenced definitions are merged into the referencing node before validation begins.
/// This rule exists as a marker in the parsed schema but performs no validation at runtime.
/// </remarks>
public sealed class RefRule : ValidationRule
{
    private readonly string reference;

    /// <summary>
    /// Gets the rule name ("ref").
    /// </summary>
    public override string RuleName => "ref";

    /// <summary>
    /// Initializes a new reference rule.
    /// </summary>
    /// <param name="reference">The reference to another definition.</param>
    /// <exception cref="ArgumentNullException"><paramref name="reference"/> is null.</exception>
    public RefRule(string reference)
    {
        this.reference = reference ?? throw new ArgumentNullException(nameof(reference));
    }

    /// <summary>
    /// Gets the reference value.
    /// </summary>
    public string Reference => reference;

    /// <summary>
    /// No-op validation since references are resolved at parse time.
    /// </summary>
    /// <param name="value">The value to validate (unused).</param>
    /// <param name="context">The validation context (unused).</param>
    /// <returns>Always true since references are already resolved.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        // References are resolved at parse time by SchemaParser.ResolveReferences()
        // This rule exists as a marker but performs no validation
        return true;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message (should never be used since Validate always returns true).</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Reference '{reference}' validation failed";
    }
}

/// <summary>
/// Tag validation rule.
/// </summary>
public sealed class TagRule : ValidationRule
{
    private readonly string expectedTag;

    /// <summary>
    /// Gets the rule name ("tag").
    /// </summary>
    public override string RuleName => "tag";

    /// <summary>
    /// Initializes a new tag rule.
    /// </summary>
    /// <param name="expectedTag">The expected tag (type annotation).</param>
    /// <exception cref="ArgumentNullException"><paramref name="expectedTag"/> is null.</exception>
    public TagRule(string expectedTag)
    {
        this.expectedTag = expectedTag ?? throw new ArgumentNullException(nameof(expectedTag));
    }

    /// <summary>
    /// Validates that the value or node has the expected tag annotation.
    /// </summary>
    /// <param name="value">The value to validate (KdlValue or KdlNode).</param>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public override bool Validate(object? value, ValidationContext context)
    {
        // Check if it's a KdlValue with type annotation
        if (value is KdlValue kdlValue)
        {
            if (kdlValue.TypeAnnotation == null)
            {
                // No annotation present, validation fails
                return false;
            }
            return kdlValue.TypeAnnotation.TypeName == expectedTag;
        }

        // Check if it's a KdlNode with type annotation
        if (value is KdlNode kdlNode)
        {
            if (kdlNode.TypeAnnotation == null)
            {
                // No annotation present, validation fails
                return false;
            }
            return kdlNode.TypeAnnotation.TypeName == expectedTag;
        }

        // Not a KdlValue or KdlNode, cannot validate tag annotation
        return false;
    }

    /// <summary>
    /// Gets the error message for validation failure.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <returns>The error message.</returns>
    public override string GetErrorMessage(object? value)
    {
        return $"Expected tag '{expectedTag}'";
    }
}

