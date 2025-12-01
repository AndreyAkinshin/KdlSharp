namespace KdlSharp.Parsing;

/// <summary>
/// Internal token types used by the KDL lexer during parsing.
/// </summary>
/// <remarks>
/// <para>
/// This enum is for internal lexer use only. It includes token types that are consumed
/// during lexing (such as whitespace and comments) and never exposed to consumers.
/// </para>
/// <para>
/// For the public streaming API, see <see cref="KdlTokenType"/> which provides a cleaner
/// subset of token types exposed by <see cref="KdlReader"/>.
/// </para>
/// </remarks>
internal enum TokenType
{
    // Structural tokens
    OpenBrace,          // {
    CloseBrace,         // }
    Semicolon,          // ;
    Equals,             // =
    OpenParen,          // (
    CloseParen,         // )

    // Value tokens
    String,             // "text", """text""", #"raw"#, identifier
    Number,             // 123, 0xFF, 0o755, 0b1010
    True,               // #true
    False,              // #false
    Null,               // #null
    Infinity,           // #inf, #-inf
    NaN,                // #nan

    // Special tokens
    Slashdash,          // /-

    /// <summary>Reserved for internal use. Consumed by the lexer, not returned to consumers.</summary>
    LineContinuation,   // \

    Newline,            // \n, \r\n, etc.

    /// <summary>Reserved for internal use. Consumed by the lexer, not returned to consumers.</summary>
    Whitespace,         // spaces, tabs, etc.

    /// <summary>Reserved for internal use. Consumed by the lexer, not returned to consumers.</summary>
    Comment,            // //, /* */

    // Sentinel
    EndOfFile
}

