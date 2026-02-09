using System.Text.Json;
using FluentAssertions;
using KdlSharp;
using KdlSharp.Settings;
using Xunit;
using Xunit.Abstractions;

namespace KdlSharp.Tests.OfficialTests;

public class OfficialTestRunner
{
    private readonly ITestOutputHelper output;
    private static readonly string manifestPath = Path.Combine(AppContext.BaseDirectory, "OfficialTests", "manifest.json");
    private static readonly string? testDataPath = FindTestDataPath();
    private static TestManifest? manifest;

    public OfficialTestRunner(ITestOutputHelper output)
    {
        this.output = output;
        LoadManifest();
    }

    /// <summary>
    /// Finds the test data path by locating the specs/tests/test_cases directory in the repository.
    /// Returns null if the submodule is not initialized.
    /// </summary>
    private static string? FindTestDataPath()
    {
        // Navigate up from bin/Debug/net9.0 to repository root
        var currentDir = AppContext.BaseDirectory;

        for (int i = 0; i < 10; i++)
        {
            var parent = Directory.GetParent(currentDir);
            if (parent == null)
                break;

            currentDir = parent.FullName;

            // Check if this looks like the repo root (has KdlSharp.sln)
            if (File.Exists(Path.Combine(currentDir, "KdlSharp.sln")))
            {
                var specsPath = Path.Combine(currentDir, "specs", "tests", "test_cases");
                if (Directory.Exists(specsPath))
                    return specsPath;
                break;
            }
        }

        return null;
    }

    private static void LoadManifest()
    {
        if (manifest != null) return;

        try
        {
            if (File.Exists(manifestPath))
            {
                var json = File.ReadAllText(manifestPath);
                manifest = JsonSerializer.Deserialize<TestManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
            }
        }
        catch
        {
            // If manifest loading fails, use default (v2)
            manifest = new TestManifest { DefaultVersion = "v2", Tests = new TestVersions() };
        }

        manifest ??= new TestManifest { DefaultVersion = "v2", Tests = new TestVersions() };
    }

    private string GetTestVersion(string testName)
    {
        if (manifest?.Tests?.V2Only?.Contains(testName) == true)
            return "v2";
        if (manifest?.Tests?.V1Only?.Contains(testName) == true)
            return "v1";
        if (manifest?.Tests?.Both?.Contains(testName) == true)
            return "both";

        return manifest?.DefaultVersion ?? "v2";
    }

    public static IEnumerable<object[]> GetTestCases()
    {
        if (testDataPath == null)
        {
            // Yield a sentinel so xUnit doesn't fail with "No data found"
            yield return new object[] { "submodule_not_initialized", "", "", false, false };
            yield break;
        }

        var inputPath = Path.Combine(testDataPath, "input");
        if (!Directory.Exists(inputPath))
            yield break;

        foreach (var inputFile in Directory.GetFiles(inputPath, "*.kdl", SearchOption.AllDirectories))
        {
            var testName = Path.GetFileNameWithoutExtension(inputFile);
            var relativePath = Path.GetRelativePath(inputPath, inputFile);
            var expectedFile = Path.Combine(testDataPath, "expected_kdl", relativePath);

            var shouldFail = testName.EndsWith("_fail");
            var hasExpected = File.Exists(expectedFile);

            yield return new object[] { testName, inputFile, expectedFile, shouldFail, hasExpected };
        }
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void RunOfficialTest(string testName, string inputFile, string expectedFile, bool shouldFail, bool hasExpected)
    {
        if (testDataPath == null)
        {
            output.WriteLine("Skipped: specs submodule not initialized (run 'git submodule update --init')");
            return;
        }

        var version = GetTestVersion(testName);
        output.WriteLine($"Running test: {testName} (version: {version})");

        var inputKdl = File.ReadAllText(inputFile);

        if (shouldFail)
        {
            // Test should fail to parse in all applicable versions
            var versionsToTest = version == "both" ? new[] { "v1", "v2" } : new[] { version };

            foreach (var v in versionsToTest)
            {
                var settings = new KdlParserSettings
                {
                    TargetVersion = v == "v1" ? KdlVersion.V1 : KdlVersion.V2
                };

                try
                {
                    var doc = KdlDocument.Parse(inputKdl, settings);
                    Assert.Fail($"Test {testName} should have failed to parse in {v}, but succeeded");
                }
                catch (Exceptions.KdlParseException)
                {
                    // Expected
                    output.WriteLine($"  ✓ Failed as expected ({v})");
                }
            }
            return;
        }

        if (!hasExpected)
        {
            // Skip tests without expected output for now
            output.WriteLine($"  ⊘ Skipped (no expected output)");
            return;
        }

        try
        {
            // Determine which version(s) to test
            var versionsToTest = version == "both" ? new[] { "v1", "v2" } : new[] { version };

            foreach (var v in versionsToTest)
            {
                var settings = new KdlParserSettings
                {
                    TargetVersion = v == "v1" ? KdlVersion.V1 : KdlVersion.V2
                };

                // Parse the input
                var doc = KdlDocument.Parse(inputKdl, settings);

                // Serialize it back
                var serialized = doc.ToKdlString();

                // Parse the expected output (always with v2 since expected files are normalized)
                var expectedKdl = File.ReadAllText(expectedFile);
                var expectedDoc = KdlDocument.Parse(expectedKdl);

                // Compare the two documents structurally
                CompareDocuments(doc, expectedDoc, $"{testName} ({v})");

                output.WriteLine($"  ✓ Passed ({v})");
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"  ✗ Failed: {ex.Message}");
            throw;
        }
    }

    private void CompareDocuments(KdlDocument actual, KdlDocument expected, string testName)
    {
        actual.Nodes.Should().HaveCount(expected.Nodes.Count,
            $"test {testName} should have {expected.Nodes.Count} nodes");

        for (int i = 0; i < actual.Nodes.Count; i++)
        {
            CompareNodes(actual.Nodes[i], expected.Nodes[i], $"{testName}/node[{i}]");
        }
    }

    private void CompareNodes(KdlNode actual, KdlNode expected, string path)
    {
        actual.Name.Should().Be(expected.Name, $"at {path}");
        actual.Arguments.Should().HaveCount(expected.Arguments.Count, $"at {path}");

        // Normalize properties: per test suite README, duplicates should be removed
        // with only the rightmost one remaining
        var normalizedActual = NormalizeProperties(actual.Properties);
        var normalizedExpected = NormalizeProperties(expected.Properties);

        normalizedActual.Should().HaveCount(normalizedExpected.Count, $"at {path}");
        actual.Children.Should().HaveCount(expected.Children.Count, $"at {path}");

        // Compare arguments
        for (int i = 0; i < actual.Arguments.Count; i++)
        {
            CompareValues(actual.Arguments[i], expected.Arguments[i], $"{path}/arg[{i}]");
        }

        // Compare properties (normalized, in alphabetical order per test suite README)
        for (int i = 0; i < normalizedExpected.Count; i++)
        {
            var expectedProp = normalizedExpected[i];
            var actualProp = normalizedActual[i];
            actualProp.Key.Should().Be(expectedProp.Key, $"at {path}/prop[{i}]");
            CompareValues(actualProp.Value, expectedProp.Value, $"{path}/prop[{expectedProp.Key}]");
        }

        // Compare children
        for (int i = 0; i < actual.Children.Count; i++)
        {
            CompareNodes(actual.Children[i], expected.Children[i], $"{path}/child[{i}]");
        }
    }

    /// <summary>
    /// Normalizes properties by keeping only the rightmost property for each key
    /// and sorting alphabetically, per the official test suite README.
    /// </summary>
    private static List<KdlProperty> NormalizeProperties(IList<KdlProperty> properties)
    {
        // Keep only the rightmost property for each key
        var deduped = new Dictionary<string, KdlProperty>();
        foreach (var prop in properties)
        {
            deduped[prop.Key] = prop;
        }

        // Sort alphabetically by key
        return deduped.Values.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
    }

    private void CompareValues(KdlValue actual, KdlValue expected, string path)
    {
        actual.ValueType.Should().Be(expected.ValueType, $"at {path}");

        switch (expected.ValueType)
        {
            case KdlValueType.String:
                actual.AsString().Should().Be(expected.AsString(), $"at {path}");
                break;
            case KdlValueType.Number:
                // Handle special numbers (infinity, NaN) which return null from AsNumber()
                var actualNum = actual as KdlSharp.Values.KdlNumber;
                var expectedNum = expected as KdlSharp.Values.KdlNumber;
                if (actualNum?.IsSpecial == true || expectedNum?.IsSpecial == true)
                {
                    actualNum!.IsPositiveInfinity.Should().Be(expectedNum!.IsPositiveInfinity, $"at {path} (positive infinity)");
                    actualNum.IsNegativeInfinity.Should().Be(expectedNum.IsNegativeInfinity, $"at {path} (negative infinity)");
                    actualNum.IsNaN.Should().Be(expectedNum.IsNaN, $"at {path} (NaN)");
                }
                else
                {
                    actual.AsNumber().Should().BeApproximately(expected.AsNumber()!.Value, 0.0000001m, $"at {path}");
                }
                break;
            case KdlValueType.Boolean:
                actual.AsBoolean().Should().Be(expected.AsBoolean(), $"at {path}");
                break;
            case KdlValueType.Null:
                actual.IsNull().Should().BeTrue($"at {path}");
                break;
        }
    }
}

// Manifest data structures
internal class TestManifest
{
    public string? DefaultVersion { get; set; }
    public TestVersions? Tests { get; set; }
}

internal class TestVersions
{
    public List<string>? V2Only { get; set; }
    public List<string>? V1Only { get; set; }
    public List<string>? Both { get; set; }
}

