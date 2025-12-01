namespace KdlSharp.Parsing;

/// <summary>
/// Represents the type of token in a KDL document for streaming readers.
/// </summary>
/// <remarks>
/// <para>
/// This is the public token type enum exposed by <see cref="KdlReader"/> for streaming
/// KDL parsing. It provides a clean subset of token types relevant to API consumers.
/// </para>
/// <para>
/// Unlike the internal <c>TokenType</c> enum used by the lexer, this enum excludes
/// internal-only tokens (whitespace, comments, line continuations) that are consumed
/// during lexing and never exposed to consumers.
/// </para>
/// </remarks>
public enum KdlTokenType
{
    /// <summary>
    /// No token has been read yet or the reader is at the end.
    /// </summary>
    None,

    /// <summary>
    /// Opening brace: {
    /// </summary>
    OpenBrace,

    /// <summary>
    /// Closing brace: }
    /// </summary>
    CloseBrace,

    /// <summary>
    /// Semicolon: ;
    /// </summary>
    Semicolon,

    /// <summary>
    /// Equals: =
    /// </summary>
    Equals,

    /// <summary>
    /// Opening parenthesis: (
    /// </summary>
    OpenParen,

    /// <summary>
    /// Closing parenthesis: )
    /// </summary>
    CloseParen,

    /// <summary>
    /// String value (identifier, quoted, multi-line, or raw).
    /// </summary>
    String,

    /// <summary>
    /// Numeric value.
    /// </summary>
    Number,

    /// <summary>
    /// Boolean true value.
    /// </summary>
    True,

    /// <summary>
    /// Boolean false value.
    /// </summary>
    False,

    /// <summary>
    /// Null value.
    /// </summary>
    Null,

    /// <summary>
    /// Infinity value (#inf or #-inf).
    /// </summary>
    Infinity,

    /// <summary>
    /// NaN value (#nan).
    /// </summary>
    NaN,

    /// <summary>
    /// Slashdash comment: /-
    /// </summary>
    Slashdash,

    /// <summary>
    /// Newline character.
    /// </summary>
    Newline,

    /// <summary>
    /// End of file/document.
    /// </summary>
    EndOfFile
}

