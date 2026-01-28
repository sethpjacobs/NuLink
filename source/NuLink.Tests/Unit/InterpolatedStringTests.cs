using AwesomeAssertions;
using NuLink.Cli;
using Xunit;

namespace NuLink.Tests.Unit
{
    public class InterpolatedStringTests
    {
        [Fact]
        public void CanParse()
        {
            var num = 123;
            var str = "abc";
            var parsed = new InterpolatedString(() => $"N={num}, S={str}.");

            parsed.FormatString.Should().Be("N={0}, S={1}.");
            parsed.FormatParts.Should().BeEquivalentTo(new[] { "N=", "", ", S=", "", "." });
            parsed.FormatArgs.Should().BeEquivalentTo(new object[] { 123, "abc" });
        }

        [Fact]
        public void CanParseFormatSpecs()
        {
            var parsed = new InterpolatedString(() => $"N={123:#,###}, S={"abc":-15}.");

            parsed.FormatString.Should().Be("N={0:#,###}, S={1:-15}.");
            parsed.FormatParts.Should().BeEquivalentTo(new[] { "N=", "#,###", ", S=", "-15", "." });
            parsed.FormatArgs.Should().BeEquivalentTo(new object[] { 123, "abc" });
        }

        [Fact]
        public void CanEvaluateArgumentExpressions()
        {
            var num = 123;
            var str = "abc";
            var parsed = new InterpolatedString(() => $"N={num * 2}, S={str.ToUpper()}.");

            parsed.FormatArgs.Should().BeEquivalentTo(new object[] { 246, "ABC" });
        }

        [Fact]
        public void CanParseJustLiteral()
        {
            var parsed = new InterpolatedString(() => $"just-text");

            parsed.FormatString.Should().Be("just-text");
            parsed.FormatParts.Should().BeEquivalentTo(new[] { "just-text" });
            parsed.FormatArgs.Should().BeEmpty();
        }

        [Fact]
        public void CanParseJustSinglePlaceholder()
        {
            var parsed = new InterpolatedString(() => $"{123}");

            parsed.FormatString.Should().Be("{0}");
            parsed.FormatParts.Should().BeEquivalentTo(new[] { "", "", "" });
            parsed.FormatArgs.Should().BeEquivalentTo(new object[] { 123 });
        }

        [Fact]
        public void CanParseJustPlaceholders()
        {
            var parsed = new InterpolatedString(() => $"{123}{"abc"}{456}");

            parsed.FormatString.Should().Be("{0}{1}{2}");
            parsed.FormatParts.Should().BeEquivalentTo(new[] { "", "", "", "", "", "", "" });
            parsed.FormatArgs.Should().BeEquivalentTo(new object[] { 123, "abc", 456 });
        }

        [Fact]
        public void CanParseMoreThanThreePlaceholders()
        {
            var parsed = new InterpolatedString(() => $"aa{123}bb{"abc"}cc{456}dd{"def"}ee");

            parsed.FormatString.Should().Be("aa{0}bb{1}cc{2}dd{3}ee");
            parsed.FormatParts.Should().BeEquivalentTo(new[] { "aa", "", "bb", "", "cc", "", "dd", "", "ee" });
            parsed.FormatArgs.Should().BeEquivalentTo(new object[] { 123, "abc", 456, "def" });
        }
    }
}
