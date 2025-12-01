using FluentAssertions;
using KdlSharp.Serialization;
using Xunit;

namespace KdlSharp.Tests.SerializerTests;

/// <summary>
/// Tests for collection serialization and deserialization.
/// </summary>
public class CollectionSerializationTests
{
    [Fact]
    public void Serialize_ListOfStrings_Success()
    {
        var serializer = new KdlSerializer();
        var data = new ListWrapper { Items = new List<string> { "one", "two", "three" } };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("one");
        kdl.Should().Contain("two");
        kdl.Should().Contain("three");
    }

    [Fact]
    public void RoundTrip_ListOfStrings_Success()
    {
        var serializer = new KdlSerializer();
        var original = new ListWrapper { Items = new List<string> { "alpha", "beta", "gamma" } };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<ListWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Items.Should().BeEquivalentTo(new[] { "alpha", "beta", "gamma" });
    }

    [Fact]
    public void Serialize_ListOfIntegers_Success()
    {
        var serializer = new KdlSerializer();
        var data = new IntListWrapper { Numbers = new List<int> { 1, 2, 3, 4, 5 } };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("1");
        kdl.Should().Contain("5");
    }

    [Fact]
    public void RoundTrip_ListOfIntegers_Success()
    {
        var serializer = new KdlSerializer();
        var original = new IntListWrapper { Numbers = new List<int> { 10, 20, 30 } };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<IntListWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Numbers.Should().BeEquivalentTo(new[] { 10, 20, 30 });
    }

    [Fact]
    public void Serialize_ArrayOfStrings_Success()
    {
        var serializer = new KdlSerializer();
        var data = new ArrayWrapper { Values = new[] { "a", "b", "c" } };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("a");
        kdl.Should().Contain("b");
        kdl.Should().Contain("c");
    }

    [Fact]
    public void RoundTrip_ArrayOfStrings_Success()
    {
        var serializer = new KdlSerializer();
        var original = new ArrayWrapper { Values = new[] { "x", "y", "z" } };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<ArrayWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Values.Should().BeEquivalentTo(new[] { "x", "y", "z" });
    }

    [Fact]
    public void Serialize_EmptyList_Success()
    {
        var serializer = new KdlSerializer();
        var data = new ListWrapper { Items = new List<string>() };

        var kdl = serializer.Serialize(data);

        // Should serialize successfully even with empty collection
        kdl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RoundTrip_EmptyList_Success()
    {
        var serializer = new KdlSerializer();
        var original = new ListWrapper { Items = new List<string>() };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<ListWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_ListOfDecimals_Success()
    {
        var serializer = new KdlSerializer();
        var data = new DecimalListWrapper { Amounts = new List<decimal> { 1.5m, 2.75m, 3.125m } };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("1.5");
        kdl.Should().Contain("2.75");
    }

    [Fact]
    public void RoundTrip_ListOfDecimals_Success()
    {
        var serializer = new KdlSerializer();
        var original = new DecimalListWrapper { Amounts = new List<decimal> { 100.50m, 200.75m } };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<DecimalListWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Amounts.Should().BeEquivalentTo(new[] { 100.50m, 200.75m });
    }

    [Fact]
    public void Serialize_ListOfBooleans_Success()
    {
        var serializer = new KdlSerializer();
        var data = new BoolListWrapper { Flags = new List<bool> { true, false, true } };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("#true");
        kdl.Should().Contain("#false");
    }

    [Fact]
    public void RoundTrip_ListOfBooleans_Success()
    {
        var serializer = new KdlSerializer();
        var original = new BoolListWrapper { Flags = new List<bool> { true, false, true, false } };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<BoolListWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Flags.Should().BeEquivalentTo(new[] { true, false, true, false });
    }

    [Fact]
    public void Serialize_ListOfPOCOs_Success()
    {
        var serializer = new KdlSerializer();
        var data = new PersonListWrapper
        {
            People = new List<PersonItem>
            {
                new PersonItem("Alice", 30),
                new PersonItem("Bob", 25)
            }
        };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("Alice");
        kdl.Should().Contain("Bob");
        kdl.Should().Contain("30");
        kdl.Should().Contain("25");
    }

    [Fact]
    public void RoundTrip_ListOfPOCOs_Success()
    {
        var serializer = new KdlSerializer();
        var original = new PersonListWrapper
        {
            People = new List<PersonItem>
            {
                new PersonItem("Charlie", 35),
                new PersonItem("Diana", 28)
            }
        };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<PersonListWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.People.Should().HaveCount(2);
        deserialized.People[0].Name.Should().Be("Charlie");
        deserialized.People[0].Age.Should().Be(35);
        deserialized.People[1].Name.Should().Be("Diana");
        deserialized.People[1].Age.Should().Be(28);
    }

    [Fact]
    public void RoundTrip_ArrayOfPOCOs_Success()
    {
        var serializer = new KdlSerializer();
        var original = new PersonArrayWrapper
        {
            People = new[]
            {
                new PersonItem("Eve", 22),
                new PersonItem("Frank", 40)
            }
        };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<PersonArrayWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.People.Should().HaveCount(2);
        deserialized.People[0].Name.Should().Be("Eve");
        deserialized.People[0].Age.Should().Be(22);
        deserialized.People[1].Name.Should().Be("Frank");
        deserialized.People[1].Age.Should().Be(40);
    }

    [Fact]
    public void RoundTrip_EmptyPOCOList_Success()
    {
        var serializer = new KdlSerializer();
        var original = new PersonListWrapper { People = new List<PersonItem>() };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<PersonListWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.People.Should().BeEmpty();
    }
}

// Test data types
internal class ListWrapper
{
    public List<string> Items { get; set; } = new();
}

internal class IntListWrapper
{
    public List<int> Numbers { get; set; } = new();
}

internal class ArrayWrapper
{
    public string[] Values { get; set; } = Array.Empty<string>();
}

internal class DecimalListWrapper
{
    public List<decimal> Amounts { get; set; } = new();
}

internal class BoolListWrapper
{
    public List<bool> Flags { get; set; } = new();
}

// POCO item for collection tests
internal record PersonItem(string Name, int Age);

internal class PersonListWrapper
{
    public List<PersonItem> People { get; set; } = new();
}

internal class PersonArrayWrapper
{
    public PersonItem[] People { get; set; } = Array.Empty<PersonItem>();
}
