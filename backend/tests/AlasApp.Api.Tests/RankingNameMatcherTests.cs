using AlasApp.Application.Rankings;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class RankingNameMatcherTests
{
    // Names as SurfScores stores them in RankingSnapshotEntries.
    private static readonly RankingNameMatcher Matcher = new(
    [
        ("Constanza Soto", 1),
        ("Viviana Margarita Araya Riquelme", 2),
        ("julian schweizer", 3),
        ("Emilian González", 5),
        ("Gino Perez", 6),
        ("Ana Perez", 7),
    ]);

    [Theory]
    [InlineData("Constanza", "Soto", 1)]
    [InlineData("JULIAN", "Schweizer", 3)]
    [InlineData("Emilian", "Gonzalez", 5)]
    [InlineData("Soto", "Constanza", 1)]
    [InlineData("Viviana", "Araya", 2)]
    [InlineData("Viviana Margarita", "Araya Riquelme", 2)]
    [InlineData("Gino", "Pérez Rojas", 6)]
    public void FindPosition_MatchesIgnoringCaseAccentsOrderAndExtraNames(string firstName, string lastName, int expected)
    {
        Assert.Equal(expected, Matcher.FindPosition(firstName, lastName));
    }

    [Theory]
    [InlineData("Carlos", "Soto")]
    [InlineData("Perez", "")]
    [InlineData("", "")]
    public void FindPosition_ReturnsNullWhenNotRankedOrAmbiguous(string firstName, string lastName)
    {
        Assert.Null(Matcher.FindPosition(firstName, lastName));
    }
}
