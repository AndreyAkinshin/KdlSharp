# KdlSharp

![](https://raw.githubusercontent.com/AndreyAkinshin/KdlSharp/refs/heads/assets/cover.png)

[![NuGet](https://img.shields.io/nuget/v/KdlSharp.svg)](https://www.nuget.org/packages/KdlSharp)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A feature-rich C# library for [KDL (KDL Document Language)](https://kdl.dev/) with strong v2.0 specification support.

## Features

- **POCO Serialization**: Map .NET objects to KDL and back with `KdlSerializer`
- **KDL v2.0 Support**: All string types, number formats, and modern syntax
- **Document Model API**: Parse and manipulate KDL documents programmatically
- **Query Language**: CSS-selector-like queries for finding nodes
- **Schema Validation**: Validate documents against schemas with full `ref` resolution support
- **Special Numbers**: Full support for `#inf`, `#-inf`, and `#nan` with proper round-trip semantics
- **Zero Dependencies**: No external dependencies in core library
- **Cross-Platform**: .NET Standard 2.1 compatible

## Installation

```bash
dotnet add package KdlSharp
```

## Examples

### 1. Parse a KDL Document

```csharp
using KdlSharp;

var doc = KdlDocument.Parse(@"
    server host=""localhost"" port=8080 {
        database connection=""postgres://localhost/mydb""
    }
");

var server = doc.Nodes[0];
Console.WriteLine(server.GetProperty("host")?.AsString()); // "localhost"
Console.WriteLine(server.GetProperty("port")?.AsInt32());  // 8080
```

### 2. Serialize Objects to KDL

```csharp
using KdlSharp.Serialization;

public record ServerConfig(string Host, int Port, bool UseSsl);

var config = new ServerConfig("localhost", 8080, true);
var serializer = new KdlSerializer(new KdlSerializerOptions
{
    RootNodeName = "server",
    PropertyNamingPolicy = KdlNamingPolicy.KebabCase
});

string kdl = serializer.Serialize(config);
// server host="localhost" port=8080 use-ssl=#true
```

### 3. Deserialize KDL to Objects

```csharp
var kdl = @"server host=""localhost"" port=8080 use-ssl=#true";
var config = serializer.Deserialize<ServerConfig>(kdl);
```

> **Note**: Circular references are not supported. Serializing an object graph containing cycles (e.g., A → B → A) throws `KdlSerializationException`. To serialize graphs with cycles, break them first using reference IDs or other patterns.

### 4. Build Documents Programmatically

```csharp
var doc = new KdlDocument();
doc.Nodes.Add(
    new KdlNode("package")
        .AddArgument("my-app")
        .AddProperty("version", "1.0.0")
        .AddChild(new KdlNode("author").AddArgument("Alice"))
);

string kdl = doc.ToKdlString();
```

### 5. Query Nodes

```csharp
using KdlSharp.Query;

var doc = KdlDocument.Parse(@"
    package {
        dependencies { lodash ""^4.0"" }
        dev-dependencies { jest ""^29.0"" }
    }
");

// Find all dependency nodes
var deps = KdlQuery.Execute(doc, "package >> dependencies");
```

### 6. Validate Against Schema

```csharp
using KdlSharp.Schema;
using KdlSharp.Schema.Rules;

var schema = new SchemaDocument(
    new SchemaInfo { Title = "Config Schema" },
    nodes: new[] {
        new SchemaNode("server", properties: new[] {
            new SchemaProperty("port", required: true, validationRules: new[] {
                new GreaterThanRule(0), new LessThanRule(65536)
            })
        })
    }
);

var result = KdlSchema.Validate(doc, schema);
if (!result.IsValid)
    foreach (var error in result.Errors)
        Console.WriteLine($"{error.Path}: {error.Message}");
```

### 7. Parse Legacy v1 Documents

```csharp
var settings = new KdlParserSettings { TargetVersion = KdlVersion.V1 };
var doc = KdlDocument.Parse("node true false null", settings);
```

### 8. Serializer Options

```csharp
var options = new KdlSerializerOptions
{
    // Custom root node name (default: "root")
    RootNodeName = "config",

    // Naming policy: CamelCase, PascalCase, SnakeCase, KebabCase, None (default: KebabCase)
    PropertyNamingPolicy = KdlNamingPolicy.KebabCase,

    // Include null values in output (default: false)
    IncludeNullValues = true,

    // Add type annotations like (i32), (string), (bool) (default: false)
    WriteTypeAnnotations = true,

    // Write simple values as arguments vs properties (default: true)
    UseArgumentsForSimpleValues = true,

    // Flatten single-child objects into parent node (default: false)
    FlattenSingleChildObjects = false,

    // Target KDL version: V1 uses bare true/false/null, V2 uses #true/#false/#null (default: V2)
    TargetVersion = KdlVersion.V2
};
```

### 9. Version Markers

When formatting KDL documents, you can include a version marker at the start of the output:

```csharp
using KdlSharp.Formatting;
using KdlSharp.Settings;

var settings = new KdlFormatterSettings
{
    IncludeVersionMarker = true,  // Emit /- kdl-version N at start
    TargetVersion = KdlVersion.V2
};
var formatter = new KdlFormatter(settings);
var kdl = formatter.Serialize(doc);
// Output: /- kdl-version 2
//         node #true ...
```

### 10. Preserving String Formatting

By default, all strings are serialized as quoted strings. To preserve the original string format (identifier, quoted, raw, or multi-line) from parsing:

```csharp
using KdlSharp.Formatting;
using KdlSharp.Settings;

// Parse a document with various string types
var doc = KdlDocument.Parse("node identifier-value #\"raw string\"# \"quoted\"");

// Enable PreserveStringTypes to maintain original formatting
var settings = new KdlFormatterSettings { PreserveStringTypes = true };
var formatter = new KdlFormatter(settings);
var kdl = formatter.Serialize(doc);
// Output: node identifier-value #"raw string"# "quoted"
```

Raw strings preserve their original hash count delimiter during round-tripping:

```csharp
// Parse a raw string with multiple hashes
var doc = KdlDocument.Parse("node ##\"contains \"# pattern\"##");

// The hash count is preserved when serializing with PreserveStringTypes
var settings = new KdlFormatterSettings { PreserveStringTypes = true };
var formatter = new KdlFormatter(settings);
var kdl = formatter.Serialize(doc);
// Output: node ##"contains "# pattern"##
```

> **Note**: When constructing `KdlString` values programmatically (not from parsing), they default to `KdlStringType.Quoted`. Specify the string type in the constructor to use other formats: `new KdlString("value", KdlStringType.Identifier)`. For raw strings, use the static helper method for convenience:

```csharp
// Create a simple raw string (hash count computed automatically)
var raw = KdlString.Raw(@"C:\path\to\file");

// Create a raw string with explicit hash count
var rawWithHashes = KdlString.Raw("contains \"# pattern", hashCount: 2);

// Create a multi-line raw string
var rawMultiLine = KdlString.Raw("line1\nline2", multiLine: true);
```

## Streaming API

### Reading from Streams

Parse documents directly from streams with the `leaveOpen` parameter to control stream lifetime:

```csharp
using var fileStream = File.OpenRead("config.kdl");

// Parse from stream, closing it after parsing (default)
var doc = KdlDocument.ParseStream(fileStream);

// Or keep the stream open for further operations
var doc = KdlDocument.ParseStream(fileStream, leaveOpen: true);
```

Async versions are available for all stream operations:

```csharp
var doc = await KdlDocument.ParseStreamAsync(fileStream, cancellationToken: token);
await doc.WriteToAsync(outputStream, leaveOpen: true);
```

### Token-by-Token Reading

For scenarios requiring token-level control over parsing, use `KdlReader`:

```csharp
using KdlSharp.Parsing;

using var reader = new StringReader(kdlText);
using var kdlReader = new KdlReader(reader);

while (kdlReader.Read())
{
    Console.WriteLine($"[{kdlReader.Line}:{kdlReader.Column}] {kdlReader.TokenType}");
}
```

### Async Streaming Serialization

For memory-efficient serialization of large sequences, use `SerializeStreamAsync`:

```csharp
// Serialize an async sequence directly to a stream
await serializer.SerializeStreamAsync(GetEventsAsync(), outputStream);

async IAsyncEnumerable<Event> GetEventsAsync()
{
    // Objects are serialized one at a time as they are yielded
    await foreach (var evt in eventSource.ReadAsync())
        yield return evt;
}
```

The streaming serializer supports cancellation tokens and respects serializer options like `TargetVersion`.

> **Note**: All parsing methods (including `ParseStream`, `ParseStreamAsync`, and `KdlReader`) read the entire input into memory before tokenizing. The "streaming" APIs provide convenient stream-based interfaces but do not perform incremental parsing. For typical configuration files (under 1MB), this is not a concern. See [AsyncOperations.cs](KdlSharp.Demo/Examples/AsyncOperations.cs) for detailed examples.

## Requirements

- .NET Standard 2.1 or higher
- Compatible with .NET 6+, .NET Framework 4.7.2+, Mono, Xamarin

## More Resources

- [KdlSharp.Demo](KdlSharp.Demo) - Working examples of all features
- [KDL Website](https://kdl.dev/) - Official KDL documentation
- [AGENTS.md](AGENTS.md) - Development guide and contributing

## License

MIT License - see [LICENSE](LICENSE) for details.
