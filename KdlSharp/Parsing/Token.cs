using KdlSharp.Values;

namespace KdlSharp.Parsing;

/// <summary>
/// Represents a lexical token from KDL source.
/// </summary>
internal sealed class Token
{
    public TokenType Type { get; }
    public string? Text { get; }
    public object? Value { get; }
    public int Line { get; }
    public int Column { get; }
    public int Offset { get; }

    /// <summary>
    /// For string tokens, indicates the type of string (identifier, quoted, raw, multi-line).
    /// Defaults to <see cref="KdlStringType.Quoted"/> for non-string tokens.
    /// </summary>
    public KdlStringType StringType { get; }

    /// <summary>
    /// For raw string tokens, indicates the number of hash characters used in the delimiter.
    /// Defaults to 0 for non-raw strings.
    /// </summary>
    public int RawHashCount { get; }

    /// <summary>
    /// For raw string tokens, indicates whether the string was multi-line (triple-quoted).
    /// </summary>
    public bool IsRawMultiLine { get; }

    /// <summary>
    /// For multi-line raw string tokens, the original whitespace prefix from the closing line.
    /// Used to preserve formatting when serializing with PreserveStringTypes.
    /// </summary>
    public string? RawMultiLineIndent { get; }

    public Token(TokenType type, int line, int column, int offset, string? text = null, object? value = null, KdlStringType stringType = KdlStringType.Quoted, int rawHashCount = 0, bool isRawMultiLine = false, string? rawMultiLineIndent = null)
    {
        Type = type;
        Line = line;
        Column = column;
        Offset = offset;
        Text = text;
        Value = value;
        StringType = stringType;
        RawHashCount = rawHashCount;
        IsRawMultiLine = isRawMultiLine;
        RawMultiLineIndent = rawMultiLineIndent;
    }

    public override string ToString() => $"{Type} at {Line}:{Column}" + (Text != null ? $" '{Text}'" : "");
}

