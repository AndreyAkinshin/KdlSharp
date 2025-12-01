namespace KdlSharp.Values;

/// <summary>
/// Represents a KDL string value.
/// </summary>
public sealed class KdlString : KdlValue
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the original string type used when parsing this string.
    /// </summary>
    /// <remarks>
    /// This is used to preserve the original format when serializing.
    /// When constructing a string programmatically, this defaults to <see cref="KdlStringType.Quoted"/>.
    /// </remarks>
    public KdlStringType StringType { get; }

    /// <summary>
    /// Gets the number of hash characters used in the raw string delimiter.
    /// Only meaningful when <see cref="StringType"/> is <see cref="KdlStringType.Raw"/>.
    /// </summary>
    public int RawHashCount { get; }

    /// <summary>
    /// Gets a value indicating whether this raw string was multi-line (triple-quoted).
    /// Only meaningful when <see cref="StringType"/> is <see cref="KdlStringType.Raw"/>.
    /// </summary>
    public bool IsRawMultiLine { get; }

    /// <summary>
    /// Gets the original whitespace prefix from the closing line of a multi-line raw string.
    /// Used to preserve formatting when serializing with PreserveStringTypes.
    /// Only meaningful when <see cref="IsRawMultiLine"/> is true.
    /// </summary>
    public string? RawMultiLineIndent { get; }

    /// <summary>
    /// Gets the value type (String).
    /// </summary>
    public override KdlValueType ValueType => KdlValueType.String;

    /// <summary>
    /// Initializes a new string value.
    /// </summary>
    /// <param name="value">The string content.</param>
    /// <param name="stringType">The string type (defaults to Quoted).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public KdlString(string value, KdlStringType stringType = KdlStringType.Quoted)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        StringType = stringType;
        RawHashCount = 0;
        IsRawMultiLine = false;
        RawMultiLineIndent = null;
    }

    /// <summary>
    /// Initializes a new string value with raw string metadata.
    /// </summary>
    /// <param name="value">The string content.</param>
    /// <param name="stringType">The string type.</param>
    /// <param name="rawHashCount">The number of hash characters in the raw string delimiter.</param>
    /// <param name="isRawMultiLine">Whether the raw string was multi-line (triple-quoted).</param>
    /// <param name="rawMultiLineIndent">The original whitespace prefix from the closing line (for multi-line raw strings).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public KdlString(string value, KdlStringType stringType, int rawHashCount, bool isRawMultiLine, string? rawMultiLineIndent = null)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        StringType = stringType;
        RawHashCount = rawHashCount;
        IsRawMultiLine = isRawMultiLine;
        RawMultiLineIndent = rawMultiLineIndent;
    }

    /// <summary>
    /// Creates a raw string value.
    /// </summary>
    /// <param name="value">The string content.</param>
    /// <param name="hashCount">The number of hash characters to use in the delimiter. If 0, the minimal safe count is computed automatically.</param>
    /// <param name="multiLine">Whether to format as a multi-line raw string.</param>
    /// <returns>A new <see cref="KdlString"/> with <see cref="KdlStringType.Raw"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Create a simple raw string
    /// var raw = KdlString.Raw(@"C:\path\to\file");
    ///
    /// // Create a raw string with explicit hash count
    /// var rawWithHashes = KdlString.Raw("contains \"# pattern", hashCount: 2);
    ///
    /// // Create a multi-line raw string
    /// var rawMultiLine = KdlString.Raw("line1\nline2", multiLine: true);
    /// </code>
    /// </example>
    public static KdlString Raw(string value, int hashCount = 0, bool multiLine = false)
    {
        return new KdlString(value ?? throw new ArgumentNullException(nameof(value)), KdlStringType.Raw, hashCount, multiLine);
    }

    /// <summary>
    /// Returns this value as a string.
    /// </summary>
    /// <returns>The string value.</returns>
    public override string AsString() => Value;

    /// <summary>
    /// Converts this value to a KDL string representation.
    /// </summary>
    /// <returns>The KDL string representation.</returns>
    public override string ToKdlString()
    {
        return StringType switch
        {
            KdlStringType.Identifier => Value,
            KdlStringType.Quoted => Utilities.StringEscaper.Escape(Value),
            KdlStringType.MultiLine => $"\"\"\"{Value}\"\"\"",
            KdlStringType.Raw => FormatRawString(),
            _ => Utilities.StringEscaper.Escape(Value)
        };
    }

    private string FormatRawString()
    {
        // Determine the hash count to use
        var hashCount = RawHashCount > 0 ? RawHashCount : ComputeMinimalHashCount(Value);
        var hashes = new string('#', hashCount);

        if (IsRawMultiLine)
        {
            // Multi-line raw string: #"""..."""#
            // Note: For ToKdlString(), we output a compact single-line representation
            // since proper indentation depends on context (use KdlWriter for formatted output)
            return $"{hashes}\"\"\"\n{Value}\n\"\"\"{hashes}";
        }
        else
        {
            // Single-line raw string: #"..."#
            return $"{hashes}\"{Value}\"{hashes}";
        }
    }

    /// <summary>
    /// Computes the minimal number of hash characters needed to safely delimit a raw string.
    /// </summary>
    private static int ComputeMinimalHashCount(string value)
    {
        // We need to ensure the closing delimiter ("# pattern) doesn't appear in the value
        // Start with 1 hash and increment if the pattern appears in the value
        var hashCount = 1;
        while (true)
        {
            var closingPattern = '"' + new string('#', hashCount);
            if (!value.Contains(closingPattern))
            {
                break;
            }
            hashCount++;
        }
        return hashCount;
    }

    /// <summary>
    /// Creates a deep clone of this value.
    /// </summary>
    /// <returns>A new KdlString with the same value and string type.</returns>
    public override KdlValue Clone() => new KdlString(Value, StringType, RawHashCount, IsRawMultiLine, RawMultiLineIndent) { TypeAnnotation = TypeAnnotation };
}

/// <summary>
/// Enumeration of KDL string types.
/// </summary>
public enum KdlStringType
{
    /// <summary>Bare identifier (e.g., <c>foo</c>).</summary>
    Identifier,

    /// <summary>Quoted string with escapes (e.g., <c>"hello\nworld"</c>).</summary>
    Quoted,

    /// <summary>Multi-line string with dedentation (e.g., <c>"""text"""</c>).</summary>
    MultiLine,

    /// <summary>Raw string with no escapes (e.g., <c>#"raw"#</c>).</summary>
    Raw
}

