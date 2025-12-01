using KdlSharp.Exceptions;
using KdlSharp.Settings;
using KdlSharp.Values;

namespace KdlSharp.Parsing;

/// <summary>
/// Provides methods for parsing KDL documents.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thread Safety</b>: This class is not thread-safe. Create separate parser instances for concurrent parsing operations.
/// </para>
/// <para>
/// <b>Usage</b>: For most scenarios, use the static methods on <see cref="KdlDocument"/> instead of creating parser instances directly.
/// Create parser instances when you need to reuse custom settings across multiple parsing operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple parsing (recommended)
/// var doc = KdlDocument.Parse("node value=123");
///
/// // With custom settings
/// var parser = new KdlParser(new KdlParserSettings
/// {
///     TargetVersion = KdlVersion.V1
/// });
/// var doc = parser.Parse(kdlText);
/// </code>
/// </example>
public sealed class KdlParser
{
    private readonly KdlParserSettings settings;

    /// <summary>
    /// Initializes a new parser with optional settings.
    /// </summary>
    /// <param name="settings">Optional parser settings to control parsing behavior.</param>
    public KdlParser(KdlParserSettings? settings = null)
    {
        this.settings = settings ?? new KdlParserSettings();
    }

    /// <summary>
    /// Parses a KDL document from a string.
    /// </summary>
    /// <param name="kdl">The KDL text to parse.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="kdl"/> is null.</exception>
    /// <exception cref="KdlParseException">Thrown when the KDL text contains syntax errors.</exception>
    public KdlDocument Parse(string kdl)
    {
        if (kdl == null)
        {
            throw new ArgumentNullException(nameof(kdl));
        }

        // If Auto mode and no version marker, try V2 first, then V1
        if (settings.TargetVersion == KdlVersion.Auto)
        {
            // Try V2 first
            try
            {
                var v2Settings = settings.Clone();
                v2Settings.TargetVersion = KdlVersion.V2;
                var v2Parser = new KdlParser(v2Settings);
                return v2Parser.Parse(kdl);
            }
            catch (KdlParseException)
            {
                // V2 failed, try V1
                var v1Settings = settings.Clone();
                v1Settings.TargetVersion = KdlVersion.V1;
                var v1Parser = new KdlParser(v1Settings);
                return v1Parser.Parse(kdl);
            }
        }

        var lexer = new Lexer(kdl, settings);
        return ParseDocument(lexer);
    }

    /// <summary>
    /// Parses a KDL document from a file.
    /// </summary>
    public KdlDocument ParseFile(string path)
    {
        var kdl = File.ReadAllText(path);
        return Parse(kdl);
    }

    /// <summary>
    /// Asynchronously parses a KDL document from a file.
    /// </summary>
    public async Task<KdlDocument> ParseFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var kdl = await File.ReadAllTextAsync(path, cancellationToken);
        return Parse(kdl);
    }

    /// <summary>
    /// Parses a KDL document from a stream.
    /// </summary>
    public KdlDocument ParseStream(Stream stream, bool leaveOpen = false)
    {
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096, leaveOpen);
        var kdl = reader.ReadToEnd();
        return Parse(kdl);
    }

    /// <summary>
    /// Asynchronously parses a KDL document from a stream.
    /// </summary>
    public async Task<KdlDocument> ParseStreamAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096, leaveOpen);
        var kdl = await reader.ReadToEndAsync();
        return Parse(kdl);
    }

    /// <summary>
    /// Parses top-level nodes from a file, yielding each node as it is parsed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Important</b>: This method reads the entire file into memory before parsing.
    /// The async enumerable interface allows processing nodes one at a time after loading,
    /// which can reduce memory pressure when processing results, but does not provide
    /// true incremental parsing from the file system.
    /// </para>
    /// <para>
    /// For typical configuration files (under 1MB), this is not a concern. For very large
    /// files, consider splitting them into smaller chunks or using a different approach.
    /// </para>
    /// </remarks>
    /// <param name="path">The path to the KDL file.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>An async enumerable of top-level nodes.</returns>
    public async IAsyncEnumerable<KdlNode> ParseNodesStreamAsync(string path, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Note: This reads the entire file into memory before parsing.
        // True incremental streaming would require a streaming-capable lexer.
        var kdl = await File.ReadAllTextAsync(path, cancellationToken);
        var lexer = new Lexer(kdl, settings);

        var token = lexer.NextToken();
        while (token.Type != TokenType.EndOfFile)
        {
            // Skip newlines and semicolons between nodes
            while (token.Type is TokenType.Newline or TokenType.Semicolon)
            {
                token = lexer.NextToken();
            }

            if (token.Type == TokenType.EndOfFile)
            {
                break;
            }

            var node = ParseNode(lexer, ref token, 0);
            yield return node;
        }
    }

    /// <summary>
    /// Parses top-level nodes from a stream, yielding each node as it is parsed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Important</b>: This method reads the entire stream into memory before parsing.
    /// The async enumerable interface allows processing nodes one at a time after loading,
    /// which can reduce memory pressure when processing results, but does not provide
    /// true incremental parsing from the stream.
    /// </para>
    /// <para>
    /// For typical configuration files (under 1MB), this is not a concern. For very large
    /// streams, consider splitting them into smaller chunks or using a different approach.
    /// </para>
    /// </remarks>
    /// <param name="stream">The stream containing KDL content.</param>
    /// <param name="leaveOpen">If true, leaves the stream open after reading.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>An async enumerable of top-level nodes.</returns>
    public async IAsyncEnumerable<KdlNode> ParseNodesStreamAsync(Stream stream, bool leaveOpen = false, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Note: This reads the entire stream into memory before parsing.
        // True incremental streaming would require a streaming-capable lexer.
        string kdl;
        using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096, leaveOpen))
        {
            kdl = await reader.ReadToEndAsync();
        }

        var lexer = new Lexer(kdl, settings);

        var token = lexer.NextToken();
        while (token.Type != TokenType.EndOfFile)
        {
            // Skip newlines and semicolons between nodes
            while (token.Type is TokenType.Newline or TokenType.Semicolon)
            {
                token = lexer.NextToken();
            }

            if (token.Type == TokenType.EndOfFile)
            {
                break;
            }

            var node = ParseNode(lexer, ref token, 0);
            yield return node;
        }
    }

    private KdlDocument ParseDocument(Lexer lexer)
    {
        KdlVersion detectedVersion = settings.TargetVersion;
        var token = lexer.NextToken();

        // Skip initial newlines and semicolons
        while (token.Type is TokenType.Newline or TokenType.Semicolon)
        {
            token = lexer.NextToken();
        }

        // Check for version marker: /- kdl-version N
        if (token.Type == TokenType.Slashdash)
        {
            var versionMarkerToken = lexer.NextToken();

            // Skip whitespace after slashdash
            while (versionMarkerToken.Type is TokenType.Newline or TokenType.Semicolon)
            {
                versionMarkerToken = lexer.NextToken();
            }

            if (versionMarkerToken.Type == TokenType.String && versionMarkerToken.Text == "kdl-version")
            {
                // This is a version marker
                var versionArgToken = lexer.NextToken();

                if (versionArgToken.Type == TokenType.Number)
                {
                    var versionNumber = (int)(decimal)versionArgToken.Value!;
                    KdlVersion markerVersion = versionNumber switch
                    {
                        1 => KdlVersion.V1,
                        2 => KdlVersion.V2,
                        _ => throw new KdlParseException($"Unknown KDL version: {versionNumber}. Supported versions are 1 and 2.", versionArgToken.Line, versionArgToken.Column)
                    };

                    // Validate version marker against TargetVersion setting
                    if (settings.TargetVersion != KdlVersion.Auto)
                    {
                        if (markerVersion != settings.TargetVersion)
                        {
                            throw new KdlParseException(
                                $"Parse error at line {versionArgToken.Line}: Document specifies '/- kdl-version {versionNumber}' but parser is configured for {settings.TargetVersion}.\n\n" +
                                $"Either:\n" +
                                $"1. Remove the explicit TargetVersion setting to respect the document marker\n" +
                                $"2. Change TargetVersion to {markerVersion} to match the document\n" +
                                $"3. Use TargetVersion = KdlVersion.Auto for automatic version detection",
                                versionArgToken.Line, versionArgToken.Column);
                        }
                    }

                    detectedVersion = markerVersion;

                    // Consume the rest of the version marker node (should just be newline/semicolon/EOF)
                    token = lexer.NextToken();
                    while (token.Type is TokenType.Newline or TokenType.Semicolon)
                    {
                        token = lexer.NextToken();
                    }
                }
                else
                {
                    throw new KdlParseException($"Version marker must have a numeric argument", versionArgToken.Line, versionArgToken.Column);
                }
            }
            else
            {
                // Not a version marker, parse as regular slashdashed node
                if (token.Type == TokenType.EndOfFile)
                {
                    throw new KdlParseException("Slashdash (/-) cannot appear before EOF", token.Line, token.Column);
                }
                SkipNode(lexer, ref token);
            }
        }

        var document = new KdlDocument(detectedVersion);

        while (token.Type != TokenType.EndOfFile)
        {
            // Skip newlines and semicolons between nodes
            while (token.Type is TokenType.Newline or TokenType.Semicolon)
            {
                token = lexer.NextToken();
            }

            if (token.Type == TokenType.EndOfFile)
            {
                break;
            }

            // Check for slashdash (commented-out node)
            if (token.Type == TokenType.Slashdash)
            {
                token = lexer.NextToken();

                // Skip any newlines/semicolons/whitespace before the node to be skipped
                while (token.Type is TokenType.Newline or TokenType.Semicolon)
                {
                    token = lexer.NextToken();
                }

                if (token.Type == TokenType.EndOfFile)
                {
                    throw new KdlParseException("Slashdash (/-) cannot appear before EOF", token.Line, token.Column);
                }

                // Skip the entire node
                SkipNode(lexer, ref token);
                continue;
            }

            var node = ParseNode(lexer, ref token, 0);
            document.Nodes.Add(node);
        }

        return document;
    }

    private KdlNode ParseNode(Lexer lexer, ref Token token, int depth)
    {
        if (depth > settings.MaxNestingDepth)
        {
            throw new KdlParseException($"Maximum nesting depth ({settings.MaxNestingDepth}) exceeded", token.Line, token.Column);
        }

        // Parse optional type annotation
        KdlAnnotation? typeAnnotation = null;
        if (token.Type == TokenType.OpenParen)
        {
            token = lexer.NextToken();
            if (token.Type != TokenType.String)
            {
                throw new KdlParseException("Expected type name in annotation", token.Line, token.Column);
            }
            typeAnnotation = new KdlAnnotation(token.Text!);
            token = lexer.NextToken();
            if (token.Type != TokenType.CloseParen)
            {
                throw new KdlParseException("Expected ')' after type annotation", token.Line, token.Column);
            }
            token = lexer.NextToken();
        }

        // Parse node name
        if (token.Type != TokenType.String)
        {
            throw new KdlParseException($"Expected node name, got {token.Type}", token.Line, token.Column);
        }

        var nodeName = token.Text!;
        var node = new KdlNode(nodeName)
        {
            TypeAnnotation = typeAnnotation,
            SourcePosition = new SourcePosition(token.Line, token.Column, token.Offset)
        };

        token = lexer.NextToken();

        // Parse arguments and properties
        bool inSlashdashChildren = false; // Track if we're entering slashdashed children handling
        while (token.Type is not (TokenType.OpenBrace or TokenType.Newline or TokenType.Semicolon or TokenType.EndOfFile or TokenType.CloseBrace))
        {
            // Check for slashdash (commented-out argument/property)
            if (token.Type == TokenType.Slashdash)
            {
                token = lexer.NextToken();

                // Skip any newlines after slashdash (slashdash applies to next value, not newlines)
                while (token.Type is TokenType.Newline or TokenType.Semicolon)
                {
                    token = lexer.NextToken();
                }

                // Validate that there's something to skip
                if (token.Type is TokenType.EndOfFile or TokenType.Semicolon or TokenType.CloseBrace or TokenType.Equals)
                {
                    throw new KdlParseException("Slashdash (/-) must be followed by a value or node", token.Line, token.Column);
                }

                // If it's a children block, set flag and exit the argument loop
                if (token.Type == TokenType.OpenBrace)
                {
                    inSlashdashChildren = true;
                    break;
                }

                // Now skip the actual value or property
                if (token.Type == TokenType.String)
                {
                    var nextToken = lexer.NextToken();
                    if (nextToken.Type == TokenType.Equals)
                    {
                        // It's a property, skip it
                        token = lexer.NextToken();
                        SkipValue(lexer, ref token);
                    }
                    else
                    {
                        // It's an argument (string value), skip it
                        token = nextToken;
                    }
                }
                else
                {
                    // It's a non-string value, skip it
                    SkipValue(lexer, ref token);
                }
                continue;
            }

            // Check for property (key=value)
            // We need to peek ahead to see if this is a property or argument
            if (token.Type == TokenType.String)
            {
                var possibleKey = token.Text!;
                var nextToken = lexer.NextToken();

                if (nextToken.Type == TokenType.Equals)
                {
                    // It's a property
                    token = lexer.NextToken(); // move past =
                    var value = ParseValue(lexer, ref token);

                    // Check if duplicates are allowed
                    if (!settings.AllowDuplicateProperties && node.HasProperty(possibleKey))
                    {
                        throw new KdlParseException($"Duplicate property key '{possibleKey}' is not allowed", token.Line, token.Column);
                    }

                    // In KDL v2, duplicate properties are allowed with rightmost taking precedence
                    // Always use AddProperty to preserve all duplicates in the document model.
                    // GetProperty will return the rightmost value per the spec.
                    node.AddProperty(possibleKey, value);
                }
                else
                {
                    // It's an argument (string value)
                    // We already consumed the string token, so create the value directly
                    var value = new KdlString(possibleKey, token.StringType, token.RawHashCount, token.IsRawMultiLine, token.RawMultiLineIndent);
                    value.SourcePosition = new SourcePosition(token.Line, token.Column, token.Offset);
                    node.Arguments.Add(value);
                    token = nextToken; // continue with the next token
                }
            }
            else
            {
                // It's an argument (non-string value)
                var value = ParseValue(lexer, ref token);
                node.Arguments.Add(value);
            }
        }

        // Check for slashdashed and regular children blocks
        // There can be multiple slashdashed blocks followed by one real block
        // If we already detected a slashdashed children block in the argument loop, handle it
        if (inSlashdashChildren && token.Type == TokenType.OpenBrace)
        {
            // We're at the opening brace of a slashdashed children block
            // Skip it
            token = lexer.NextToken(); // move past {
            var braceDepth = 1;
            while (braceDepth > 0 && token.Type != TokenType.EndOfFile)
            {
                if (token.Type == TokenType.OpenBrace)
                {
                    braceDepth++;
                }
                else if (token.Type == TokenType.CloseBrace)
                {
                    braceDepth--;
                }
                token = lexer.NextToken();
            }
            // After slashdashed children block, check for invalid entries
            if (token.Type is TokenType.String or TokenType.Number or TokenType.True or TokenType.False or
                TokenType.Null or TokenType.Infinity or TokenType.NaN or TokenType.OpenParen)
            {
                throw new KdlParseException("Cannot have entries (arguments or properties) after children blocks", token.Line, token.Column);
            }
        }

        bool hadSlashdashChildren = false;
        while (token.Type == TokenType.Slashdash)
        {
            token = lexer.NextToken();

            // Skip newlines/semicolons after slashdash
            while (token.Type is TokenType.Newline or TokenType.Semicolon)
            {
                token = lexer.NextToken();
            }

            if (token.Type == TokenType.OpenBrace)
            {
                hadSlashdashChildren = true;

                // Skip the entire children block
                token = lexer.NextToken(); // move past {
                var braceDepth = 1;
                while (braceDepth > 0 && token.Type != TokenType.EndOfFile)
                {
                    if (token.Type == TokenType.OpenBrace)
                    {
                        braceDepth++;
                    }
                    else if (token.Type == TokenType.CloseBrace)
                    {
                        braceDepth--;
                    }
                    token = lexer.NextToken();
                }
                // token is now the first token after the children block
                // DON'T skip newlines here - we need to check if there's a terminator
                // If the next token is not a terminator, it's an error
                if (token.Type is TokenType.String or TokenType.Number or TokenType.True or TokenType.False or
                    TokenType.Null or TokenType.Infinity or TokenType.NaN or TokenType.OpenParen)
                {
                    throw new KdlParseException("Cannot have entries (arguments or properties) after children blocks", token.Line, token.Column);
                }
                // If it's a newline or semicolon, skip them to check for more slashdash blocks
                while (token.Type is TokenType.Newline or TokenType.Semicolon)
                {
                    token = lexer.NextToken();
                }
                // Continue to check for more slashdashed blocks
            }
            else
            {
                // It wasn't a children block - this is an error
                throw new KdlParseException("Slashdash (/-) in this position must be followed by a children block", token.Line, token.Column);
            }
        }

        // After any slashdashed children blocks, if there are more entries before a real children block, that's an error
        if (hadSlashdashChildren && (token.Type is TokenType.String or TokenType.Number or TokenType.True or TokenType.False or
            TokenType.Null or TokenType.Infinity or TokenType.NaN or TokenType.OpenParen))
        {
            throw new KdlParseException("Cannot have entries (arguments or properties) after children blocks", token.Line, token.Column);
        }

        // Parse optional (non-slashdashed) children block
        if (token.Type == TokenType.OpenBrace)
        {
            token = lexer.NextToken();

            while (token.Type != TokenType.CloseBrace)
            {
                // Skip newlines and semicolons
                while (token.Type is TokenType.Newline or TokenType.Semicolon)
                {
                    token = lexer.NextToken();
                }

                if (token.Type == TokenType.CloseBrace)
                {
                    break;
                }

                if (token.Type == TokenType.EndOfFile)
                {
                    throw new KdlParseException("Unexpected end of file in children block", token.Line, token.Column);
                }

                // Check for slashdash
                if (token.Type == TokenType.Slashdash)
                {
                    token = lexer.NextToken();

                    // Skip any newlines/semicolons before the node to be skipped
                    while (token.Type is TokenType.Newline or TokenType.Semicolon)
                    {
                        token = lexer.NextToken();
                    }

                    if (token.Type == TokenType.CloseBrace)
                    {
                        throw new KdlParseException("Slashdash (/-) cannot appear before closing brace", token.Line, token.Column);
                    }

                    if (token.Type == TokenType.EndOfFile)
                    {
                        throw new KdlParseException("Slashdash (/-) cannot appear before EOF", token.Line, token.Column);
                    }

                    SkipNode(lexer, ref token);
                    continue;
                }

                var child = ParseNode(lexer, ref token, depth + 1);
                node.AddChild(child);
            }

            token = lexer.NextToken(); // consume }

            // After a children block, validate that the node is properly terminated
            // Valid: newline, semicolon, EOF, CloseBrace (in parent's children), or Slashdash (for more slashdashed blocks)
            // Invalid: values or property keys (String, Number, etc.) without separator
            if (token.Type is TokenType.String or TokenType.Number or TokenType.True or TokenType.False or
                TokenType.Null or TokenType.Infinity or TokenType.NaN or TokenType.OpenParen)
            {
                throw new KdlParseException(
                    $"After a children block, expected newline, semicolon, or end of input before starting a new node, but got {token.Type}. " +
                    "Nodes must be properly separated.",
                    token.Line, token.Column);
            }
        }

        return node;
    }

    private KdlValue ParseValue(Lexer lexer, ref Token token)
    {
        // Parse optional type annotation
        KdlAnnotation? typeAnnotation = null;
        if (token.Type == TokenType.OpenParen)
        {
            token = lexer.NextToken();
            if (token.Type != TokenType.String)
            {
                throw new KdlParseException("Expected type name in annotation", token.Line, token.Column);
            }
            typeAnnotation = new KdlAnnotation(token.Text!);
            token = lexer.NextToken();
            if (token.Type != TokenType.CloseParen)
            {
                throw new KdlParseException("Expected ')' after type annotation", token.Line, token.Column);
            }
            token = lexer.NextToken();
        }

        KdlValue value = token.Type switch
        {
            TokenType.String => new KdlString(token.Text!, token.StringType, token.RawHashCount, token.IsRawMultiLine, token.RawMultiLineIndent),
            TokenType.Number => ParseNumberValue(token),
            TokenType.True => KdlBoolean.True,
            TokenType.False => KdlBoolean.False,
            TokenType.Null => KdlNull.Instance,
            TokenType.Infinity => (double)token.Value! > 0 ? KdlNumber.PositiveInfinity() : KdlNumber.NegativeInfinity(),
            TokenType.NaN => KdlNumber.NaN(),
            _ => throw new KdlParseException($"Expected value, got {token.Type}", token.Line, token.Column)
        };

        // Handle singletons carefully to avoid polluting shared state
        var isSingleton = value == KdlBoolean.True || value == KdlBoolean.False || value == KdlNull.Instance;
        if (isSingleton)
        {
            if (typeAnnotation != null)
            {
                // Must create a new instance to hold the annotation
                value = token.Type switch
                {
                    TokenType.True => new KdlBoolean(true),
                    TokenType.False => new KdlBoolean(false),
                    TokenType.Null => new KdlNull(),
                    _ => value
                };
                value.TypeAnnotation = typeAnnotation;
                value.SourcePosition = new SourcePosition(token.Line, token.Column, token.Offset);
            }
            // For singletons without annotations, don't set any properties - use them as-is
        }
        else
        {
            // Non-singletons: safe to set properties
            value.TypeAnnotation = typeAnnotation;
            value.SourcePosition = new SourcePosition(token.Line, token.Column, token.Offset);
        }

        token = lexer.NextToken();
        return value;
    }


    private KdlNumber ParseNumberValue(Token token)
    {
        // Check if it's a special format based on the text
        if (token.Value is string rawText)
        {
            if (rawText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return new KdlNumber(rawText, KdlNumberFormat.Hexadecimal);
            }
            else if (rawText.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            {
                return new KdlNumber(rawText, KdlNumberFormat.Octal);
            }
            else if (rawText.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            {
                return new KdlNumber(rawText, KdlNumberFormat.Binary);
            }
        }

        // Regular decimal number
        return new KdlNumber((decimal)token.Value!);
    }

    private void SkipValue(Lexer lexer, ref Token token)
    {
        // Parse optional type annotation
        if (token.Type == TokenType.OpenParen)
        {
            token = lexer.NextToken();
            token = lexer.NextToken();
            if (token.Type == TokenType.CloseParen)
            {
                token = lexer.NextToken();
            }
        }

        // Skip the value itself
        token = lexer.NextToken();
    }

    private void SkipNode(Lexer lexer, ref Token token)
    {
        // Skip optional type annotation
        if (token.Type == TokenType.OpenParen)
        {
            token = lexer.NextToken();
            token = lexer.NextToken();
            if (token.Type == TokenType.CloseParen)
            {
                token = lexer.NextToken();
            }
        }

        // Skip node name
        if (token.Type == TokenType.String)
        {
            token = lexer.NextToken();
        }

        // Skip arguments and properties
        while (token.Type is not (TokenType.OpenBrace or TokenType.Newline or TokenType.Semicolon or TokenType.EndOfFile or TokenType.CloseBrace))
        {
            token = lexer.NextToken();
        }

        // Skip children block
        if (token.Type == TokenType.OpenBrace)
        {
            var depth = 1;
            token = lexer.NextToken();
            while (depth > 0 && token.Type != TokenType.EndOfFile)
            {
                if (token.Type == TokenType.OpenBrace)
                {
                    depth++;
                }
                else if (token.Type == TokenType.CloseBrace)
                {
                    depth--;
                }
                token = lexer.NextToken();
            }
        }
    }
}

