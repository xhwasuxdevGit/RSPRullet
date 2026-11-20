using NUnit.Framework;

// 상단 정보 UI 문구 규칙 (UI·UX 화면 흐름 세부 기획서 5-1장 상단 정보 UI, RSP-43)
public class TopInfoRulesTests
{
    private const string ShuffleColor = "D9A626";
    private const string PlayerColor = "66D966";
    private const string DealerColor = "F2594D";

    // 카드 섞기 번호는 황금색, 턴 플레이어 이름은 플레이어면 초록색으로 감싼다
    [Test]
    public void FirstMatch_ShowsAllItemsInOrder()
    {
        string text = GetText(1, 1, GamePhase.Draw, 0, Side.Player);

        Assert.AreEqual("1라운드    |    <color=#D9A626>카드 섞기 1번째</color>    |    첫 번째 판    |    " +
            "턴 플레이어: <color=#66D966>플레이어</color>", text);
    }

    // 턴 플레이어가 딜러면 이름을 빨간색으로 감싼다
    [Test]
    public void SecondMatch_ShowsSecondMatchAndDealer()
    {
        string text = GetText(2, 3, GamePhase.Distribute, 1, Side.Dealer);

        Assert.AreEqual("2라운드    |    <color=#D9A626>카드 섞기 3번째</color>    |    두 번째 판    |    " +
            "턴 플레이어: <color=#F2594D>딜러</color>", text);
    }

    // 정산 중에는 판 이름 대신 "정산"을 보여 주고, 턴 플레이어는 보여 주지 않는다
    [TestCase(Side.Player)]
    [TestCase(Side.Dealer)]
    public void Settlement_ShowsSettlement_WithoutTurnPlayer(Side turnPlayer)
    {
        string text = GetText(1, 2, GamePhase.Settlement, 1, turnPlayer);

        Assert.AreEqual("1라운드    |    <color=#D9A626>카드 섞기 2번째</color>    |    정산", text);
    }

    // 시계를 쓴 쪽을 강조색 태그로 감싸 상단 정보 뒤에 붙인다 (UI·UX 화면 흐름 세부 기획서 5-2장 시계 사용 표시)
    [TestCase(Side.Player, "플레이어가 시계를 사용함")]
    [TestCase(Side.Dealer, "딜러가 시계를 사용함")]
    public void AppendClockText_AddsColoredClockUser(Side clockUser, string expectedClockText)
    {
        string text = TopInfoRules.AppendClockText("2라운드", clockUser, "FFCC4D");

        Assert.AreEqual($"2라운드    |    <color=#FFCC4D>{expectedClockText}</color>", text);
    }

    private static string GetText(int roundNumber, int shuffleCount, GamePhase phase, int matchIndex, Side turnPlayer)
    {
        return TopInfoRules.GetText(roundNumber, shuffleCount, phase, matchIndex, turnPlayer, ShuffleColor, PlayerColor, DealerColor);
    }
}
