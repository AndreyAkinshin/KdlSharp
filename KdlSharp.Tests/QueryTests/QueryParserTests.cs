using FluentAssertions;
using KdlSharp.Query;
using Xunit;

namespace KdlSharp.Tests.QueryTests;

public class QueryParserTests
{
    [Fact]
    public void Parse_SimpleNodeName_Success()
    {
        var query = QueryParser.Parse("package");
        query.Should().NotBeNull();
        query.Selectors.Should().HaveCount(1);

        var selector = query.Selectors[0];
        selector.Segments.Should().HaveCount(1);

        var segment = selector.Segments[0];
        segment.Filter.Should().BeOfType<MatchersFilter>();
        var filter = (MatchersFilter)segment.Filter;
        filter.NodeName.Should().Be("package");
    }

    [Fact]
    public void Parse_TypeMatcher_Success()
    {
        var query = QueryParser.Parse("(foo)");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        filter.TypeMatcher.Should().NotBeNull();
        filter.TypeMatcher!.TypeName.Should().Be("foo");
    }

    [Fact]
    public void Parse_AnyTypeMatcher_Success()
    {
        var query = QueryParser.Parse("()");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        filter.TypeMatcher.Should().NotBeNull();
        filter.TypeMatcher!.IsAnyType.Should().BeTrue();
    }

    [Fact]
    public void Parse_TopFilter_Success()
    {
        var query = QueryParser.Parse("top()");
        query.Should().NotBeNull();

        var filter = query.Selectors[0].Segments[0].Filter;
        filter.Should().BeOfType<TopFilter>();
    }

    [Fact]
    public void Parse_ChildOperator_Success()
    {
        var query = QueryParser.Parse("a > b");
        query.Should().NotBeNull();

        var selector = query.Selectors[0];
        selector.Segments.Should().HaveCount(2);
        selector.Segments[0].Operator.Should().Be(SelectorOperator.Child);
    }

    [Fact]
    public void Parse_DescendantOperator_Success()
    {
        var query = QueryParser.Parse("a >> b");
        query.Should().NotBeNull();

        var selector = query.Selectors[0];
        selector.Segments.Should().HaveCount(2);
        selector.Segments[0].Operator.Should().Be(SelectorOperator.Descendant);
    }

    [Fact]
    public void Parse_NextSiblingOperator_Success()
    {
        var query = QueryParser.Parse("a + b");
        query.Should().NotBeNull();

        var selector = query.Selectors[0];
        selector.Segments.Should().HaveCount(2);
        selector.Segments[0].Operator.Should().Be(SelectorOperator.NextSibling);
    }

    [Fact]
    public void Parse_FollowingSiblingOperator_Success()
    {
        var query = QueryParser.Parse("a ++ b");
        query.Should().NotBeNull();

        var selector = query.Selectors[0];
        selector.Segments.Should().HaveCount(2);
        selector.Segments[0].Operator.Should().Be(SelectorOperator.FollowingSibling);
    }

    [Fact]
    public void Parse_UnionOperator_Success()
    {
        var query = QueryParser.Parse("a > b || c > d");
        query.Should().NotBeNull();
        query.Selectors.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_EmptyAccessorMatcher_Success()
    {
        var query = QueryParser.Parse("[]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        filter.AccessorMatchers.Should().HaveCount(1);
        filter.AccessorMatchers[0].IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Parse_ValAccessor_Success()
    {
        var query = QueryParser.Parse("[val()]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        filter.AccessorMatchers.Should().HaveCount(1);
        filter.AccessorMatchers[0].Accessor.Should().BeOfType<ValAccessor>();
        ((ValAccessor)filter.AccessorMatchers[0].Accessor!).Index.Should().Be(0);
    }

    [Fact]
    public void Parse_ValAccessorWithIndex_Success()
    {
        var query = QueryParser.Parse("[val(1)]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        var accessor = (ValAccessor)filter.AccessorMatchers[0].Accessor!;
        accessor.Index.Should().Be(1);
    }

    [Fact]
    public void Parse_PropAccessor_Success()
    {
        var query = QueryParser.Parse("[prop(foo)]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        var accessor = (PropAccessor)filter.AccessorMatchers[0].Accessor!;
        accessor.PropertyName.Should().Be("foo");
    }

    [Fact]
    public void Parse_PropAccessorShorthand_Success()
    {
        var query = QueryParser.Parse("[foo]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        var accessor = (PropAccessor)filter.AccessorMatchers[0].Accessor!;
        accessor.PropertyName.Should().Be("foo");
    }

    [Fact]
    public void Parse_NameAccessor_Success()
    {
        var query = QueryParser.Parse("[name()]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        filter.AccessorMatchers[0].Accessor.Should().BeOfType<NameAccessor>();
    }

    [Fact]
    public void Parse_TagAccessor_Success()
    {
        var query = QueryParser.Parse("[tag()]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        filter.AccessorMatchers[0].Accessor.Should().BeOfType<TagAccessor>();
    }

    [Fact]
    public void Parse_EqualityComparison_Success()
    {
        var query = QueryParser.Parse("[val() = 42]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        filter.AccessorMatchers[0].Comparison.Should().NotBeNull();
        var comparison = filter.AccessorMatchers[0].Comparison!;
        comparison.Operator.Should().Be(MatcherOperator.Equal);
        comparison.Right.Should().Be(42m);
    }

    [Fact]
    public void Parse_StringComparison_Success()
    {
        var query = QueryParser.Parse("[name() = \"foo\"]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        var comparison = filter.AccessorMatchers[0].Comparison!;
        comparison.Operator.Should().Be(MatcherOperator.Equal);
        comparison.Right.Should().Be("foo");
    }

    [Fact]
    public void Parse_StartsWithOperator_Success()
    {
        var query = QueryParser.Parse("[val() ^= \"foo\"]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        var comparison = filter.AccessorMatchers[0].Comparison!;
        comparison.Operator.Should().Be(MatcherOperator.StartsWith);
    }

    [Fact]
    public void Parse_EndsWithOperator_Success()
    {
        var query = QueryParser.Parse("[val() $= \"foo\"]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        var comparison = filter.AccessorMatchers[0].Comparison!;
        comparison.Operator.Should().Be(MatcherOperator.EndsWith);
    }

    [Fact]
    public void Parse_ContainsOperator_Success()
    {
        var query = QueryParser.Parse("[val() *= \"foo\"]");
        query.Should().NotBeNull();

        var filter = (MatchersFilter)query.Selectors[0].Segments[0].Filter;
        var comparison = filter.AccessorMatchers[0].Comparison!;
        comparison.Operator.Should().Be(MatcherOperator.Contains);
    }

    [Fact]
    public void Parse_ComplexQuery_Success()
    {
        var query = QueryParser.Parse("package >> name[prop(foo) = \"bar\"]");
        query.Should().NotBeNull();

        var selector = query.Selectors[0];
        selector.Segments.Should().HaveCount(2);
        selector.Segments[0].Operator.Should().Be(SelectorOperator.Descendant);
    }
}

