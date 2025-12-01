namespace KdlSharp;

/// <summary>
/// Represents a position in KDL source text.
/// </summary>
/// <remarks>
/// Used for error reporting and source mapping. Only present for parsed nodes, not programmatically constructed ones.
/// </remarks>
public sealed class SourcePosition
{
    /// <summary>
    /// Gets the line number (1-indexed).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column number (1-indexed).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the byte offset in the source text.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Initializes a new source position.
    /// </summary>
    public SourcePosition(int line, int column, int offset)
    {
        Line = line;
        Column = column;
        Offset = offset;
    }

    /// <summary>
    /// Returns a string representation like "line 10, column 5".
    /// </summary>
    public override string ToString() => $"line {Line}, column {Column}";
}

