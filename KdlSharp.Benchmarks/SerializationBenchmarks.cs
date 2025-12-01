using BenchmarkDotNet.Attributes;
using KdlSharp.Serialization;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private KdlSerializer serializer = null!;
    private TestData testData = null!;
    private string serializedKdl = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        serializer = new KdlSerializer(new KdlSerializerOptions
        {
            RootNodeName = "test-data",
            PropertyNamingPolicy = KdlNamingPolicy.KebabCase
        });

        testData = new TestData
        {
            Name = "Test",
            Value = 42,
            IsEnabled = true,
            Tags = ["tag1", "tag2", "tag3"],
            Nested = new NestedData { Description = "Nested object" }
        };

        serializedKdl = serializer.Serialize(testData);
    }

    [Benchmark]
    public string SerializeObject()
    {
        return serializer.Serialize(testData);
    }

    [Benchmark]
    public TestData DeserializeObject()
    {
        return serializer.Deserialize<TestData>(serializedKdl);
    }

    [Benchmark]
    public TestData RoundTrip()
    {
        var kdl = serializer.Serialize(testData);
        return serializer.Deserialize<TestData>(kdl);
    }
}

public class TestData
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool IsEnabled { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public NestedData? Nested { get; set; }
}

public class NestedData
{
    public string Description { get; set; } = string.Empty;
}
