using NUnit.Framework;

// 상단 정보 UI 문구 규칙 (UI·UX 화면 흐름 세부 기획서 5-1장 상단 정보 UI, RSP-43)
public class TopInfoRulesTests
{
    [Test]
    public void FirstMatch_ShowsAllItemsInOrder()
    {
        string text = TopInfoRules.GetText(1, 1, GamePhase.Draw, 0, Side.Player);

        Assert.AreEqual("1라운드    |    카드 섞기 1번째    |    첫 번째 판    |    턴 플레이어: 플레이어", text);
    }

    [Test]
    public void SecondMatch_ShowsSecondMatchAndDealer()
    {
        string text = TopInfoRules.GetText(2, 3, GamePhase.Distribute, 1, Side.Dealer);

        Assert.AreEqual("2라운드    |    카드 섞기 3번째    |    두 번째 판    |    턴 플레이어: 딜러", text);
    }

    // 정산 중에는 판 이름 대신 "정산"을 보여 주고, 턴 플레이어는 보여 주지 않는다
    [TestCase(Side.Player)]
    [TestCase(Side.Dealer)]
    public void Settlement_ShowsSettlement_WithoutTurnPlayer(Side turnPlayer)
    {
        string text = TopInfoRules.GetText(1, 2, GamePhase.Settlement, 1, turnPlayer);

        Assert.AreEqual("1라운드    |    카드 섞기 2번째    |    정산", text);
    }
}
