using System;
using NUnit.Framework;

// 코어 규칙 상세 기획서 4-2장 경우의 수 표 기준
public class CombinationRulesTests
{
    [TestCase(HandShape.Scissors, HandShape.Scissors, HandShape.Scissors, CombinationType.Triple)]
    [TestCase(HandShape.Rock, HandShape.Rock, HandShape.Rock, CombinationType.Triple)]
    [TestCase(HandShape.Paper, HandShape.Paper, HandShape.Paper, CombinationType.Triple)]
    [TestCase(HandShape.Scissors, HandShape.Rock, HandShape.Paper, CombinationType.AttackFullSet)]
    [TestCase(HandShape.Rock, HandShape.Paper, HandShape.Scissors, CombinationType.AttackFullSet)]
    [TestCase(HandShape.Paper, HandShape.Scissors, HandShape.Rock, CombinationType.AttackFullSet)]
    [TestCase(HandShape.Scissors, HandShape.Paper, HandShape.Rock, CombinationType.HealFullSet)]
    [TestCase(HandShape.Rock, HandShape.Scissors, HandShape.Paper, CombinationType.HealFullSet)]
    [TestCase(HandShape.Paper, HandShape.Rock, HandShape.Scissors, CombinationType.HealFullSet)]
    [TestCase(HandShape.Scissors, HandShape.Scissors, HandShape.Rock, CombinationType.None)]
    [TestCase(HandShape.Rock, HandShape.Paper, HandShape.Rock, CombinationType.None)]
    [TestCase(HandShape.Paper, HandShape.Scissors, HandShape.Scissors, CombinationType.None)]
    public void Judge_ReturnsExpectedPattern(HandShape first, HandShape second, HandShape third, CombinationType expected)
    {
        Assert.AreEqual(expected, CombinationRules.Judge(first, second, third));
    }

    [Test]
    public void Judge_AllOrders_MatchCountTable()
    {
        int triple = 0, attack = 0, heal = 0, none = 0;
        var shapes = (HandShape[])Enum.GetValues(typeof(HandShape));

        foreach (HandShape first in shapes)
        foreach (HandShape second in shapes)
        foreach (HandShape third in shapes)
        {
            switch (CombinationRules.Judge(first, second, third))
            {
                case CombinationType.Triple: triple++; break;
                case CombinationType.AttackFullSet: attack++; break;
                case CombinationType.HealFullSet: heal++; break;
                case CombinationType.None: none++; break;
            }
        }

        Assert.AreEqual(3, triple);
        Assert.AreEqual(3, attack);
        Assert.AreEqual(3, heal);
        Assert.AreEqual(18, none);
    }
}
