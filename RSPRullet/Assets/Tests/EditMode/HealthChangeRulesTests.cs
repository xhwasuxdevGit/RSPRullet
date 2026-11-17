using NUnit.Framework;

// 체력 변화량 표기 규칙 (UI·UX 화면 흐름 세부 기획서 5-7장 체력)
public class HealthChangeRulesTests
{
    [TestCase(-1, true)]
    [TestCase(1, true)]
    [TestCase(-3, true)]
    [TestCase(0, false)]
    public void IsShown_OnlyWhenHealthChanged(int change, bool expected)
    {
        Assert.AreEqual(expected, HealthChangeRules.IsShown(change));
    }

    // 회복은 초록, 감소는 빨강
    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(-1, false)]
    [TestCase(-2, false)]
    public void IsHeal_TrueOnlyForIncrease(int change, bool expected)
    {
        Assert.AreEqual(expected, HealthChangeRules.IsHeal(change));
    }

    // 회복은 +를 붙이고, 감소는 -가 그대로 붙는다
    [TestCase(1, "+1")]
    [TestCase(2, "+2")]
    [TestCase(-1, "-1")]
    [TestCase(-2, "-2")]
    public void ChangeText_HasSign(int change, string expected)
    {
        Assert.AreEqual(expected, HealthChangeRules.GetChangeText(change));
    }
}
