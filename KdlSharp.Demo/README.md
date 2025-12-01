# KdlSharp Demo

This directory contains example applications showcasing the KdlSharp library's features.

## Running the Demo

```bash
cd KdlSharp.Demo
dotnet run
```

The demo application will run through all examples, demonstrating:

- Basic parsing and document navigation
- POCO serialization and deserialization
- Error handling patterns
- Advanced document construction
- Schema validation
- Query language usage
- Streaming operations
- File I/O operations
- Extension methods for document navigation

## For Developers

If you're interested in contributing to KdlSharp or understanding how the library works internally, see **[AGENTS.md](../AGENTS.md)** for comprehensive development documentation including:

- Project structure and architecture
- Build and test commands
- Code quality standards and best practices
- Development workflow and contribution guidelines
- Command-line reference for common tasks

## Demo Features

### 1. Basic Parsing
- Parsing KDL documents from strings
- Accessing nodes, arguments, and properties
- Working with nested child nodes
- Using `TryParse` for safe parsing

### 2. Serialization (POCO)
- Serializing .NET objects to KDL format
- Deserializing KDL back to strongly-typed objects
- Using attributes and naming policies
- Idiomatic KDL output with proper formatting

### 3. Error Handling
- Handling parse exceptions with detailed error messages
- Line and column information for syntax errors
- Source context for debugging
- Safe parsing with `TryParse`

### 4. Advanced Usage
- Creating KDL documents programmatically
- Using the fluent API for node construction
- Working with type annotations
- Extension methods for convenient access
- Document traversal and manipulation

### 5. Schema Validation
- Defining schemas programmatically
- Validating documents against schemas
- Handling validation errors with detailed messages
- Required properties and value constraints
- String patterns and number ranges

### 6. Query Language
- Using CSS-selector-like queries to find nodes
- Selectors: child (`>`), descendant (`>>`), sibling (`+`, `++`)
- Matchers: name, value, property, tag matching
- Operators: equality, comparison, string matching
- Compiling queries for reuse

### 7. Async Operations
- Asynchronous file reading and writing
- Stream-based async operations
- Cancellation token support
- Non-blocking I/O patterns

### 8. File Operations
- Synchronous file read/write
- Error handling for file I/O
- Custom formatter settings
- Different indentation styles

### 9. Extension Methods
- LINQ-like document navigation
- Typed property access
- Tree traversal (descendants, ancestors)
- Convenient node searching

## Examples Directory

Each example is in a separate file in the `Examples/` directory:

### BasicParsing.cs
Fundamental parsing operations for getting started with KdlSharp.
- **What it demonstrates**: Parsing KDL from strings, accessing nodes/arguments/properties, navigating child nodes
- **Learning objectives**: Understand the KdlDocument, KdlNode, and KdlValue types; learn to traverse document structure

### Serialization.cs
POCO serialization and deserialization for mapping .NET objects to KDL.
- **What it demonstrates**: Serializing records to KDL, deserializing back to objects, using naming policies (kebab-case)
- **Learning objectives**: Use KdlSerializer with options; create decoupled POCOs that work with KDL

### ErrorHandling.cs
Error handling patterns and parsing failure scenarios.
- **What it demonstrates**: Catching KdlParseException with line/column info, TryParse pattern, version conflict errors
- **Learning objectives**: Handle parse errors gracefully; use TryParse for safe parsing; understand v1/v2 syntax differences

### AdvancedUsage.cs
Document construction, extension methods, and formatting customization.
- **What it demonstrates**: Fluent API for building nodes, extension methods for property access, version-aware parsing, custom formatting
- **Learning objectives**: Build KDL documents programmatically; configure parser/formatter settings; work with both KDL v1 and v2

### SchemaValidation.cs
Schema definition and validation for enforcing document structure.
- **What it demonstrates**: Permissive schemas, required properties, validation rules (length, pattern), error reporting
- **Learning objectives**: Define schemas with SchemaDocument/SchemaNode; apply validation rules; interpret validation errors

### Queries.cs
CSS-selector-like query language for finding nodes.
- **What it demonstrates**: Name matching, property filters, child (`>`) and descendant (`>>`) selectors, compiled queries
- **Learning objectives**: Write KDL queries; use matchers and combinators; compile queries for reuse

### StreamingReader.cs
Low-level streaming API for processing large documents efficiently.
- **What it demonstrates**: Token-by-token reading with KdlReader, processing large files without loading entire document, custom deserialization
- **Learning objectives**: Use KdlReader for memory-efficient parsing; process tokens manually; understand when streaming is beneficial

### AsyncOperations.cs
Asynchronous file and stream operations for non-blocking I/O.
- **What it demonstrates**: Async file reading/writing with ParseFileAsync/SaveAsync, stream-based async operations, cancellation token usage
- **Learning objectives**: Use async APIs for file operations; work with streams asynchronously; implement cancellation support

### FileOperations.cs
Synchronous file I/O patterns and formatter customization.
- **What it demonstrates**: Basic file read/write, error handling for file operations, custom formatter settings (indentation styles)
- **Learning objectives**: Save and load KDL files; handle file errors gracefully; customize output formatting

### ExtensionMethods.cs
LINQ-like extension methods for convenient document navigation.
- **What it demonstrates**: FindNode/FindNodes for searching, Descendants/Ancestors for tree traversal, GetPropertyValue<T> for typed access, HasProperty checks
- **Learning objectives**: Navigate documents using extension methods; use typed property access; traverse node hierarchies efficiently

## More Examples

For more detailed examples and API usage, see:
- The main [README.md](../README.md) in the project root
- Unit tests in `KdlSharp.Tests/` which demonstrate all features comprehensively
- [KDL specification](../specs/SPEC.md) for understanding KDL syntax

## Interactive Mode

The demo runs automatically through all examples. To explore specific features interactively, modify `Program.cs` to call only the examples you're interested in:

```csharp
// Run only specific examples
BasicParsing.Run();
Serialization.Run();
```

## Contributing Examples

If you have ideas for additional demo examples, please:
1. Add a new example file in `Examples/`
2. Follow the existing pattern with a static `Run()` method
3. Include comments explaining what's being demonstrated
4. Update this README to list the new example

For complete development setup and contribution workflows, see the **[For Developers](#for-developers)** section above.

