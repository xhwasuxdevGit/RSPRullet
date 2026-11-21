using NUnit.Framework;

// 딜러 고민 중 말풍선 표시 규칙 (UI·UX 화면 흐름 세부 기획서 3-1장 판-03, RSP-84)
public class DealerSpeechBubbleRulesTests
{
    // 1라운드는 튜토리얼 안내를 띄우므로 딜러가 고민 중이어도 말풍선을 보여 주지 않는다
    [Test]
    public void FirstRound_WaitingForDealer_HidesBubble()
    {
        Assert.IsFalse(DealerSpeechBubbleRules.IsThinkingShown(true, 1));
    }

    // 2·3라운드는 딜러가 고민 중이면 말풍선을 보여 준다
    [TestCase(2)]
    [TestCase(3)]
    public void LaterRounds_WaitingForDealer_ShowsBubble(int roundNumber)
    {
        Assert.IsTrue(DealerSpeechBubbleRules.IsThinkingShown(true, roundNumber));
    }

    // 딜러가 고민 중이 아니면 어느 라운드든 말풍선을 보여 주지 않는다
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void NotWaitingForDealer_HidesBubble(int roundNumber)
    {
        Assert.IsFalse(DealerSpeechBubbleRules.IsThinkingShown(false, roundNumber));
    }
}
