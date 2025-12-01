using KdlSharp.Settings;
using KdlSharp.Utilities;

namespace KdlSharp.Parsing;

/// <summary>
/// Low-level reader for processing KDL input token by token.
/// </summary>
/// <remarks>
/// <para>
/// This class provides fine-grained control over KDL parsing for scenarios
/// that require token-level access or custom deserialization logic.
/// </para>
/// <para>
/// <b>Note:</b> The reader loads the entire input into memory on first <see cref="Read"/> call.
/// It does not perform true incremental streaming. For typical configuration files, this is not a concern.
/// </para>
/// <para>
/// For most scenarios, use <see cref="KdlParser"/> or <see cref="Serialization.KdlSerializer"/> instead.
/// </para>
/// <para>
/// This class is not thread-safe. Create separate instances for concurrent reads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var reader = new StreamReader("config.kdl");
/// using var kdlReader = new KdlReader(reader);
/// 
/// while (kdlReader.Read())
/// {
///     if (kdlReader.TokenType == KdlTokenType.String)
///     {
///         Console.WriteLine($"String: {kdlReader.StringValue}");
///     }
///     else if (kdlReader.TokenType == KdlTokenType.Number)
///     {
///         Console.WriteLine($"Number: {kdlReader.NumberValue}");
///     }
/// }
/// </code>
/// </example>
public sealed class KdlReader : IDisposable
{
    private readonly TextReader reader;
    private readonly bool leaveOpen;
    private readonly KdlParserSettings settings;
    private Lexer? lexer;
    private Token? currentToken;
    private bool disposed;

    /// <summary>
    /// Initializes a new reader with the specified input.
    /// </summary>
    /// <param name="reader">The text reader to read KDL content from.</param>
    /// <param name="settings">Optional parser settings. Defaults to v2 if not specified.</param>
    /// <param name="leaveOpen">Whether to leave the underlying reader open when disposing.</param>
    public KdlReader(TextReader reader, KdlParserSettings? settings = null, bool leaveOpen = false)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.settings = settings ?? new KdlParserSettings();
        this.leaveOpen = leaveOpen;
        currentToken = null;
    }

    /// <summary>
    /// Gets the type of the current token.
    /// </summary>
    public KdlTokenType TokenType
    {
        get
        {
            if (currentToken == null)
                return KdlTokenType.None;

            return currentToken.Type switch
            {
                Parsing.TokenType.OpenBrace => KdlTokenType.OpenBrace,
                Parsing.TokenType.CloseBrace => KdlTokenType.CloseBrace,
                Parsing.TokenType.Semicolon => KdlTokenType.Semicolon,
                Parsing.TokenType.Equals => KdlTokenType.Equals,
                Parsing.TokenType.OpenParen => KdlTokenType.OpenParen,
                Parsing.TokenType.CloseParen => KdlTokenType.CloseParen,
                Parsing.TokenType.String => KdlTokenType.String,
                Parsing.TokenType.Number => KdlTokenType.Number,
                Parsing.TokenType.True => KdlTokenType.True,
                Parsing.TokenType.False => KdlTokenType.False,
                Parsing.TokenType.Null => KdlTokenType.Null,
                Parsing.TokenType.Infinity => KdlTokenType.Infinity,
                Parsing.TokenType.NaN => KdlTokenType.NaN,
                Parsing.TokenType.Slashdash => KdlTokenType.Slashdash,
                Parsing.TokenType.Newline => KdlTokenType.Newline,
                Parsing.TokenType.EndOfFile => KdlTokenType.EndOfFile,
                _ => KdlTokenType.None
            };
        }
    }

    /// <summary>
    /// Gets the current line number (1-indexed).
    /// </summary>
    public int Line => currentToken?.Line ?? 1;

    /// <summary>
    /// Gets the current column number (1-indexed).
    /// </summary>
    public int Column => currentToken?.Column ?? 1;

    /// <summary>
    /// Gets the current position (offset from start of document).
    /// </summary>
    public int Position => currentToken?.Offset ?? 0;

    /// <summary>
    /// Gets the string value of the current token (for strings and identifiers).
    /// Returns null if the current token is not a string.
    /// </summary>
    public string? StringValue
    {
        get
        {
            if (currentToken?.Type == Parsing.TokenType.String && currentToken.Value is string str)
                return str;
            return null;
        }
    }

    /// <summary>
    /// Gets the numeric value of the current token.
    /// Returns null if the current token is not a number.
    /// </summary>
    /// <remarks>
    /// For hex/octal/binary numbers, this parses the raw text and may throw OverflowException if the number exceeds decimal range.
    /// </remarks>
    public decimal? NumberValue
    {
        get
        {
            if (currentToken?.Type == Parsing.TokenType.Number)
            {
                if (currentToken.Value is decimal num)
                    return num;

                // Handle hex/octal/binary stored as strings
                if (currentToken.Value is string rawText)
                {
                    return ParseNumberFromRawText(rawText);
                }
            }
            if (currentToken?.Type == Parsing.TokenType.Infinity)
            {
                // For infinity tokens, we don't have a meaningful decimal value
                return null;
            }
            return null;
        }
    }

    private static decimal ParseNumberFromRawText(string rawText)
    {
        if (NumberParser.TryParseNonDecimal(rawText, out var decimalValue))
        {
            return decimalValue;
        }

        // Fall back to decimal parsing
        return decimal.Parse(rawText, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the boolean value of the current token.
    /// Returns null if the current token is not a boolean.
    /// </summary>
    public bool? BooleanValue
    {
        get
        {
            if (currentToken?.Type == Parsing.TokenType.True)
                return true;
            if (currentToken?.Type == Parsing.TokenType.False)
                return false;
            return null;
        }
    }

    /// <summary>
    /// Gets the raw text representation of the current token (if available).
    /// </summary>
    public string? RawText => currentToken?.Text;

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns><c>true</c> if a token was read; <c>false</c> if end of document.</returns>
    /// <exception cref="Exceptions.KdlParseException">Thrown if a parsing error occurs.</exception>
    public bool Read()
    {
        EnsureNotDisposed();
        EnsureLexerInitialized();

        currentToken = lexer!.NextToken();
        return currentToken.Type != Parsing.TokenType.EndOfFile;
    }

    /// <summary>
    /// Skips the current token and advances to the next one.
    /// </summary>
    /// <returns><c>true</c> if a token was read after skipping; <c>false</c> if end of document.</returns>
    public bool Skip()
    {
        return Read();
    }

    /// <summary>
    /// Reads and returns the next value from the stream.
    /// </summary>
    /// <returns>The parsed value, or null if at end of document.</returns>
    /// <exception cref="Exceptions.KdlParseException">Thrown if a parsing error occurs.</exception>
    /// <remarks>
    /// This method reads a single KDL value (string, number, boolean, or null).
    /// </remarks>
    public KdlValue? ReadValue()
    {
        if (!Read())
            return null;

        return TokenType switch
        {
            KdlTokenType.String => new Values.KdlString(StringValue!),
            KdlTokenType.Number => CreateNumberValue(),
            KdlTokenType.True => Values.KdlBoolean.True,
            KdlTokenType.False => Values.KdlBoolean.False,
            KdlTokenType.Null => Values.KdlNull.Instance,
            KdlTokenType.Infinity => currentToken?.Value is double d && d > 0
                ? Values.KdlNumber.PositiveInfinity()
                : Values.KdlNumber.NegativeInfinity(),
            KdlTokenType.NaN => Values.KdlNumber.NaN(),
            _ => throw new Exceptions.KdlParseException(
                $"Expected a value, but found {TokenType}", Line, Column)
        };
    }

    private Values.KdlNumber CreateNumberValue()
    {
        // Check if it's a special format based on the raw text
        if (currentToken?.Value is string rawText)
        {
            if (rawText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return new Values.KdlNumber(rawText, Values.KdlNumberFormat.Hexadecimal);
            }
            else if (rawText.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            {
                return new Values.KdlNumber(rawText, Values.KdlNumberFormat.Octal);
            }
            else if (rawText.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            {
                return new Values.KdlNumber(rawText, Values.KdlNumberFormat.Binary);
            }
        }

        // Regular decimal number
        return new Values.KdlNumber(NumberValue!.Value);
    }

    private void EnsureLexerInitialized()
    {
        if (lexer == null)
        {
            var source = reader.ReadToEnd();
            lexer = new Lexer(source, settings);
        }
    }

    private void EnsureNotDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(KdlReader));
    }

    /// <summary>
    /// Releases all resources used by the reader.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;

        if (!leaveOpen)
        {
            reader?.Dispose();
        }

        disposed = true;
    }
}

