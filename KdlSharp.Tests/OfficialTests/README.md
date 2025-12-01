# Official KDL Test Suite

This directory contains the test runner for the official KDL specification test suite.

## How It Works

The test runner reads test data directly from the `specs/tests/test_cases` directory (the official KDL repository submodule). No copying or symlinks are required.

## Setup

### For Local Development

Ensure the submodule is initialized:

```bash
git submodule update --init --recursive
```

Then run the tests:

```bash
dotnet test --filter "FullyQualifiedName~OfficialTestRunner"
```

### For CI/CD

The GitHub workflows use `submodules: recursive` during checkout, which automatically initializes the specs submodule.

## Test Structure

The official test suite in `specs/tests/test_cases/` is organized as follows:

```
specs/tests/test_cases/
├── input/           # Input KDL files
│   ├── *.kdl        # Test cases
│   └── *_fail.kdl   # Tests that should fail parsing
└── expected_kdl/    # Expected output for successful parses
    └── *.kdl        # Normalized expected output
```

## Running Tests

```bash
# Run all official tests
dotnet test --filter "FullyQualifiedName~OfficialTestRunner"

# Run with verbose output
dotnet test --filter "FullyQualifiedName~OfficialTestRunner" --verbosity normal
```

## Test Manifest

The `manifest.json` file specifies version requirements for specific tests.

### Format

```json
{
  "defaultVersion": "v2",
  "tests": {
    "v2_only": ["test1", "test2"],
    "v1_only": ["test3"],
    "both": ["test4", "test5"]
  }
}
```

### Important Constraints

- **Test names must NOT include the `.kdl` extension** - use `"binary_underscore"` not `"binary_underscore.kdl"`
- **Property names use snake_case** - use `v2_only`, `v1_only`, `both` (the test runner uses case-insensitive matching)
- **Arrays must contain only test name strings** - do not put comments or other values in the arrays
- **No comments in JSON** - Standard JSON does not support comments (`// comment` or `/* comment */`). If you need to document why a test is in a particular category, add it to this README instead.
- **If a test is not listed**, it defaults to `defaultVersion` (typically `v2`)

### Version Categories

- `v2_only`: Tests that use KDL v2.0-only features (e.g., `#true`, `#false`, multiline strings, underscores in numbers)
- `v1_only`: Tests that use KDL v1.0-only syntax (e.g., bare `true`, `false`, `null` without `#` prefix)
- `both`: Tests that work identically in both v1 and v2

If no manifest is provided or manifest loading fails, all tests default to v2.

## Updating to Latest Tests

To update to the latest official tests:

```bash
git submodule update --remote --merge
```

## Current Status

The test runner supports:
- ✅ Basic parsing tests
- ✅ Error detection (_fail tests)
- ✅ Version-specific tests (v1/v2)
- ✅ Structural document comparison

Known limitations:
- ⚠️ **Comment preservation**: Comments are discarded during parsing. This is intentional per selected scope and may be added in a future version.
- ⚠️ **Number formatting edge cases**: Minor differences in number serialization (e.g., trailing zeros, exponent notation) may occur. The parsed numeric values are accurate; only the text representation may vary.
