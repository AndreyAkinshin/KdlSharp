# Agents Instructions

This document provides essential context for LLM agents working on the KdlSharp project. It serves as an entry point for understanding the codebase, project structure, and development workflow.

## Project Overview

**KdlSharp** is a feature-rich C# library for parsing, manipulating, and serializing [KDL (KDL Document Language)](https://kdl.dev/) documents. KDL is a human-friendly, node-based document language similar to XML/HTML but with cleaner syntax, designed for configuration files and data serialization.

### Key Features
- **KDL v2.0 Support**: Full support for the current specification (with v1.0 legacy support)
- **POCO Serialization**: Maps .NET objects to/from KDL documents
- **Document Model API**: Programmatically parse and manipulate KDL documents
- **Query Language**: CSS-selector-like queries (based on KDL Query spec)
- **Schema Validation**: Basic validation against schemas (work in progress)
- **High Performance**: Efficient parsing with optimized lexer
- **Zero Dependencies**: Core library has no external dependencies
- **Cross-Platform**: .NET Standard 2.1 compatible

### Target Framework
- **Library (KdlSharp/)**: .NET Standard 2.1 (compatible with .NET 6+, Framework 4.7.2+)
- **Tests/Demo/Benchmarks**: .NET 9.0

## Project Structure

```
KdlSharp/
├── .github/                       # GitHub Actions workflows
│   └── workflows/                 # CI/CD pipeline definitions
│       ├── build.yaml             # Build and test automation
│       └── release.yaml           # Release and NuGet publish automation
│
├── KdlSharp/                      # Main library (NuGet package)
│   ├── Exceptions/                # Exception types (parse, validation, query, schema)
│   ├── Extensions/                # LINQ-like extension methods for querying
│   ├── Formatting/                # KDL formatting and writing
│   ├── Parsing/                   # Lexer, parser, and reader for KDL text
│   ├── Query/                     # KDL Query Language implementation
│   ├── Schema/                    # KDL Schema validation (basic support)
│   │   └── Rules/                 # Validation rules (string, number, generic, structural)
│   ├── Serialization/             # POCO serialization/deserialization
│   │   ├── Converters/            # Type converters for serialization
│   │   ├── Metadata/              # Attributes and metadata for mapping
│   │   └── Reflection/            # Reflection-based type mapping
│   ├── Settings/                  # Parser and formatter settings
│   ├── Utilities/                 # Number parsing, string escaping
│   ├── Values/                    # KDL value types (string, number, bool, null)
│   ├── KdlAnnotation.cs           # Type annotations for values
│   ├── KdlDocument.cs             # Root document type
│   ├── KdlNode.cs                 # Node with name, args, props, children
│   ├── KdlProperty.cs             # Key-value property
│   ├── KdlValue.cs                # Base value type
│   ├── KdlVersion.cs              # Version enumeration (V1, V2)
│   ├── SourcePosition.cs          # Line/column tracking for errors
│   └── ValidationResult.cs        # Schema validation result
│
├── KdlSharp.Tests/                # Unit tests (xUnit, .NET 9)
│   ├── OfficialTests/             # Official KDL specification tests
│   │   ├── manifest.json          # Test manifest from official repo
│   │   ├── OfficialTestRunner.cs  # Runner for official tests
│   │   └── README.md              # Documentation for official tests
│   ├── QueryTests/                # Query language tests
│   ├── SchemaTests/               # Schema validation tests
│   ├── SerializerTests/           # POCO serialization tests
│   ├── ReaderTests/               # Low-level reader tests
│   ├── BasicParsingTests.cs       # Core parsing functionality
│   └── VersioningTests.cs         # v1/v2 compatibility tests
│
├── KdlSharp.Demo/                 # Working examples (.NET 9)
│   ├── Examples/                  # Example files demonstrating features
│   ├── Program.cs                 # Demo runner
│   └── README.md                  # Demo documentation
│
├── KdlSharp.Benchmarks/           # BenchmarkDotNet performance tests (.NET 9)
│   └── Program.cs                 # Benchmark runner
│
├── specs/                         # Git submodule: https://github.com/kdl-org/kdl
│   ├── SPEC.md                    # KDL v2.0 specification
│   ├── SPEC_v1.md                 # KDL v1.0 specification (legacy)
│   ├── QUERY-SPEC.md              # Query language specification
│   ├── SCHEMA-SPEC.md             # Schema language specification
│   ├── tests/                     # Official compatibility test suite
│   └── examples/                  # Example KDL documents
│
├── .editorconfig                  # Code style rules
├── .gitignore                     # Git ignore patterns
├── .gitmodules                    # Submodule configuration
├── AGENTS.md                      # This file (development guide)
├── Directory.Build.props          # Shared MSBuild configuration
├── KdlSharp.sln                   # Solution file
├── LICENSE                        # MIT License
└── README.md                      # User-facing documentation
```

### Important Files

- **`README.md`**: User-facing project documentation. Contains brief overview, Quick Start Guide, comprehensive demos, and additional useful information for users (excluding internal development details). Should NOT cover the development process or repository internals.
- **`AGENTS.md`**: LLM-friendly development guide for contributors and automated agents (this file). Covers repository structure, command-line commands, best practices, and essential development workflows.
- **`.editorconfig`**: Code style rules and formatting configuration enforced across all projects.
- **`.gitignore`**: Git ignore patterns excluding build artifacts, IDE-specific files, and temporary files.
- **`.gitmodules`**: Submodule configuration referencing the official KDL specification repository as `specs/`.
- **`Directory.Build.props`**: Shared MSBuild properties and configuration applied to all projects in the solution.
- **`KdlSharp.sln`**: Solution file defining all projects (library, tests, demo, benchmarks).
- **`LICENSE`**: MIT License governing the project.
- **`.github/`**: Directory containing GitHub Actions workflows for build and release automation.

The root folder also contains the following directories:

- **`.github/`**: GitHub Actions workflows for continuous integration, testing, and release automation.
- **`KdlSharp/`**: Main library project containing the core KDL parsing, serialization, and manipulation logic.
- **`KdlSharp.Demo/`**: Demo/example project showcasing various library features and usage scenarios.
- **`KdlSharp.Tests/`**: Unit tests project with comprehensive test coverage including official KDL specification tests.
- **`KdlSharp.Benchmarks/`**: Performance benchmarks project for measuring and optimizing library performance.
- **`specs/`**: Git submodule referencing the official KDL specification repository (https://github.com/kdl-org/kdl).

## Command-Line Reference

### Building

```bash
# Build entire solution (Debug)
dotnet build KdlSharp.sln

# Build in Release mode
dotnet build KdlSharp.sln --configuration Release

# Build only the main library
dotnet build KdlSharp/KdlSharp.csproj --configuration Release

# Clean build artifacts
dotnet clean KdlSharp.sln
```

### Testing

```bash
# Run all tests
dotnet test KdlSharp.Tests/KdlSharp.Tests.csproj

# Run tests in Release mode
dotnet test KdlSharp.Tests/KdlSharp.Tests.csproj --configuration Release

# Run tests with detailed output
dotnet test KdlSharp.Tests/KdlSharp.Tests.csproj --verbosity normal

# Run specific test class
dotnet test KdlSharp.Tests/KdlSharp.Tests.csproj --filter "FullyQualifiedName~BasicParsingTests"

# Run tests with code coverage
dotnet test KdlSharp.Tests/KdlSharp.Tests.csproj --collect:"XPlat Code Coverage"

# Generate coverage report (requires reportgenerator tool)
# Install: dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### Running Demos

```bash
# Run demo project (shows usage examples)
dotnet run --project KdlSharp.Demo/KdlSharp.Demo.csproj

# Run in Release mode for better performance
dotnet run --project KdlSharp.Demo/KdlSharp.Demo.csproj --configuration Release
```

### Running Benchmarks

```bash
# Run all performance benchmarks (takes several minutes)
dotnet run --project KdlSharp.Benchmarks/KdlSharp.Benchmarks.csproj --configuration Release

# Run specific benchmark categories
dotnet run --project KdlSharp.Benchmarks/KdlSharp.Benchmarks.csproj --configuration Release -- --filter "*ParsingBenchmark*"
dotnet run --project KdlSharp.Benchmarks/KdlSharp.Benchmarks.csproj --configuration Release -- --filter "QueryBenchmarks.*"
dotnet run --project KdlSharp.Benchmarks/KdlSharp.Benchmarks.csproj --configuration Release -- --filter "SchemaBenchmarks.*"
```

### Code Style & Formatting

```bash
# Check code style compliance (does NOT modify files)
dotnet format KdlSharp.sln --verify-no-changes

# Auto-fix formatting issues
dotnet format KdlSharp.sln

# Check formatting for specific project
dotnet format KdlSharp/KdlSharp.csproj --verify-no-changes
```

### Packaging

```bash
# Create NuGet package
dotnet pack KdlSharp/KdlSharp.csproj --configuration Release

# Output location: KdlSharp/bin/Release/KdlSharp.*.nupkg
```

### Restoring Dependencies

```bash
# Restore NuGet packages for solution
dotnet restore KdlSharp.sln

# Clean NuGet cache (if having dependency issues)
dotnet nuget locals all --clear
dotnet restore KdlSharp.sln
```

### Submodule Management

The official KDL specification is included as a Git submodule at `specs/` (https://github.com/kdl-org/kdl).

```bash
# Initialize submodule (first time setup)
git submodule update --init --recursive

# Update submodule to latest
git submodule update --remote --merge

# Check submodule status
git submodule status
```

## Development Workflow

### Code Quality Standards

1. **Warnings as Errors**: The project treats all warnings as errors (`TreatWarningsAsErrors=true`)
2. **Code Style Enforcement**: Style rules are enforced during build (`EnforceCodeStyleInBuild=true`)
3. **.NET Analyzers**: All analyzers are enabled at latest level
4. **XML Documentation**: All public APIs must have XML doc comments

### KdlSharp-Specific Principles

**Zero External Dependencies (Strict)**:
- The core `KdlSharp` library must have NO NuGet dependencies
- Use only .NET Standard 2.1 built-in APIs
- This ensures maximum portability and minimal maintenance
- Tests/demos/benchmarks may have dependencies (xUnit, BenchmarkDotNet, etc.)

**KDL Specification Compliance**:
- All parsing behavior must match the official KDL v2.0 specification exactly
- Support KDL v1.0 as legacy opt-in mode
- Pass all official KDL test cases (currently 460+ tests passing)
- When in doubt, specification wording is authoritative

**C#/.NET Conventions**:
- Use PascalCase for types/methods/properties
- Use camelCase for parameters and local variables
- Follow standard .NET async patterns (async/await, cancellation tokens)
- Use XML documentation comments on all public APIs
- Target .NET Standard 2.1 for core library (ensures .NET Framework 4.7.2+ and .NET 6+ compatibility)

**Test Organization**:
- **460+ passing tests** covering specification compliance and additional scenarios
- **Official KDL tests**: Full integration with official compatibility test suite via `specs/tests/`
- Test categories: Core parsing, POCO serialization, Query language, Schema validation, Error handling, v1/v2 version compatibility

## Key Concepts

### KDL Basics

KDL is a node-based document language with the following structure:

```kdl
// Node with arguments and properties
package "my-app" version="1.0.0" {
    // Child nodes
    author "Alice"
    dependencies {
        lodash "^4.17.0"
    }
}
```

**Components**:
- **Node**: Has a name, arguments (values), properties (key-value pairs), and optional children
- **Arguments**: Ordered values attached to a node
- **Properties**: Named key-value pairs
- **Children**: Nested nodes inside `{ }`

### KDL v2.0 vs v1.0

**KDL v2.0** (current, default):
- Prefixed keywords: `#true`, `#false`, `#null`, `#inf`, `#-inf`, `#nan`
- Multi-line strings: `"""text"""`
- Underscores in numbers: `1_000_000`
- Quoted node names: `"my node"`
- Raw strings: `#"C:\path"#`

**KDL v1.0** (legacy, opt-in):
- Bare keywords: `true`, `false`, `null`
- Explicit opt-in via `KdlParserSettings { TargetVersion = KdlVersion.V1 }`

### Core Types in KdlSharp

- **`KdlDocument`**: Root container with top-level nodes
- **`KdlNode`**: Node with name, arguments, properties, children
- **`KdlProperty`**: Key-value pair
- **`KdlValue`**: Base class for all values
  - `KdlString`: String value (4 types: identifier, quoted, multi-line, raw)
  - `KdlNumber`: Number value (5 formats: decimal, hex, octal, binary, special)
  - `KdlBoolean`: Boolean value
  - `KdlNull`: Null value

### Parsing API

```csharp
// From string
var doc = KdlDocument.Parse(kdlText);

// From file
var doc = KdlDocument.ParseFile("config.kdl");

// Try parse (no exceptions)
if (KdlDocument.TryParse(kdlText, out var doc, out var error))
{
    // Success
}
else
{
    // error contains detailed parse exception
}
```

### Serialization API

```csharp
using KdlSharp.Serialization;

// POCO to KDL
var serializer = new KdlSerializer(new KdlSerializerOptions
{
    RootNodeName = "config",
    PropertyNamingPolicy = KdlNamingPolicy.KebabCase
});

string kdl = serializer.Serialize(myObject);

// KDL to POCO
var obj = serializer.Deserialize<MyType>(kdl);
```

## References

### Specifications

All specifications are in the `specs/` submodule:

- **KDL v2.0 Spec**: `specs/SPEC.md` (current)
- **KDL v1.0 Spec**: `specs/SPEC_v1.md` (legacy)
- **Query Language Spec**: `specs/QUERY-SPEC.md`
- **Schema Language Spec**: `specs/SCHEMA-SPEC.md`
- **Official Tests**: `specs/tests/` (compatibility test suite)

### Online Resources

- **KDL Official Site**: https://kdl.dev/
- **KDL Playground**: https://kdl.dev/play/
- **KDL Specification (RFC)**: https://kdl-org.github.io/kdl/
- **GitHub Repository**: https://github.com/kdl-org/kdl

### Project Documentation

- **README.md**: User documentation, API reference, examples
- **AGENTS.md**: Development guide for LLM agents and contributors
- **KdlSharp.Demo/README.md**: Demo project documentation

## Troubleshooting

### Build Issues

**"Warnings treated as errors"**:
- Check build output for specific warnings
- Fix warnings or update `.editorconfig` if needed
- Never suppress warnings without good reason

**"NuGet package restore failed"**:
```bash
dotnet nuget locals all --clear
dotnet restore KdlSharp.sln
```

### Test Issues

**"Official tests failing"**:
- Ensure submodule is initialized: `git submodule update --init --recursive`
- Verify `specs/tests/` directory exists and contains test files

**"Tests timeout"**:
- Run with `--blame-hang-timeout 5min` to identify hanging tests

### Formatting Issues

**"Format verification failed"**:
```bash
# Auto-fix all formatting issues
dotnet format KdlSharp.sln

# Check what would be changed without applying
dotnet format KdlSharp.sln --verify-no-changes --verbosity diagnostic
```

## Quick Start Checklist

For a fresh agent session, verify:

1. ✅ **Read `README.md`, `AGENTS.md` first**: Critical for understanding project goals, philosophy, and constraints (especially if making any architectural or design decisions)
2. ✅ Understand project goal: KDL parser/serializer for .NET with v2.0 specification support
3. ✅ Know key locations: `KdlSharp/` (library), `KdlSharp.Tests/` (tests), `specs/` (official KDL specification)
4. ✅ Build succeeds: `dotnet build KdlSharp.sln --configuration Release`
5. ✅ Tests pass: `dotnet test KdlSharp.Tests/KdlSharp.Tests.csproj --configuration Release`
6. ✅ Code style passes: `dotnet format KdlSharp.sln --verify-no-changes`
7. ✅ Check `specs/SPEC.md` for KDL v2.0 specification details (if implementing KDL features)
8. ✅ Verify `README.md` for user-facing documentation (if adding user-facing features)

## Working with KDL Specifications

### KDL Specification Compliance

The official KDL specification is the source of truth for all parsing and serialization behavior:

- **Primary spec**: `specs/SPEC.md` (KDL v2.0 - current)
- **Legacy spec**: `specs/SPEC_v1.md` (KDL v1.0 - legacy support)
- **Query spec**: `specs/QUERY-SPEC.md` (KDL Query Language)
- **Schema spec**: `specs/SCHEMA-SPEC.md` (KDL Schema validation)
- **Official tests**: `specs/tests/test_cases/` (must all pass for specification compliance)

### Implementing Specification Features

**When implementing spec features**:
1. Read the relevant specification section carefully
2. Identify all requirements (including edge cases mentioned in spec)
3. Check official tests for examples of expected behavior
4. Implement with specification wording in mind (match terminology)
5. Verify implementation passes all relevant official tests
6. Add additional tests for edge cases not covered by official suite

**If specification is ambiguous**:
1. Check other official KDL implementations (Rust, JavaScript) for guidance
2. Examine official test cases for intended behavior
3. Make a reasonable decision and document it with comments
4. Consider raising an issue on the official KDL specification repo

### KDL-Specific Exception Types

KdlSharp uses specific exception types for different error categories:

- **`KdlParseException`**: Syntax errors during parsing (invalid KDL syntax)
- **`KdlValidationException`**: Schema validation failures (document doesn't match schema)
- **`KdlQueryException`**: Errors in query expressions
- **`KdlSchemaException`**: Schema document itself is malformed or invalid
- **`KdlSerializationException`**: Errors during POCO serialization/deserialization

All exceptions include source position (line, column) when relevant and should provide clear, actionable error messages.
