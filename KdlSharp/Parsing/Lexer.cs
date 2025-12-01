using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using KdlSharp.Exceptions;
using KdlSharp.Settings;
using KdlSharp.Values;

namespace KdlSharp.Parsing;

/// <summary>
/// Lexer for tokenizing KDL source text.
/// </summary>
internal sealed class Lexer
{
    private readonly string source;
    private readonly KdlParserSettings settings;
    private int position;
    private int line;
    private int column;

    // Reusable StringBuilder to reduce allocations
    private readonly StringBuilder tokenBuilder = new StringBuilder(64);

    public Lexer(string source, KdlParserSettings settings)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        position = 0;
        line = 1;
        column = 1;

        // Skip BOM if present
        if (this.source.Length > 0 && this.source[0] == '\uFEFF')
        {
            Advance();
        }
    }

    public Token NextToken()
    {
        SkipWhitespaceAndComments();

        if (IsAtEnd())
        {
            return new Token(TokenType.EndOfFile, line, column, position);
        }

        var start = position;
        var startLine = line;
        var startColumn = column;
        var ch = Peek();

        // BOM is only allowed at the very start of the file (already handled in constructor)
        if (ch == '\uFEFF')
        {
            throw new KdlParseException("BOM (U+FEFF) is only allowed at the start of the file", line, column);
        }

        // Check for multi-character tokens first
        if (ch == '/' && PeekAhead(1) == '-')
        {
            Advance();
            Advance();
            return new Token(TokenType.Slashdash, startLine, startColumn, start);
        }

        switch (ch)
        {
            case '{':
                Advance();
                return new Token(TokenType.OpenBrace, startLine, startColumn, start);
            case '}':
                Advance();
                return new Token(TokenType.CloseBrace, startLine, startColumn, start);
            case '=':
                Advance();
                return new Token(TokenType.Equals, startLine, startColumn, start);
            case ';':
                Advance();
                return new Token(TokenType.Semicolon, startLine, startColumn, start);
            case '(':
                Advance();
                return new Token(TokenType.OpenParen, startLine, startColumn, start);
            case ')':
                Advance();
                return new Token(TokenType.CloseParen, startLine, startColumn, start);
            case '"':
                if (PeekAhead(1) == '"' && PeekAhead(2) == '"')
                {
                    return ScanMultiLineString(startLine, startColumn, start);
                }
                return ScanQuotedString(startLine, startColumn, start);
            case '#':
                return ScanHashPrefixed(startLine, startColumn, start);
            default:
                if (IsNewline(ch))
                {
                    return ScanNewline(startLine, startColumn, start);
                }
                if (IsDigit(ch) || (ch == '-' && IsDigit(PeekAhead(1))) || (ch == '+' && IsDigit(PeekAhead(1))))
                {
                    return ScanNumber(startLine, startColumn, start);
                }
                if (IsIdentifierStart(ch))
                {
                    return ScanIdentifier(startLine, startColumn, start);
                }
                throw new KdlParseException($"Unexpected character: '{ch}'", line, column);
        }
    }

    private Token ScanHashPrefixed(int startLine, int startColumn, int start)
    {
        Advance(); // consume first #

        // Check for raw string: count consecutive # symbols
        var hashCount = 1;
        while (!IsAtEnd() && Peek() == '#')
        {
            hashCount++;
            Advance();
        }

        // If we see a quote after the hashes, it's a raw string
        if (!IsAtEnd() && Peek() == '"')
        {
            if (PeekAhead(1) == '"' && PeekAhead(2) == '"')
            {
                return ScanRawMultiLineString(startLine, startColumn, start, hashCount);
            }
            return ScanRawString(startLine, startColumn, start, hashCount);
        }

        // Otherwise, it must be a keyword (only if hashCount == 1)
        // But keywords with # prefix are only valid in V2
        if (settings.TargetVersion == KdlVersion.V1)
        {
            throw new KdlParseException($"Keywords with # prefix are not valid in KDL v1", startLine, startColumn);
        }

        if (hashCount > 1)
        {
            throw new KdlParseException($"Invalid token: multiple # symbols not followed by a quote", startLine, startColumn);
        }

        var word = ScanWord();
        return word switch
        {
            "true" => new Token(TokenType.True, startLine, startColumn, start, "#true", true),
            "false" => new Token(TokenType.False, startLine, startColumn, start, "#false", false),
            "null" => new Token(TokenType.Null, startLine, startColumn, start, "#null", null),
            "inf" => new Token(TokenType.Infinity, startLine, startColumn, start, "#inf", double.PositiveInfinity),
            "-inf" => new Token(TokenType.Infinity, startLine, startColumn, start, "#-inf", double.NegativeInfinity),
            "nan" => new Token(TokenType.NaN, startLine, startColumn, start, "#nan", double.NaN),
            _ => throw new KdlParseException($"Unknown keyword: #{word}", startLine, startColumn)
        };
    }

    private Token ScanIdentifier(int startLine, int startColumn, int start)
    {
        tokenBuilder.Clear();
        while (!IsAtEnd() && IsIdentifierContinue(Peek()))
        {
            tokenBuilder.Append(Peek());
            Advance();
        }

        var text = tokenBuilder.ToString();

        // Validate that the identifier doesn't look like an invalid number
        // ".0" is invalid (looks like a number but missing leading digit)
        // "." or "+." or "-." are valid identifiers (no digits)
        // "1abc" is invalid (starts with digit)
        if (text.Length > 0)
        {
            // If it starts with a digit, it's invalid (should have been parsed as number)
            if (char.IsDigit(text[0]))
            {
                throw new KdlParseException(
                    $"Invalid identifier '{text}': identifiers cannot start with digits",
                    startLine, startColumn);
            }

            // If it starts with +/- or . followed by a digit, it's an invalid number
            if (text.Length >= 2 && (text[0] == '.' || text[0] == '+' || text[0] == '-'))
            {
                if (char.IsDigit(text[1]))
                {
                    throw new KdlParseException(
                        $"Invalid identifier '{text}': identifiers cannot look like numbers",
                        startLine, startColumn);
                }
            }
        }

        // Validate that identifier is followed by valid separator
        // (whitespace, special char, or EOF)
        // Note: '(' is NOT a valid separator - it requires whitespace
        // This prevents ambiguity like "foo(bar)" which should be invalid
        // But ')' IS valid (for closing type annotations before the identifier)
        if (!IsAtEnd())
        {
            var next = Peek();
            if (!IsWhitespace(next) && !IsNewline(next) &&
                next != '{' && next != '}' && next != ')' &&
                next != '=' && next != ';' && next != '/' && next != '\\')
            {
                throw new KdlParseException(
                    $"Invalid character after identifier: '{next}'. Identifiers must be followed by whitespace or a special character.",
                    line, column);
            }
        }

        // In v1 mode, bare keywords are allowed
        if (settings.TargetVersion == KdlVersion.V1)
        {
            switch (text)
            {
                case "true":
                    return new Token(TokenType.True, startLine, startColumn, start, text, true);
                case "false":
                    return new Token(TokenType.False, startLine, startColumn, start, text, false);
                case "null":
                    return new Token(TokenType.Null, startLine, startColumn, start, text, null);
                case "inf":
                    return new Token(TokenType.Infinity, startLine, startColumn, start, text, double.PositiveInfinity);
                case "nan":
                    return new Token(TokenType.NaN, startLine, startColumn, start, text, double.NaN);
            }
        }
        else if (settings.TargetVersion == KdlVersion.V2)
        {
            // In v2 mode, reject bare keywords with helpful message
            if (text is "true" or "false" or "null" or "inf" or "nan")
            {
                throw new KdlParseException(
                    $"Bare keyword '{text}' is not valid in KDL v2 (line {startLine}, column {startColumn}).\n\n" +
                    $"Did you mean '#{text}'?\n\n" +
                    "If you're parsing a legacy KDL v1 document, use:\n" +
                    "    var settings = new KdlParserSettings { TargetVersion = KdlVersion.V1 };\n" +
                    "    var doc = KdlDocument.Parse(kdl, settings);",
                    startLine, startColumn);
            }
        }

        if (text == "-inf")
        {
            if (settings.TargetVersion == KdlVersion.V1)
            {
                return new Token(TokenType.Infinity, startLine, startColumn, start, text, double.NegativeInfinity);
            }
            else if (settings.TargetVersion == KdlVersion.V2)
            {
                throw new KdlParseException(
                    $"Bare keyword '-inf' is not valid in KDL v2 (line {startLine}, column {startColumn}).\n\n" +
                    $"Did you mean '#-inf'?\n\n" +
                    "If you're parsing a legacy KDL v1 document, use:\n" +
                    "    var settings = new KdlParserSettings { TargetVersion = KdlVersion.V1 };\n" +
                    "    var doc = KdlDocument.Parse(kdl, settings);",
                    startLine, startColumn);
            }
        }

        return new Token(TokenType.String, startLine, startColumn, start, text, text, KdlStringType.Identifier);
    }

    private Token ScanQuotedString(int startLine, int startColumn, int start)
    {
        Advance(); // consume opening "
        tokenBuilder.Clear();

        while (!IsAtEnd() && Peek() != '"')
        {
            // Single-line strings cannot contain unescaped newlines
            if (IsNewline(Peek()))
            {
                throw new KdlParseException("Single-line strings cannot contain unescaped newlines. Use \"\"\" for multi-line strings.", line, column);
            }

            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd())
                {
                    throw new KdlParseException("Unexpected end of file in string escape", line, column);
                }

                var escapeChar = Peek();
                switch (escapeChar)
                {
                    case 'n':
                        tokenBuilder.Append('\n');
                        Advance();
                        break;
                    case 'r':
                        tokenBuilder.Append('\r');
                        Advance();
                        break;
                    case 't':
                        tokenBuilder.Append('\t');
                        Advance();
                        break;
                    case '\\':
                        tokenBuilder.Append('\\');
                        Advance();
                        break;
                    case '"':
                        tokenBuilder.Append('"');
                        Advance();
                        break;
                    case 'b':
                        tokenBuilder.Append('\b');
                        Advance();
                        break;
                    case 'f':
                        tokenBuilder.Append('\f');
                        Advance();
                        break;
                    case 's':
                        tokenBuilder.Append(' ');
                        Advance();
                        break;
                    case 'u':
                        // Unicode escape: \u{hex}
                        Advance(); // consume 'u'
                        if (Peek() != '{')
                        {
                            throw new KdlParseException($"Invalid unicode escape: expected '{{' after \\u", line, column);
                        }
                        Advance(); // consume '{'

                        var hexStart = position;
                        var hexLength = 0;
                        while (!IsAtEnd() && Peek() != '}')
                        {
                            if (!IsHexDigit(Peek()))
                            {
                                throw new KdlParseException($"Invalid character in unicode escape: '{Peek()}'", line, column);
                            }
                            hexLength++;
                            Advance();
                        }

                        if (IsAtEnd() || Peek() != '}')
                        {
                            throw new KdlParseException("Unterminated unicode escape", line, column);
                        }
                        Advance(); // consume '}'

                        if (hexLength == 0 || hexLength > 6)
                        {
                            throw new KdlParseException($"Unicode escape must have 1-6 hex digits, got {hexLength}", line, column);
                        }

                        var codePoint = Convert.ToInt32(source.Substring(hexStart, hexLength), 16);

                        // Validate code point range
                        if (codePoint < 0 || codePoint > 0x10FFFF)
                        {
                            throw new KdlParseException($"Unicode code point out of range: U+{codePoint:X}", line, column);
                        }

                        // Check for disallowed code points (surrogates)
                        if (codePoint >= 0xD800 && codePoint <= 0xDFFF)
                        {
                            throw new KdlParseException($"Unicode surrogate code points are not allowed: U+{codePoint:X}", line, column);
                        }

                        tokenBuilder.Append(char.ConvertFromUtf32(codePoint));
                        break;
                    default:
                        // Whitespace escape: \ followed by whitespace consumes all whitespace including newlines
                        if (IsWhitespace(escapeChar) || IsNewline(escapeChar))
                        {
                            // Skip ALL whitespace characters including multiple newlines
                            while (!IsAtEnd() && (IsWhitespace(Peek()) || IsNewline(Peek())))
                            {
                                Advance();
                            }
                        }
                        else
                        {
                            throw new KdlParseException($"Invalid escape sequence: \\{escapeChar}", line, column);
                        }
                        break;
                }
            }
            else
            {
                tokenBuilder.Append(Peek());
                Advance();
            }
        }

        if (IsAtEnd())
        {
            throw new KdlParseException("Unterminated string", line, column);
        }

        Advance(); // consume closing "

        // Validate that the string is followed by whitespace or a valid separator
        if (!IsAtEnd())
        {
            var next = Peek();
            if (!IsWhitespace(next) && !IsNewline(next) &&
                next != '{' && next != '}' && next != '(' && next != ')' &&
                next != '=' && next != ';' && next != '/' && next != '\\')
            {
                throw new KdlParseException(
                    $"Invalid character after string: '{next}'. Strings must be followed by whitespace or a special character.",
                    line, column);
            }
        }

        var result = tokenBuilder.ToString();
        return new Token(TokenType.String, startLine, startColumn, start, result, result, KdlStringType.Quoted);
    }

    private Token ScanMultiLineString(int startLine, int startColumn, int start)
    {
        // Multi-line strings are v2-only
        if (settings.TargetVersion == KdlVersion.V1)
        {
            throw new KdlParseException(
                $"Multi-line strings (\"\"\"....\"\"\") are not supported in KDL v1 (line {startLine}, column {startColumn}).\n\n" +
                "Multi-line strings are a KDL v2 feature. Either:\n" +
                "1. Use single-line strings with \\n escapes for v1 documents\n" +
                "2. Parse this document with v2: var settings = new KdlParserSettings { TargetVersion = KdlVersion.V2 };",
                startLine, startColumn);
        }

        // Consume opening """
        Advance();
        Advance();
        Advance();

        // The opening """ must be followed by a newline (which becomes part of the content initially)
        // Per KDL spec: resolve whitespace escapes first, then check prefix, then resolve other escapes
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var hasNewline = false;

        while (!IsAtEnd())
        {
            // Check for closing """ (but not if preceded by escape)
            if (Peek() == '"' && PeekAhead(1) == '"' && PeekAhead(2) == '"')
            {
                // Multi-line strings must contain at least one newline
                if (!hasNewline)
                {
                    throw new KdlParseException("Multi-line strings must span multiple lines (contain at least one newline)", line, column);
                }

                // Found closing """, capture current line (this is the indentation line)
                var closingIndent = currentLine.ToString();

                // Validate that the closing line contains only whitespace (as required by KDL spec)
                // If it contains non-whitespace, it means a whitespace escape consumed the newline
                // placing the closing """ on a line with content, which is invalid
                if (closingIndent.Length > 0 && !closingIndent.All(c => IsWhitespace(c)))
                {
                    throw new KdlParseException(
                        "Multi-line string: closing \"\"\" must be on a line containing only whitespace. " +
                        "Whitespace escapes cannot consume the final line's newline or prefix.",
                        line, column);
                }

                Advance();
                Advance();
                Advance();

                // Process dedentation: remove the closing line's leading whitespace from all lines
                // Per KDL spec: all non-whitespace-only lines must start with exactly the same prefix
                // Note: At this point, whitespace escapes have been resolved, but other escapes (like \s, \n) have NOT
                var dedentedLines = new List<string>();
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    // Check if line is whitespace-only (all characters are whitespace OR escape sequences)
                    // A line with only \s or \t escapes is still "whitespace-only" in terms of prefix matching
                    var isWhitespaceOnly = line.Length == 0 || IsLineWhitespaceOrEscapes(line);

                    if (isWhitespaceOnly)
                    {
                        // Whitespace-only lines become empty lines regardless of content
                        // Per KDL spec: all characters on whitespace-only lines are ignored
                        dedentedLines.Add("");
                    }
                    else if (line.StartsWith(closingIndent))
                    {
                        // Line starts with the required prefix, remove it and resolve escapes
                        var dedented = line.Substring(closingIndent.Length);
                        dedentedLines.Add(ResolveNonWhitespaceEscapes(dedented));
                    }
                    else
                    {
                        // Line doesn't start with the required prefix - this is an error
                        throw new KdlParseException(
                            $"Multi-line string: all lines must start with the same whitespace prefix as the closing line (exactly matching codepoints). " +
                            $"Expected prefix of {closingIndent.Length} characters but line {i + 1} doesn't match.",
                            startLine, startColumn);
                    }
                }

                // Per KDL spec: remove first and last newlines
                // In our representation, this means:
                // - Always remove the first line (represents content between first newline and next newline)
                // - Only remove the last line if it's empty AND we have more than one line
                //   (to handle the case where first and last newline are the same)
                if (dedentedLines.Count > 0)
                {
                    dedentedLines.RemoveAt(0);
                }

                // Remove last line only if it's empty and it's the ONLY line left
                // This handles the case of """ followed immediately by """ with one newline between
                if (dedentedLines.Count == 1 && dedentedLines[0].Length == 0)
                {
                    dedentedLines.RemoveAt(0);
                }

                // Join with LF (normalized newlines)
                var value = string.Join("\n", dedentedLines);

                return new Token(TokenType.String, startLine, startColumn, start, value, value, KdlStringType.MultiLine);
            }

            if (Peek() == '\\')
            {
                // Handle escape sequences in multi-line strings
                // Only resolve whitespace escapes now; other escapes are kept literal for later
                Advance(); // consume backslash
                if (IsAtEnd())
                {
                    throw new KdlParseException("Unexpected end of file in string escape", line, column);
                }

                var escapeChar = Peek();
                // Whitespace escape: \ followed by whitespace consumes all whitespace including newlines
                if (IsWhitespace(escapeChar) || IsNewline(escapeChar))
                {
                    // Skip ALL whitespace characters including multiple newlines
                    while (!IsAtEnd() && (IsWhitespace(Peek()) || IsNewline(Peek())))
                    {
                        Advance();
                    }
                }
                else
                {
                    // Not a whitespace escape - keep the backslash and character literal
                    // They will be resolved after dedentation
                    currentLine.Append('\\');
                    currentLine.Append(escapeChar);
                    Advance();
                }
            }
            else if (IsNewline(Peek()))
            {
                hasNewline = true; // Mark that we've seen a newline
                // Save current line and start new one
                lines.Add(currentLine.ToString());
                currentLine.Clear();

                // Consume newline (Advance handles CRLF internally)
                Advance();
            }
            else
            {
                currentLine.Append(Peek());
                Advance();
            }
        }

        throw new KdlParseException("Unterminated multi-line string", line, column);
    }

    // Helper method to check if a line contains only literal whitespace
    // Per KDL spec: "lines containing only literal whitespace characters, not including whitespace escapes like \t"
    private bool IsLineWhitespaceOrEscapes(string line)
    {
        // A line is whitespace-only if ALL characters are literal whitespace
        // Escape sequences (even whitespace-producing ones like \s or \t) mean it's NOT whitespace-only
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '\\')
            {
                // Any escape sequence means this is NOT a whitespace-only line
                return false;
            }
            if (!IsWhitespace(line[i]))
            {
                return false;
            }
        }
        return true;
    }

    // Helper method to resolve non-whitespace escapes after dedentation
    private string ResolveNonWhitespaceEscapes(string text)
    {
        var sb = new StringBuilder();
        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                var escapeChar = text[i + 1];
                switch (escapeChar)
                {
                    case 'n':
                        sb.Append('\n');
                        i += 2;
                        break;
                    case 'r':
                        sb.Append('\r');
                        i += 2;
                        break;
                    case 't':
                        sb.Append('\t');
                        i += 2;
                        break;
                    case '\\':
                        sb.Append('\\');
                        i += 2;
                        break;
                    case '"':
                        sb.Append('"');
                        i += 2;
                        break;
                    case 'b':
                        sb.Append('\b');
                        i += 2;
                        break;
                    case 'f':
                        sb.Append('\f');
                        i += 2;
                        break;
                    case 's':
                        sb.Append(' ');
                        i += 2;
                        break;
                    case 'u':
                        // Unicode escape: \u{hex}
                        i += 2; // skip \u
                        if (i < text.Length && text[i] == '{')
                        {
                            i++; // skip {
                            var hexStart = i;
                            while (i < text.Length && text[i] != '}')
                            {
                                i++;
                            }
                            if (i < text.Length)
                            {
                                var hexStr = text.Substring(hexStart, i - hexStart);
                                var codePoint = Convert.ToInt32(hexStr, 16);
                                sb.Append(char.ConvertFromUtf32(codePoint));
                                i++; // skip }
                            }
                        }
                        break;
                    default:
                        // Invalid escape - this shouldn't happen if we validated correctly
                        throw new KdlParseException($"Invalid escape sequence: \\{escapeChar}", line, column);
                }
            }
            else
            {
                sb.Append(text[i]);
                i++;
            }
        }
        return sb.ToString();
    }

    private Token ScanRawString(int startLine, int startColumn, int start, int hashCount)
    {
        Advance(); // consume opening "
        tokenBuilder.Clear();

        while (!IsAtEnd())
        {
            // Single-quote raw strings (not triple-quote) cannot contain newlines
            if (IsNewline(Peek()))
            {
                throw new KdlParseException("Single-line raw strings cannot contain newlines. Use #\"\"\" for multi-line raw strings.", line, column);
            }

            if (Peek() == '"')
            {
                var endHashCount = 0;
                var pos = position + 1;
                while (pos < source.Length && source[pos] == '#')
                {
                    endHashCount++;
                    pos++;
                }

                if (endHashCount == hashCount)
                {
                    Advance(); // consume closing "
                    for (int i = 0; i < hashCount; i++)
                    {
                        Advance(); // consume closing #
                    }
                    var value = tokenBuilder.ToString();
                    return new Token(TokenType.String, startLine, startColumn, start, value, value, KdlStringType.Raw, rawHashCount: hashCount, isRawMultiLine: false);
                }
            }
            tokenBuilder.Append(Peek());
            Advance();
        }

        throw new KdlParseException("Unterminated raw string", line, column);
    }

    private Token ScanRawMultiLineString(int startLine, int startColumn, int start, int hashCount)
    {
        // Consume opening """
        Advance();
        Advance();
        Advance();

        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var hasNewline = false;

        while (!IsAtEnd())
        {
            // Check for closing """
            if (Peek() == '"' && PeekAhead(1) == '"' && PeekAhead(2) == '"')
            {
                var endHashCount = 0;
                var pos = position + 3;
                while (pos < source.Length && source[pos] == '#')
                {
                    endHashCount++;
                    pos++;
                }

                if (endHashCount == hashCount)
                {
                    // Multi-line strings must contain at least one newline
                    if (!hasNewline)
                    {
                        throw new KdlParseException("Multi-line strings must span multiple lines (contain at least one newline)", line, column);
                    }

                    // Found matching closing delimiter
                    // The current line is the indentation reference line
                    var closingIndent = currentLine.ToString();

                    // Validate that the closing line contains only whitespace
                    // Raw strings don't have escapes, but we still need this check for consistency
                    if (closingIndent.Length > 0 && !closingIndent.All(c => IsWhitespace(c)))
                    {
                        throw new KdlParseException(
                            "Multi-line raw string: closing delimiter must be on a line containing only whitespace.",
                            line, column);
                    }

                    // Consume closing """#...#
                    Advance();
                    Advance();
                    Advance();
                    for (int i = 0; i < hashCount; i++)
                    {
                        Advance();
                    }

                    // Process dedentation using closing line's indentation
                    // Per KDL spec: all non-whitespace-only lines must start with exactly the same prefix
                    var dedentedLines = new List<string>();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        // Check if line is whitespace-only (all characters are whitespace)
                        var isWhitespaceOnly = line.Length == 0 || line.All(c => IsWhitespace(c));

                        if (isWhitespaceOnly)
                        {
                            // Whitespace-only lines become empty lines regardless of content
                            dedentedLines.Add("");
                        }
                        else if (line.StartsWith(closingIndent))
                        {
                            // Line starts with the required prefix, remove it
                            dedentedLines.Add(line.Substring(closingIndent.Length));
                        }
                        else
                        {
                            // Line doesn't start with the required prefix - this is an error
                            throw new KdlParseException(
                                $"Multi-line raw string: all lines must start with the same whitespace prefix as the closing line (exactly matching codepoints). " +
                                $"Expected prefix of {closingIndent.Length} characters but line {i + 1} doesn't match.",
                                startLine, startColumn);
                        }
                    }

                    // Per KDL spec: remove first and last newlines
                    // Same logic as regular multiline strings
                    if (dedentedLines.Count > 0)
                    {
                        dedentedLines.RemoveAt(0);
                    }

                    // Remove last line only if it's empty and it's the ONLY line left
                    if (dedentedLines.Count == 1 && dedentedLines[0].Length == 0)
                    {
                        dedentedLines.RemoveAt(0);
                    }

                    // Join with LF
                    var value = string.Join("\n", dedentedLines);

                    // Raw multi-line strings are still considered "Raw" type for formatting purposes
                    // Preserve the closing line's indent for round-trip fidelity
                    return new Token(TokenType.String, startLine, startColumn, start, value, value, KdlStringType.Raw, rawHashCount: hashCount, isRawMultiLine: true, rawMultiLineIndent: closingIndent);
                }
            }

            if (IsNewline(Peek()))
            {
                hasNewline = true;
                // Save current line and start new one
                lines.Add(currentLine.ToString());
                currentLine.Clear();

                // Consume newline (Advance handles CRLF internally)
                Advance();
            }
            else
            {
                currentLine.Append(Peek());
                Advance();
            }
        }

        throw new KdlParseException("Unterminated raw multi-line string", line, column);
    }

    private Token ScanNumber(int startLine, int startColumn, int start)
    {
        tokenBuilder.Clear();
        var hasUnderscore = false;

        if (Peek() == '-')
        {
            tokenBuilder.Append(Peek());
            Advance();
        }
        else if (Peek() == '+')
        {
            tokenBuilder.Append(Peek());
            Advance();
        }

        // Check for hex/octal/binary
        if (Peek() == '0' && !IsAtEnd())
        {
            var next = PeekAhead(1);
            if (next == 'x' || next == 'X')
            {
                Advance(); // consume 0
                Advance(); // consume x

                // Underscore cannot come immediately after prefix
                if (Peek() == '_')
                {
                    throw new KdlParseException("Underscore cannot appear immediately after number prefix", startLine, startColumn);
                }

                if (!IsHexDigit(Peek()))
                {
                    throw new KdlParseException($"Invalid hexadecimal number: expected hex digit after 0x", startLine, startColumn);
                }

                while (!IsAtEnd() && (IsHexDigit(Peek()) || Peek() == '_'))
                {
                    if (Peek() == '_')
                    {
                        hasUnderscore = true;
                        Advance();
                        continue;
                    }
                    // Check for invalid characters
                    if (!IsHexDigit(Peek()))
                    {
                        throw new KdlParseException($"Invalid character in hexadecimal number: '{Peek()}'", line, column);
                    }
                    tokenBuilder.Append(Peek());
                    Advance();
                }

                if (tokenBuilder.Length == 0)
                {
                    throw new KdlParseException("Hexadecimal number must have at least one digit", startLine, startColumn);
                }

                // Validate that hex number is followed by valid separator
                if (!IsAtEnd() && IsIdentifierContinue(Peek()))
                {
                    throw new KdlParseException(
                        $"Invalid character after hexadecimal number: '{Peek()}'. Numbers must be followed by whitespace or a special character.",
                        line, column);
                }

                // Store hex string and base info for later conversion
                var hexText = "0x" + tokenBuilder.ToString();
                return new Token(TokenType.Number, startLine, startColumn, start, hexText, hexText);
            }
            else if (next == 'o' || next == 'O')
            {
                Advance(); // consume 0
                Advance(); // consume o

                // Underscore cannot come immediately after prefix
                if (Peek() == '_')
                {
                    throw new KdlParseException("Underscore cannot appear immediately after number prefix", startLine, startColumn);
                }

                if (!IsOctalDigit(Peek()))
                {
                    throw new KdlParseException($"Invalid octal number: expected octal digit after 0o", startLine, startColumn);
                }

                while (!IsAtEnd() && (IsOctalDigit(Peek()) || Peek() == '_'))
                {
                    if (Peek() == '_')
                    {
                        hasUnderscore = true;
                        Advance();
                        continue;
                    }
                    // Check for invalid characters (8 or 9 in octal)
                    if (!IsOctalDigit(Peek()))
                    {
                        throw new KdlParseException($"Invalid character in octal number: '{Peek()}'", line, column);
                    }
                    tokenBuilder.Append(Peek());
                    Advance();
                }

                if (tokenBuilder.Length == 0)
                {
                    throw new KdlParseException("Octal number must have at least one digit", startLine, startColumn);
                }

                // Validate that octal number is followed by valid separator
                if (!IsAtEnd() && IsIdentifierContinue(Peek()))
                {
                    throw new KdlParseException(
                        $"Invalid character after octal number: '{Peek()}'. Numbers must be followed by whitespace or a special character.",
                        line, column);
                }

                // Store octal string and base info for later conversion
                var octalText = "0o" + tokenBuilder.ToString();
                return new Token(TokenType.Number, startLine, startColumn, start, octalText, octalText);
            }
            else if (next == 'b' || next == 'B')
            {
                Advance(); // consume 0
                Advance(); // consume b

                // Underscore cannot come immediately after prefix
                if (Peek() == '_')
                {
                    throw new KdlParseException("Underscore cannot appear immediately after number prefix", startLine, startColumn);
                }

                if (!IsBinaryDigit(Peek()))
                {
                    throw new KdlParseException($"Invalid binary number: expected binary digit after 0b", startLine, startColumn);
                }

                while (!IsAtEnd() && (IsBinaryDigit(Peek()) || Peek() == '_'))
                {
                    if (Peek() == '_')
                    {
                        hasUnderscore = true;
                        Advance();
                        continue;
                    }
                    // Check for invalid characters (2-9, a-z, etc. in binary)
                    if (!IsBinaryDigit(Peek()))
                    {
                        throw new KdlParseException($"Invalid character in binary number: '{Peek()}'", line, column);
                    }
                    tokenBuilder.Append(Peek());
                    Advance();
                }

                if (tokenBuilder.Length == 0)
                {
                    throw new KdlParseException("Binary number must have at least one digit", startLine, startColumn);
                }

                // Validate that binary number is followed by valid separator
                if (!IsAtEnd() && IsIdentifierContinue(Peek()))
                {
                    throw new KdlParseException(
                        $"Invalid character after binary number: '{Peek()}'. Numbers must be followed by whitespace or a special character.",
                        line, column);
                }

                // Store binary string and base info for later conversion
                var binaryText = "0b" + tokenBuilder.ToString();
                return new Token(TokenType.Number, startLine, startColumn, start, binaryText, binaryText);
            }
        }

        // Scan decimal number
        var hasDot = false;
        var hasExponent = false;

        while (!IsAtEnd())
        {
            var ch = Peek();

            if (ch == '_')
            {
                hasUnderscore = true;
                Advance();
                continue;
            }
            else if (IsDigit(ch))
            {
                tokenBuilder.Append(ch);
                Advance();
            }
            else if (ch == '.')
            {
                if (hasDot)
                {
                    throw new KdlParseException("Number cannot have multiple decimal points", startLine, startColumn);
                }
                if (hasExponent)
                {
                    throw new KdlParseException("Decimal point cannot appear after exponent", startLine, startColumn);
                }
                // Check that underscore doesn't come immediately after dot
                if (tokenBuilder.Length > 0 && tokenBuilder[tokenBuilder.Length - 1] == '_')
                {
                    throw new KdlParseException("Underscore cannot appear immediately before decimal point", startLine, startColumn);
                }
                if (PeekAhead(1) == '_')
                {
                    throw new KdlParseException("Underscore cannot appear immediately after decimal point", startLine, startColumn);
                }
                if (!IsDigit(PeekAhead(1)))
                {
                    throw new KdlParseException("Decimal point must be followed by at least one digit", startLine, startColumn);
                }
                hasDot = true;
                tokenBuilder.Append(ch);
                Advance();
            }
            else if ((ch == 'e' || ch == 'E') && !hasExponent)
            {
                hasExponent = true;
                tokenBuilder.Append(ch);
                Advance();

                // Handle optional sign after exponent
                if (Peek() == '+' || Peek() == '-')
                {
                    tokenBuilder.Append(Peek());
                    Advance();
                }

                // Must have at least one digit after exponent
                if (!IsDigit(Peek()))
                {
                    throw new KdlParseException("Exponent must be followed by at least one digit", startLine, startColumn);
                }
            }
            else
            {
                break;
            }
        }

        // Validate that number is followed by valid separator
        if (!IsAtEnd())
        {
            var next = Peek();
            if (IsIdentifierContinue(next))
            {
                throw new KdlParseException(
                    $"Invalid character after number: '{next}'. Numbers must be followed by whitespace or a special character.",
                    line, column);
            }
        }

        // Validate underscores in numbers (v2-only feature)
        if (hasUnderscore && settings.TargetVersion == KdlVersion.V1)
        {
            throw new KdlParseException(
                $"Underscores in numbers are not supported in KDL v1 (line {startLine}, column {startColumn}).\n\n" +
                "Underscores in numbers (e.g., 1_000_000) are a KDL v2 feature. Either:\n" +
                "1. Remove the underscores for v1 documents\n" +
                "2. Parse this document with v2: var settings = new KdlParserSettings { TargetVersion = KdlVersion.V2 };",
                startLine, startColumn);
        }

        // Try to parse as decimal
        var numberStr = tokenBuilder.ToString();
        if (decimal.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return new Token(TokenType.Number, startLine, startColumn, start, numberStr, decimalValue);
        }

        // If parsing failed, check if it's because the exponent is too large
        // In that case, treat it as infinity
        if (hasExponent && (numberStr.Contains("E") || numberStr.Contains("e")))
        {
            // Parse as double to see if it's infinity
            if (double.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
            {
                if (double.IsPositiveInfinity(doubleValue))
                {
                    return new Token(TokenType.Number, startLine, startColumn, start, numberStr, decimal.MaxValue);
                }
                else if (double.IsNegativeInfinity(doubleValue))
                {
                    return new Token(TokenType.Number, startLine, startColumn, start, numberStr, decimal.MinValue);
                }
            }
        }

        throw new KdlParseException($"Invalid number: {numberStr}", startLine, startColumn);
    }

    private Token ScanNewline(int startLine, int startColumn, int start)
    {
        // Advance() already handles CRLF internally, so we just need to call it once
        Advance();
        return new Token(TokenType.Newline, startLine, startColumn, start);
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            var ch = Peek();

            if (IsWhitespace(ch) && !IsNewline(ch))
            {
                Advance();
            }
            else if (ch == '\\')
            {
                // Line continuation: \ followed by optional whitespace/comments and newline
                Advance(); // consume \

                // Skip whitespace and comments until newline
                while (!IsAtEnd() && !IsNewline(Peek()))
                {
                    if (Peek() == '/' && PeekAhead(1) == '/')
                    {
                        // Skip single-line comment
                        while (!IsAtEnd() && !IsNewline(Peek()))
                        {
                            Advance();
                        }
                        break;
                    }
                    else if (Peek() == '/' && PeekAhead(1) == '*')
                    {
                        // Multi-line comment
                        Advance();
                        Advance();
                        var depth = 1;
                        while (!IsAtEnd() && depth > 0)
                        {
                            if (Peek() == '/' && PeekAhead(1) == '*')
                            {
                                depth++;
                                Advance();
                                Advance();
                            }
                            else if (Peek() == '*' && PeekAhead(1) == '/')
                            {
                                depth--;
                                Advance();
                                Advance();
                            }
                            else
                            {
                                Advance();
                            }
                        }
                    }
                    else if (!IsWhitespace(Peek()))
                    {
                        throw new KdlParseException("Invalid line continuation: must be followed by whitespace and newline", line, column);
                    }
                    else
                    {
                        Advance();
                    }
                }

                // Consume the newline
                if (!IsAtEnd() && IsNewline(Peek()))
                {
                    Advance();
                }
            }
            else if (ch == '/' && PeekAhead(1) == '/')
            {
                // Single-line comment
                while (!IsAtEnd() && !IsNewline(Peek()))
                {
                    Advance();
                }
            }
            else if (ch == '/' && PeekAhead(1) == '*')
            {
                // Multi-line comment
                Advance();
                Advance();
                var depth = 1;
                while (!IsAtEnd() && depth > 0)
                {
                    if (Peek() == '/' && PeekAhead(1) == '*')
                    {
                        depth++;
                        Advance();
                        Advance();
                    }
                    else if (Peek() == '*' && PeekAhead(1) == '/')
                    {
                        depth--;
                        Advance();
                        Advance();
                    }
                    else
                    {
                        Advance();
                    }
                }
            }
            else
            {
                break;
            }
        }
    }

    private string ScanWord()
    {
        tokenBuilder.Clear();
        while (!IsAtEnd() && (IsIdentifierContinue(Peek()) || Peek() == '-'))
        {
            tokenBuilder.Append(Peek());
            Advance();
        }
        return tokenBuilder.ToString();
    }

    private bool IsWhitespace(char ch) => char.IsWhiteSpace(ch);
    private bool IsNewline(char ch) => ch is '\n' or '\r' or '\u0085' or '\u000B' or '\u000C' or '\u2028' or '\u2029';
    private bool IsDigit(char ch) => ch >= '0' && ch <= '9';
    private bool IsHexDigit(char ch) => IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
    private bool IsOctalDigit(char ch) => ch >= '0' && ch <= '7';
    private bool IsBinaryDigit(char ch) => ch is '0' or '1';

    private bool IsIdentifierStart(char ch) => Utilities.StringEscaper.IsIdentifierStart(ch);

    private bool IsIdentifierContinue(char ch) => Utilities.StringEscaper.IsIdentifierContinue(ch);

    private bool IsAtEnd() => position >= source.Length;
    private char Peek() => IsAtEnd() ? '\0' : source[position];
    private char PeekAhead(int offset) => position + offset >= source.Length ? '\0' : source[position + offset];

    private void Advance()
    {
        if (IsAtEnd())
        {
            return;
        }

        if (IsNewline(source[position]))
        {
            line++;
            column = 1;
            if (source[position] == '\r' && position + 1 < source.Length && source[position + 1] == '\n')
            {
                position++;
            }
        }
        else
        {
            column++;
        }
        position++;
    }
}




