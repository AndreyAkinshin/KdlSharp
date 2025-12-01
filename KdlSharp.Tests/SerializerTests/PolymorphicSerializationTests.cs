using FluentAssertions;
using KdlSharp;
using KdlSharp.Serialization;
using Xunit;

namespace KdlSharp.Tests.SerializerTests;

/// <summary>
/// Tests for polymorphic serialization ensuring runtime types are honored.
/// </summary>
public class PolymorphicSerializationTests
{
    #region Base Class Serialization

    [Fact]
    public void Serialize_DerivedTypeAsDeclaredBase_UsesRuntimeTypeProperties()
    {
        // Arrange
        BaseAnimal animal = new Dog("Buddy", "Golden Retriever");
        var serializer = new KdlSerializer();

        // Act
        var kdl = serializer.Serialize(animal);

        // Assert - should include derived type's Breed property
        kdl.Should().Contain("Buddy");
        kdl.Should().Contain("Golden Retriever");
        kdl.Should().Contain("breed");
    }

    [Fact]
    public void Serialize_DerivedTypeAsDeclaredBase_RoundTripsCorrectlyWhenDeserializedAsDerived()
    {
        // Arrange
        BaseAnimal animal = new Dog("Max", "Labrador");
        var serializer = new KdlSerializer();

        // Act
        var kdl = serializer.Serialize(animal);
        var deserialized = serializer.Deserialize<Dog>(kdl);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Name.Should().Be("Max");
        deserialized.Breed.Should().Be("Labrador");
    }

    #endregion

    #region Interface Serialization

    [Fact]
    public void Serialize_ConcreteTypeAsInterface_UsesRuntimeTypeProperties()
    {
        // Arrange
        IVehicle vehicle = new Car("Tesla", "Model 3", 4);
        var serializer = new KdlSerializer();

        // Act
        var kdl = serializer.Serialize(vehicle);

        // Assert - should include concrete type's DoorCount property
        kdl.Should().Contain("Tesla");
        kdl.Should().Contain("Model 3");
        kdl.Should().Contain("4");
    }

    [Fact]
    public void Serialize_ConcreteTypeAsInterface_RoundTripsCorrectlyWhenDeserializedAsConcrete()
    {
        // Arrange
        IVehicle vehicle = new Car("Ford", "Mustang", 2);
        var serializer = new KdlSerializer();

        // Act
        var kdl = serializer.Serialize(vehicle);
        var deserialized = serializer.Deserialize<Car>(kdl);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Make.Should().Be("Ford");
        deserialized.Model.Should().Be("Mustang");
        deserialized.DoorCount.Should().Be(2);
    }

    #endregion

    #region Async Stream Polymorphic Serialization

    [Fact]
    public async Task SerializeStreamAsync_DerivedTypesAsBase_UsesRuntimeTypeProperties()
    {
        // Arrange
        var serializer = new KdlSerializer();
        using var stream = new MemoryStream();

        // Act
        await serializer.SerializeStreamAsync(GetAnimalsAsync(), stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // Both derived types should have their specific properties serialized
        content.Should().Contain("Golden Retriever"); // Dog.Breed
        content.Should().Contain("tabby"); // Cat.Pattern
    }

    private static async IAsyncEnumerable<BaseAnimal> GetAnimalsAsync()
    {
        await Task.Yield();
        yield return new Dog("Buddy", "Golden Retriever");
        yield return new Cat("Whiskers", "tabby");
    }

    #endregion

    #region Polymorphic Collection Tests

    [Fact]
    public void Serialize_PolymorphicCollection_UsesRuntimeTypeForEachItem()
    {
        // Arrange
        var data = new PolymorphicCollectionWrapper
        {
            Animals = new List<BaseAnimal>
            {
                new Dog("Buddy", "Golden Retriever"),
                new Cat("Whiskers", "tabby")
            }
        };
        var serializer = new KdlSerializer();

        // Act
        var kdl = serializer.Serialize(data);

        // Assert - each item should use its runtime type's properties
        kdl.Should().Contain("Golden Retriever"); // Dog.Breed
        kdl.Should().Contain("tabby"); // Cat.Pattern
    }

    [Fact]
    public void Serialize_PolymorphicCollection_NamingPolicyApplied()
    {
        // Arrange
        var data = new PolymorphicCollectionWrapper
        {
            Animals = new List<BaseAnimal>
            {
                new DerivedAnimalType("Test")
            }
        };
        var serializer = new KdlSerializer(new KdlSerializerOptions
        {
            PropertyNamingPolicy = KdlNamingPolicy.KebabCase
        });

        // Act
        var kdl = serializer.Serialize(data);

        // Assert - node name should follow naming policy (kebab-case)
        kdl.Should().Contain("derived-animal-type");
    }

    #endregion

    #region Child Node Polymorphism Tests

    [Fact]
    public void Serialize_PolymorphicChildNode_UsesRuntimeType()
    {
        // Arrange
        var wrapper = new AnimalHolderWrapper
        {
            Animal = new Dog("Max", "Husky")
        };
        var serializer = new KdlSerializer();

        // Act
        var kdl = serializer.Serialize(wrapper);

        // Assert - should include derived type's Breed property
        kdl.Should().Contain("Max");
        kdl.Should().Contain("Husky");
        kdl.Should().Contain("breed");
    }

    #endregion

    #region Test Helper Types

    public abstract record BaseAnimal(string Name);
    public sealed record Dog(string Name, string Breed) : BaseAnimal(Name);
    public sealed record Cat(string Name, string Pattern) : BaseAnimal(Name);
    public sealed record DerivedAnimalType(string Name) : BaseAnimal(Name);

    public interface IVehicle
    {
        string Make { get; }
        string Model { get; }
    }

    public sealed record Car(string Make, string Model, int DoorCount) : IVehicle;

    public class PolymorphicCollectionWrapper
    {
        public List<BaseAnimal> Animals { get; set; } = new();
    }

    public class AnimalHolderWrapper
    {
        public BaseAnimal? Animal { get; set; }
    }

    #endregion
}
