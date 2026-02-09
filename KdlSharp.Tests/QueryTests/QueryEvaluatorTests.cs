using AwesomeAssertions;
using KdlSharp;
using KdlSharp.Query;
using Xunit;

namespace KdlSharp.Tests.QueryTests;

public class QueryEvaluatorTests
{
    [Fact]
    public void Execute_SimpleNodeName_ReturnsMatchingNodes()
    {
        var kdl = @"
            package
            name
            version
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "package").ToList();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("package");
    }

    [Fact]
    public void Execute_TopFilter_ReturnsTopLevelNodes()
    {
        var kdl = @"
            package {
                name
            }
            version
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "top()").ToList();

        results.Should().HaveCount(2);
        results.Select(n => n.Name).Should().BeEquivalentTo(new[] { "package", "version" });
    }

    [Fact]
    public void Execute_ChildOperator_ReturnsDirectChildren()
    {
        var kdl = @"
            package {
                name
                version {
                    number
                }
            }
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "package > []").ToList();

        results.Should().HaveCount(2);
        results.Select(n => n.Name).Should().BeEquivalentTo(new[] { "name", "version" });
    }

    [Fact]
    public void Execute_DescendantOperator_ReturnsAllDescendants()
    {
        var kdl = @"
            package {
                name
                version {
                    number
                }
            }
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "package >> []").ToList();

        results.Should().HaveCount(3);
        results.Select(n => n.Name).Should().BeEquivalentTo(new[] { "name", "version", "number" });
    }

    [Fact]
    public void Execute_PropertyMatcher_FiltersNodes()
    {
        var kdl = @"
            dep1 platform=""windows""
            dep2
            dep3 platform=""linux""
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "[platform]").ToList();

        results.Should().HaveCount(2);
        results.Select(n => n.Name).Should().BeEquivalentTo(new[] { "dep1", "dep3" });
    }

    [Fact]
    public void Execute_ValueComparison_FiltersCorrectly()
    {
        var kdl = @"
            item 1
            item 5
            item 10
            item 20
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "[val() > 5]").ToList();

        results.Should().HaveCount(2);
    }

    [Fact]
    public void Execute_StringStartsWith_FiltersCorrectly()
    {
        var kdl = @"
            ""foo-bar""
            ""foo-baz""
            ""bar-foo""
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "[name() ^= \"foo\"]").ToList();

        results.Should().HaveCount(2);
    }

    [Fact]
    public void Execute_TypeAnnotation_FiltersCorrectly()
    {
        var kdl = @"
            (type1)node1
            (type2)node2
            node3
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "(type1)").ToList();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("node1");
    }

    [Fact]
    public void Execute_AnyTypeAnnotation_FiltersCorrectly()
    {
        var kdl = @"
            (type1)node1
            (type2)node2
            node3
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "()").ToList();

        results.Should().HaveCount(2);
    }

    [Fact]
    public void Execute_UnionOperator_CombinesResults()
    {
        var kdl = @"
            package
            name
            version
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "package || version").ToList();

        results.Should().HaveCount(2);
        results.Select(n => n.Name).Should().BeEquivalentTo(new[] { "package", "version" });
    }

    [Fact]
    public void Execute_ComplexQuery_WorksCorrectly()
    {
        var kdl = @"
            package {
                name ""my-package""
                version ""1.0.0""
                dependencies platform=""windows"" {
                    winapi ""1.0.0""
                }
                dependencies {
                    miette ""2.0.0""
                }
            }
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "package >> dependencies[platform]").ToList();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("dependencies");
        results[0].HasProperty("platform").Should().BeTrue();
    }

    [Fact]
    public void Execute_NextSibling_ReturnsImmediateFollowingNode()
    {
        var kdl = @"
            node1
            node2
            node3
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "node1 + []").ToList();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("node2");
    }

    [Fact]
    public void Execute_FollowingSibling_ReturnsAllFollowingNodes()
    {
        var kdl = @"
            node1
            node2
            node3
            node4
        ";
        var doc = KdlDocument.Parse(kdl);
        var results = KdlQuery.Execute(doc, "node1 ++ []").ToList();

        results.Should().HaveCount(3);
        results.Select(n => n.Name).Should().BeEquivalentTo(new[] { "node2", "node3", "node4" });
    }

    [Fact]
    public void Compile_Query_CanBeReused()
    {
        var kdl = @"
            package
            name
        ";
        var doc = KdlDocument.Parse(kdl);
        var compiled = KdlQuery.Compile("package");

        var results1 = compiled.Execute(doc).ToList();
        var results2 = compiled.Execute(doc).ToList();

        results1.Should().HaveCount(1);
        results2.Should().HaveCount(1);
    }
}

