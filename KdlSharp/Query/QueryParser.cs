using System.Globalization;
using System.Text;
using KdlSharp.Exceptions;
using KdlSharp.Values;

namespace KdlSharp.Query;

/// <summary>
/// Parses KDL query strings into Query AST.
/// </summary>
public sealed class QueryParser
{
    private readonly string input;
    private int position;
    private int line;
    private int column;

    private QueryParser(string input)
    {
        this.input = input ?? throw new ArgumentNullException(nameof(input));
        position = 0;
        line = 1;
        column = 1;

        // Skip BOM if present
        if (this.input.Length > 0 && this.input[0] == '\uFEFF')
        {
            position++;
            column++;
        }
    }

    /// <summary>
    /// Parses a query string.
    /// </summary>
    public static Query Parse(string query)
    {
        var parser = new QueryParser(query);
        return parser.ParseQuery();
    }

    private Query ParseQuery()
    {
        var selectors = new List<Selector>();
        selectors.Add(ParseSelector());

        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd()) break;

            if (Match("||"))
            {
                SkipWhitespace();
                selectors.Add(ParseSelector());
            }
            else
            {
                break;
            }
        }

        SkipWhitespace();
        if (!IsAtEnd())
        {
            throw Error("Unexpected content after query");
        }

        return new Query(selectors);
    }

    private Selector ParseSelector()
    {
        var segments = new List<SelectorSegment>();
        var filter = ParseFilter();
        SelectorOperator? op = null;

        SkipWhitespace();

        // Check for operator
        if (Match(">>"))
        {
            op = SelectorOperator.Descendant;
        }
        else if (Match(">"))
        {
            op = SelectorOperator.Child;
        }
        else if (Match("++"))
        {
            op = SelectorOperator.FollowingSibling;
        }
        else if (Match("+"))
        {
            op = SelectorOperator.NextSibling;
        }

        segments.Add(new SelectorSegment(filter, op));

        // Parse remaining segments
        if (op.HasValue)
        {
            SkipWhitespace();
            var remaining = ParseSelectorSubsequent();
            segments.AddRange(remaining);
        }

        return new Selector(segments);
    }

    private List<SelectorSegment> ParseSelectorSubsequent()
    {
        var segments = new List<SelectorSegment>();
        var matchers = ParseMatchers();
        SelectorOperator? op = null;

        SkipWhitespace();

        // Check for operator
        if (Match(">>"))
        {
            op = SelectorOperator.Descendant;
        }
        else if (Match(">"))
        {
            op = SelectorOperator.Child;
        }
        else if (Match("++"))
        {
            op = SelectorOperator.FollowingSibling;
        }
        else if (Match("+"))
        {
            op = SelectorOperator.NextSibling;
        }

        segments.Add(new SelectorSegment(matchers, op));

        // Parse remaining segments
        if (op.HasValue)
        {
            SkipWhitespace();
            var remaining = ParseSelectorSubsequent();
            segments.AddRange(remaining);
        }

        return segments;
    }

    private Filter ParseFilter()
    {
        SkipWhitespace();

        // Check for top()
        if (Match("top("))
        {
            SkipWhitespace();
            if (!Match(")"))
            {
                throw Error("Expected ')' after 'top('");
            }
            return TopFilter.Instance;
        }

        return ParseMatchers();
    }

    private MatchersFilter ParseMatchers()
    {
        TypeMatcher? typeMatcher = null;
        string? nodeName = null;
        var accessorMatchers = new List<AccessorMatcher>();

        // Try to parse type matcher first
        if (Peek() == '(')
        {
            typeMatcher = ParseTypeMatcher();
            SkipWhitespace();

            // After type matcher, we can have optional string (node name) and accessor matchers
            if (Peek() == '"' || IsIdentifierStart(Peek()))
            {
                nodeName = ParseString();
                SkipWhitespace();
            }
        }
        // Try to parse node name
        else if (Peek() == '"' || IsIdentifierStart(Peek()))
        {
            nodeName = ParseString();
            SkipWhitespace();
        }

        // Parse accessor matchers
        while (Peek() == '[')
        {
            accessorMatchers.Add(ParseAccessorMatcher());
            SkipWhitespace();
        }

        // Must have at least one component
        if (typeMatcher == null && nodeName == null && accessorMatchers.Count == 0)
        {
            throw Error("Expected matcher (type, name, or accessor)");
        }

        return new MatchersFilter(typeMatcher, nodeName, accessorMatchers);
    }

    private TypeMatcher ParseTypeMatcher()
    {
        if (!Match("("))
        {
            throw Error("Expected '('");
        }

        SkipWhitespace();

        // Check for ()
        if (Peek() == ')')
        {
            Advance();
            return new TypeMatcher(null);
        }

        // Parse type name
        var typeName = ParseIdentifier();
        SkipWhitespace();

        if (!Match(")"))
        {
            throw Error("Expected ')'");
        }

        return new TypeMatcher(typeName);
    }

    private AccessorMatcher ParseAccessorMatcher()
    {
        if (!Match("["))
        {
            throw Error("Expected '['");
        }

        SkipWhitespace();

        // Check for empty []
        if (Peek() == ']')
        {
            Advance();
            return new AccessorMatcher();
        }

        // Try to parse accessor
        var accessor = TryParseAccessor();

        if (accessor != null)
        {
            SkipWhitespace();

            // Check if there's a comparison operator
            var op = TryParseMatcherOperator();
            if (op.HasValue)
            {
                SkipWhitespace();
                var right = ParseLiteral();
                SkipWhitespace();

                if (!Match("]"))
                {
                    throw Error("Expected ']'");
                }

                return new AccessorMatcher(new Comparison(accessor, op.Value, right));
            }

            // No comparison, just accessor
            if (!Match("]"))
            {
                throw Error("Expected ']'");
            }

            return new AccessorMatcher(accessor: accessor);
        }

        // If no accessor, we should have hit the ] case above
        throw Error("Expected accessor or ']'");
    }

    private Accessor? TryParseAccessor()
    {
        var savedPos = position;
        var savedLine = line;
        var savedCol = column;

        try
        {
            // Try val()
            if (Match("val("))
            {
                SkipWhitespace();

                // Check for index
                var index = 0;
                if (char.IsDigit(Peek()))
                {
                    index = ParseInteger();
                    SkipWhitespace();
                }

                if (!Match(")"))
                {
                    throw Error("Expected ')'");
                }
                return new ValAccessor(index);
            }

            // Try prop()
            if (Match("prop("))
            {
                SkipWhitespace();
                var propName = ParseString();
                SkipWhitespace();

                if (!Match(")"))
                {
                    throw Error("Expected ')'");
                }
                return new PropAccessor(propName);
            }

            // Try name()
            if (Match("name("))
            {
                SkipWhitespace();
                if (!Match(")"))
                {
                    throw Error("Expected ')'");
                }
                return NameAccessor.Instance;
            }

            // Try tag()
            if (Match("tag("))
            {
                SkipWhitespace();
                if (!Match(")"))
                {
                    throw Error("Expected ')'");
                }
                return TagAccessor.Instance;
            }

            // Try values()
            if (Match("values("))
            {
                SkipWhitespace();
                if (!Match(")"))
                {
                    throw Error("Expected ')'");
                }
                return ValuesAccessor.Instance;
            }

            // Try props()
            if (Match("props("))
            {
                SkipWhitespace();
                if (!Match(")"))
                {
                    throw Error("Expected ')'");
                }
                return PropsAccessor.Instance;
            }

            // Try plain string (shorthand for prop())
            if (Peek() == '"' || IsIdentifierStart(Peek()))
            {
                var propName = ParseString();
                return new PropAccessor(propName);
            }

            // No accessor found
            position = savedPos;
            line = savedLine;
            column = savedCol;
            return null;
        }
        catch
        {
            position = savedPos;
            line = savedLine;
            column = savedCol;
            return null;
        }
    }

    private MatcherOperator? TryParseMatcherOperator()
    {
        if (Match(">=")) return MatcherOperator.GreaterThanOrEqual;
        if (Match("<=")) return MatcherOperator.LessThanOrEqual;
        if (Match("!=")) return MatcherOperator.NotEqual;
        if (Match("^=")) return MatcherOperator.StartsWith;
        if (Match("$=")) return MatcherOperator.EndsWith;
        if (Match("*=")) return MatcherOperator.Contains;
        if (Match("=")) return MatcherOperator.Equal;
        if (Match(">")) return MatcherOperator.GreaterThan;
        if (Match("<")) return MatcherOperator.LessThan;
        return null;
    }

    private object ParseLiteral()
    {
        // Try type annotation
        if (Peek() == '(')
        {
            return ParseTypeMatcher();
        }

        // Try string
        if (Peek() == '"' || IsIdentifierStart(Peek()))
        {
            return ParseString();
        }

        // Try number
        if (char.IsDigit(Peek()) || Peek() == '-' || Peek() == '+')
        {
            return ParseNumber();
        }

        // Try keyword
        if (Match("#true")) return true;
        if (Match("#false")) return false;
        if (Match("#null")) return null!;
        if (Match("#inf")) return decimal.MaxValue;
        if (Match("#-inf")) return decimal.MinValue;
        if (Match("#nan")) return decimal.Zero; // Placeholder

        throw Error("Expected literal (string, number, or keyword)");
    }

    private string ParseString()
    {
        // Quoted string
        if (Peek() == '"')
        {
            Advance(); // Skip opening "
            var sb = new StringBuilder();

            while (!IsAtEnd() && Peek() != '"')
            {
                if (Peek() == '\\')
                {
                    Advance();
                    sb.Append(ParseEscape());
                }
                else
                {
                    sb.Append(Advance());
                }
            }

            if (!Match("\""))
            {
                throw Error("Unterminated string");
            }

            return sb.ToString();
        }

        // Bare identifier
        return ParseIdentifier();
    }

    private string ParseIdentifier()
    {
        if (!IsIdentifierStart(Peek()))
        {
            throw Error("Expected identifier");
        }

        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd() && IsIdentifierContinue(Peek()))
        {
            sb.Append(Advance());
        }

        return sb.ToString();
    }

    private int ParseInteger()
    {
        var sb = new StringBuilder();

        if (Peek() == '-' || Peek() == '+')
        {
            sb.Append(Advance());
        }

        if (!char.IsDigit(Peek()))
        {
            throw Error("Expected digit");
        }

        while (char.IsDigit(Peek()) || Peek() == '_')
        {
            if (Peek() != '_')
            {
                sb.Append(Advance());
            }
            else
            {
                Advance(); // Skip underscore
            }
        }

        return int.Parse(sb.ToString(), CultureInfo.InvariantCulture);
    }

    private decimal ParseNumber()
    {
        var sb = new StringBuilder();

        if (Peek() == '-' || Peek() == '+')
        {
            sb.Append(Advance());
        }

        // Parse integer part
        while (char.IsDigit(Peek()) || Peek() == '_')
        {
            if (Peek() != '_')
            {
                sb.Append(Advance());
            }
            else
            {
                Advance(); // Skip underscore
            }
        }

        // Parse decimal part
        if (Peek() == '.')
        {
            sb.Append(Advance());
            while (char.IsDigit(Peek()) || Peek() == '_')
            {
                if (Peek() != '_')
                {
                    sb.Append(Advance());
                }
                else
                {
                    Advance(); // Skip underscore
                }
            }
        }

        // Parse exponent
        if (Peek() == 'e' || Peek() == 'E')
        {
            sb.Append(Advance());
            if (Peek() == '-' || Peek() == '+')
            {
                sb.Append(Advance());
            }
            while (char.IsDigit(Peek()) || Peek() == '_')
            {
                if (Peek() != '_')
                {
                    sb.Append(Advance());
                }
                else
                {
                    Advance(); // Skip underscore
                }
            }
        }

        return decimal.Parse(sb.ToString(), CultureInfo.InvariantCulture);
    }

    private string ParseEscape()
    {
        var escapeChar = Peek();
        Advance();

        return escapeChar switch
        {
            'n' => "\n",
            'r' => "\r",
            't' => "\t",
            '\\' => "\\",
            '"' => "\"",
            'b' => "\b",
            'f' => "\f",
            's' => " ",
            'u' => ParseUnicodeEscape(),
            _ => throw Error($"Invalid escape sequence '\\{escapeChar}'")
        };
    }

    private string ParseUnicodeEscape()
    {
        // Unicode escape: \u{hex}
        if (!Match("{"))
        {
            throw Error("Expected '{' after \\u");
        }

        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '}')
        {
            if (!IsHexDigit(Peek()))
            {
                throw Error($"Invalid character in unicode escape: '{Peek()}'");
            }
            sb.Append(Advance());
        }

        if (!Match("}"))
        {
            throw Error("Unterminated unicode escape");
        }

        if (sb.Length == 0 || sb.Length > 6)
        {
            throw Error($"Unicode escape must have 1-6 hex digits, got {sb.Length}");
        }

        var codePoint = Convert.ToInt32(sb.ToString(), 16);

        // Validate code point range
        if (codePoint < 0 || codePoint > 0x10FFFF)
        {
            throw Error($"Unicode code point out of range: U+{codePoint:X}");
        }

        // Check for disallowed code points (surrogates)
        if (codePoint >= 0xD800 && codePoint <= 0xDFFF)
        {
            throw Error($"Unicode surrogate code points are not allowed: U+{codePoint:X}");
        }

        return char.ConvertFromUtf32(codePoint);
    }

    private bool IsHexDigit(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }

    private bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_' || c == '-' || c == '.';
    }

    private bool IsIdentifierContinue(char c)
    {
        return IsIdentifierStart(c) || char.IsDigit(c);
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
        {
            Advance();
        }
    }

    private bool Match(string text)
    {
        if (position + text.Length > input.Length)
        {
            return false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            if (input[position + i] != text[i])
            {
                return false;
            }
        }

        for (int i = 0; i < text.Length; i++)
        {
            Advance();
        }

        return true;
    }

    private char Peek()
    {
        if (IsAtEnd())
        {
            return '\0';
        }
        return input[position];
    }

    private char Advance()
    {
        if (IsAtEnd())
        {
            throw Error("Unexpected end of input");
        }

        var c = input[position];
        position++;

        if (c == '\n')
        {
            line++;
            column = 1;
        }
        else
        {
            column++;
        }

        return c;
    }

    private bool IsAtEnd()
    {
        return position >= input.Length;
    }

    private KdlQueryException Error(string message)
    {
        var contextStart = Math.Max(0, position - 20);
        var contextEnd = Math.Min(input.Length, position + 20);
        var context = input.Substring(contextStart, contextEnd - contextStart);
        return new KdlQueryException($"{message} at line {line}, column {column}", context);
    }
}

