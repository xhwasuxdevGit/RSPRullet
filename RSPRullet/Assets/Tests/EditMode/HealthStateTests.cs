using NUnit.Framework;
using UnityEngine;

// 코어 규칙 상세 기획서 3장(한 판의 판정), 4-3·4-4장(정산 순서와 예시) 기준
public class HealthStateTests
{
    // 규칙 설정 기본값이 기획서 7-1-1장 값과 같다 (최대 체력 10, 기본 데미지 1, 커피 2배, 트리플 3, 풀세트 1)
    private RuleSettings _rules;

    [SetUp]
    public void SetUp()
    {
        _rules = ScriptableObject.CreateInstance<RuleSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_rules);
    }

    [Test]
    public void ApplyMatch_PlayerWins_DealerLosesMatchDamage()
    {
        var health = new HealthState(_rules, 3);

        MatchResult result = health.ApplyMatch(HandShape.Rock, HandShape.Scissors);

        Assert.AreEqual(MatchResult.Win, result);
        Assert.AreEqual(3, health.GetHealth(Side.Player));
        Assert.AreEqual(2, health.GetHealth(Side.Dealer));
    }

    [Test]
    public void ApplyMatch_PlayerLoses_PlayerLosesMatchDamage()
    {
        var health = new HealthState(_rules, 3);

        MatchResult result = health.ApplyMatch(HandShape.Scissors, HandShape.Rock);

        Assert.AreEqual(MatchResult.Lose, result);
        Assert.AreEqual(2, health.GetHealth(Side.Player));
        Assert.AreEqual(3, health.GetHealth(Side.Dealer));
    }

    [Test]
    public void ApplyMatch_Draw_NoChangeAndCoffeeIgnored()
    {
        var health = new HealthState(_rules, 3);

        MatchResult result = health.ApplyMatch(HandShape.Paper, HandShape.Paper, true, true);

        Assert.AreEqual(MatchResult.Draw, result);
        Assert.AreEqual(3, health.GetHealth(Side.Player));
        Assert.AreEqual(3, health.GetHealth(Side.Dealer));
    }

    [Test]
    public void ApplyMatch_WinnerUsedCoffee_DamageMultiplied()
    {
        var health = new HealthState(_rules, 3);

        health.ApplyMatch(HandShape.Scissors, HandShape.Rock, false, true);

        Assert.AreEqual(1, health.GetHealth(Side.Player));
    }

    [Test]
    public void ApplyMatch_LoserUsedCoffee_NoEffect()
    {
        var health = new HealthState(_rules, 3);

        health.ApplyMatch(HandShape.Scissors, HandShape.Rock, true, false);

        Assert.AreEqual(2, health.GetHealth(Side.Player));
    }

    [Test]
    public void ApplyMatch_DamageBelowZero_StopsAtZeroAndSignalsRoundEnd()
    {
        var health = new HealthState(_rules, 1);
        Assert.IsFalse(health.IsAnyoneAtZero);

        health.ApplyMatch(HandShape.Rock, HandShape.Scissors, true, false);

        Assert.AreEqual(0, health.GetHealth(Side.Dealer));
        Assert.IsTrue(health.IsAnyoneAtZero);
    }

    // 예시 ①: 딜러 3. 플레이어 공격형(딜러 -1), 딜러 회복형(딜러 +1) → 딜러 3
    [Test]
    public void ApplySettlement_Example1_AttackAndHealCancelOut()
    {
        var health = new HealthState(_rules, 3);

        bool isRoundOver = health.ApplySettlement(CombinationType.AttackFullSet, CombinationType.HealFullSet);

        Assert.AreEqual(3, health.GetHealth(Side.Dealer));
        Assert.AreEqual(3, health.GetHealth(Side.Player));
        Assert.IsFalse(isRoundOver);
    }

    // 예시 ②: 플레이어 1. 플레이어 회복형(+1), 딜러 트리플(-3) → 1 + (1 - 3) = -1 → 0, 플레이어 패배
    [Test]
    public void ApplySettlement_Example2_SummedBeforeApply_PlayerLoses()
    {
        var health = new HealthState(_rules, 1);

        bool isRoundOver = health.ApplySettlement(CombinationType.HealFullSet, CombinationType.Triple);

        Assert.AreEqual(0, health.GetHealth(Side.Player));
        Assert.AreEqual(1, health.GetHealth(Side.Dealer));
        Assert.IsTrue(isRoundOver);
    }

    // 예시 ③: 플레이어 1, 딜러 1. 서로 공격형 → 양쪽 0 → 둘 다 1로 두고 계속
    [Test]
    public void ApplySettlement_Example3_BothZero_ResetToOneAndContinue()
    {
        var health = new HealthState(_rules, 1);

        bool isRoundOver = health.ApplySettlement(CombinationType.AttackFullSet, CombinationType.AttackFullSet);

        Assert.AreEqual(1, health.GetHealth(Side.Player));
        Assert.AreEqual(1, health.GetHealth(Side.Dealer));
        Assert.IsFalse(isRoundOver);
    }

    // 예시 ④: 딜러 10. 딜러 회복형(+1), 플레이어 공격형(-1) → 10 + (1 - 1) = 10
    [Test]
    public void ApplySettlement_Example4_SumAtMaxHealth_StaysAtMax()
    {
        var health = new HealthState(_rules, 10);

        bool isRoundOver = health.ApplySettlement(CombinationType.AttackFullSet, CombinationType.HealFullSet);

        Assert.AreEqual(10, health.GetHealth(Side.Dealer));
        Assert.IsFalse(isRoundOver);
    }

    // 예외 E6: 최대 체력에서 회복하면 남는 회복은 버려진다
    [Test]
    public void ApplySettlement_HealAtMaxHealth_CappedAtMax()
    {
        var health = new HealthState(_rules, 10);

        health.ApplySettlement(CombinationType.HealFullSet, CombinationType.None);

        Assert.AreEqual(10, health.GetHealth(Side.Player));
    }
}
