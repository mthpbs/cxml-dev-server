using System.Text.RegularExpressions;

namespace Cxml.Protocol.Tests;

public class TimestampAndIdTests
{
    [Fact]
    public void Format_UsesNumericOffset_NeverZ()
    {
        var utc = CxmlTimestamp.Format(new DateTimeOffset(2024, 5, 1, 10, 0, 0, TimeSpan.Zero));
        var east = CxmlTimestamp.Format(new DateTimeOffset(2024, 5, 1, 10, 0, 0, TimeSpan.FromHours(5.5)));
        var west = CxmlTimestamp.Format(new DateTimeOffset(2024, 5, 1, 10, 0, 0, TimeSpan.FromHours(-8)));

        Assert.Equal("2024-05-01T10:00:00+00:00", utc);
        Assert.Equal("2024-05-01T10:00:00+05:30", east);
        Assert.Equal("2024-05-01T10:00:00-08:00", west);
        Assert.All(new[] { utc, east, west }, s => Assert.DoesNotContain("Z", s));
    }

    [Fact]
    public void Now_MatchesCxmlPattern()
    {
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$", CxmlTimestamp.Now());
    }

    [Theory]
    [InlineData("2003-01-15T11:42:46-08:00", "2003-01-15T11:42:46-08:00")]
    [InlineData("2003-01-15T11:42:46-0800", "2003-01-15T11:42:46-08:00")]
    [InlineData("2003-01-15T11:42:46Z", "2003-01-15T11:42:46+00:00")]
    [InlineData("2003-01-15T11:42:46.123+01:00", "2003-01-15T11:42:46+01:00")]
    [InlineData("  2003-01-15T11:42:46+02:00 \n", "2003-01-15T11:42:46+02:00")]
    [InlineData("2003-01-15T08:42:46\n      -0800", "2003-01-15T08:42:46-08:00")]
    [InlineData("2003-01-15T11:42:46", "2003-01-15T11:42:46+00:00")]
    public void TryParse_IsTolerant(string input, string expected)
    {
        Assert.True(CxmlTimestamp.TryParse(input, out var value));
        Assert.Equal(expected, CxmlTimestamp.Format(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("yesterday-ish")]
    public void TryParse_RejectsGarbage(string? input)
    {
        Assert.False(CxmlTimestamp.TryParse(input, out _));
        Assert.Null(CxmlTimestamp.ParseOrNull(input));
    }

    [Fact]
    public void FormatThenParse_RoundTrips()
    {
        var source = new DateTimeOffset(2024, 12, 31, 23, 59, 58, TimeSpan.FromHours(-3));

        Assert.True(CxmlTimestamp.TryParse(CxmlTimestamp.Format(source), out var parsed));
        Assert.Equal(source, parsed);
        Assert.Equal(source.Offset, parsed.Offset);
    }

    [Fact]
    public void PayloadId_HasExpectedShape()
    {
        var id = PayloadIdGenerator.Generate(new DateTimeOffset(2024, 5, 1, 10, 0, 0, 123, TimeSpan.FromHours(2)), "My Host");

        Assert.Matches(new Regex(@"^20240501080000123\.\d+\.\d+@my-host$"), id);
        Assert.Contains($".{Environment.ProcessId}.", id);
    }

    [Fact]
    public void PayloadId_IsUnique()
    {
        var ids = Enumerable.Range(0, 500).Select(_ => PayloadIdGenerator.Generate()).ToHashSet();

        Assert.Equal(500, ids.Count);
        Assert.Matches(@"^\d{17}\.\d+\.\d+@\S+$", ids.First());
    }
}
