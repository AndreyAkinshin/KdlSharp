using AwesomeAssertions;
using KdlSharp.Exceptions;
using KdlSharp.Query;
using Xunit;

namespace KdlSharp.Tests.QueryTests;

/// <summary>
/// Tests for query parsing error conditions that should throw KdlQueryException.
/// </summary>
public class KdlQueryExceptionTests
{
    [Theory]
    [InlineData(">>>")]
    [InlineData(">>>>")]
    public void Parse_MultipleConsecutiveOperatorsWithoutOperands_ThrowsKdlQueryException(string query)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>();
    }

    [Theory]
    [InlineData("a >")]
    [InlineData("a >>")]
    [InlineData("a +")]
    [InlineData("a ++")]
    public void Parse_OperatorWithoutFollowingOperand_ThrowsKdlQueryException(string query)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>();
    }

    [Theory]
    [InlineData("[val(abc)]")]
    [InlineData("[val(1.5)]")]
    public void Parse_MalformedValAccessor_ThrowsKdlQueryException(string query)
    {
        // val() accessor only accepts integers; these should fail
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>();
    }

    [Theory]
    [InlineData("[prop()]")]
    public void Parse_MalformedPropAccessor_ThrowsKdlQueryException(string query)
    {
        // prop() requires a property name
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>();
    }

    [Theory]
    [InlineData("[val()")]
    [InlineData("[prop(foo)")]
    [InlineData("[name()")]
    public void Parse_MissingClosingBracket_ThrowsKdlQueryException(string query)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>();
    }

    [Theory]
    [InlineData("(")]
    [InlineData("(foo")]
    public void Parse_MissingClosingParenthesis_ThrowsKdlQueryException(string query)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>();
    }

    [Theory]
    [InlineData("top(")]
    public void Parse_MalformedTopFilter_ThrowsKdlQueryException(string query)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>()
            .WithMessage("*Expected ')'*");
    }

    [Theory]
    [InlineData("\"unterminated", "Unterminated string")]
    [InlineData("[val() = \"unterminated]", "Unterminated string")]
    public void Parse_UnterminatedString_ThrowsKdlQueryException(string query, string expectedMessagePart)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>()
            .WithMessage($"*{expectedMessagePart}*");
    }

    [Theory]
    [InlineData("[val() = ]", "Expected literal")]
    public void Parse_MissingComparisonValue_ThrowsKdlQueryException(string query, string expectedMessagePart)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>()
            .WithMessage($"*{expectedMessagePart}*");
    }

    [Theory]
    [InlineData("\"\\q\"", "Invalid escape sequence")]
    [InlineData("\"\\x\"", "Invalid escape sequence")]
    public void Parse_InvalidEscapeSequence_ThrowsKdlQueryException(string query, string expectedMessagePart)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>()
            .WithMessage($"*{expectedMessagePart}*");
    }

    [Fact]
    public void Parse_InvalidQuery_ExceptionContainsQueryContext()
    {
        var query = "[val(abc)]";

        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>()
            .Where(ex => ex.Query.Contains("val"));
    }

    [Fact]
    public void KdlQueryException_ContainsQueryProperty()
    {
        var exception = new KdlQueryException("Test message", "test query");

        exception.Query.Should().Be("test query");
        exception.Message.Should().Contain("Test message");
        exception.Message.Should().Contain("test query");
    }

    [Theory]
    [InlineData("a > b extra", "Unexpected content")]
    [InlineData("package garbage", "Unexpected content")]
    public void Parse_UnexpectedContentAfterQuery_ThrowsKdlQueryException(string query, string expectedMessagePart)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>()
            .WithMessage($"*{expectedMessagePart}*");
    }

    [Theory]
    [InlineData("[name() >=]", "Expected literal")]
    [InlineData("[name() <=]", "Expected literal")]
    public void Parse_ComparisonOperatorWithoutValue_ThrowsKdlQueryException(string query, string expectedMessagePart)
    {
        var action = () => QueryParser.Parse(query);

        action.Should().Throw<KdlQueryException>()
            .WithMessage($"*{expectedMessagePart}*");
    }
}
