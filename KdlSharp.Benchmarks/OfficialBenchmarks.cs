using BenchmarkDotNet.Attributes;
using KdlSharp;

[MemoryDiagnoser]
public class OfficialBenchmarks
{
    private string htmlStandardKdl = string.Empty;
    private string htmlStandardCompactKdl = string.Empty;
    private KdlDocument htmlStandardDoc = null!;
    private KdlDocument htmlStandardCompactDoc = null!;
    private bool filesAvailable;

    private static string GetBenchmarkFilePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "OfficialBenchmarks", fileName);
    }

    private static bool TryReadFile(string path, out string content)
    {
        content = string.Empty;
        if (!File.Exists(path))
            return false;

        content = File.ReadAllText(path);
        return true;
    }

    [GlobalSetup]
    public void Setup()
    {
        // Gracefully handle missing benchmark files (e.g., when specs submodule isn't initialized)
        var htmlStandardPath = GetBenchmarkFilePath("html-standard.kdl");
        var htmlStandardCompactPath = GetBenchmarkFilePath("html-standard-compact.kdl");

        filesAvailable = TryReadFile(htmlStandardPath, out htmlStandardKdl)
                      && TryReadFile(htmlStandardCompactPath, out htmlStandardCompactKdl);

        if (filesAvailable)
        {
            htmlStandardDoc = KdlDocument.Parse(htmlStandardKdl);
            htmlStandardCompactDoc = KdlDocument.Parse(htmlStandardCompactKdl);
        }
        else
        {
            Console.WriteLine("Warning: Official benchmark files not found. Initialize the specs submodule:");
            Console.WriteLine("  git submodule update --init --recursive");
            Console.WriteLine("Skipping OfficialBenchmarks.");

            // Provide minimal fallback data to prevent NullReferenceException
            htmlStandardKdl = "node";
            htmlStandardCompactKdl = "node";
            htmlStandardDoc = KdlDocument.Parse(htmlStandardKdl);
            htmlStandardCompactDoc = KdlDocument.Parse(htmlStandardCompactKdl);
        }
    }

    [Benchmark]
    public KdlDocument ParseHtmlStandard()
    {
        return KdlDocument.Parse(htmlStandardKdl);
    }

    [Benchmark]
    public KdlDocument ParseHtmlStandardCompact()
    {
        return KdlDocument.Parse(htmlStandardCompactKdl);
    }

    [Benchmark]
    public string SerializeHtmlStandard()
    {
        return htmlStandardDoc.ToKdlString();
    }

    [Benchmark]
    public string SerializeHtmlStandardCompact()
    {
        return htmlStandardCompactDoc.ToKdlString();
    }
}
