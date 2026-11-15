using NUnit.Framework;

public class HandShapeRulesTests
{
    [TestCase(HandShape.Scissors, HandShape.Scissors, MatchResult.Draw)]
    [TestCase(HandShape.Scissors, HandShape.Rock, MatchResult.Lose)]
    [TestCase(HandShape.Scissors, HandShape.Paper, MatchResult.Win)]
    [TestCase(HandShape.Rock, HandShape.Scissors, MatchResult.Win)]
    [TestCase(HandShape.Rock, HandShape.Rock, MatchResult.Draw)]
    [TestCase(HandShape.Rock, HandShape.Paper, MatchResult.Lose)]
    [TestCase(HandShape.Paper, HandShape.Scissors, MatchResult.Lose)]
    [TestCase(HandShape.Paper, HandShape.Rock, MatchResult.Win)]
    [TestCase(HandShape.Paper, HandShape.Paper, MatchResult.Draw)]
    public void Compare_ReturnsExpectedResult(HandShape mine, HandShape other, MatchResult expected)
    {
        Assert.AreEqual(expected, HandShapeRules.Compare(mine, other));
    }
}
