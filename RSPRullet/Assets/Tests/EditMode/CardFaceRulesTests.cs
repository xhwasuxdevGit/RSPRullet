using NUnit.Framework;

// 카드 표시 규칙 (UI·UX 화면 흐름 세부 기획서 4-1장 기본 원칙, 4-2장 표시 범위, 4-4장 교환 표시)
public class CardFaceRulesTests
{
    // UI·UX 화면 흐름 세부 기획서 4-1장 표의 네 경우: 앞·뒷면 = 플레이어가 앎, 눈 표시 = 딜러가 앎
    [TestCase(true, true, true, true)]
    [TestCase(true, false, true, false)]
    [TestCase(false, true, false, true)]
    [TestCase(false, false, false, false)]
    public void BeforeJudgment_FaceFollowsPlayerAndMarkerFollowsDealer(bool knownByPlayer, bool knownByDealer, bool expectedFront, bool expectedMarker)
    {
        Card card = CreateCard(knownByPlayer, knownByDealer);

        Assert.AreEqual(expectedFront, CardFaceRules.ShowsFront(card));
        Assert.AreEqual(expectedMarker, CardFaceRules.ShowsKnownMarker(card, GamePhase.ItemTurnPlayer));
    }

    // 판-03처럼 딜러가 뽑아 확인 중인 카드에도 붙인다 (UI·UX 화면 흐름 세부 기획서 4-2장)
    [TestCase(GamePhase.Draw)]
    [TestCase(GamePhase.Distribute)]
    [TestCase(GamePhase.Reveal)]
    public void KnownMarker_ShownBeforeJudgment(GamePhase phase)
    {
        Assert.IsTrue(CardFaceRules.ShowsKnownMarker(CreateCard(false, true), phase));
    }

    [TestCase(GamePhase.Judge)]
    [TestCase(GamePhase.ShowResult)]
    [TestCase(GamePhase.Settlement)]
    public void KnownMarker_HiddenFromJudgment(GamePhase phase)
    {
        Assert.IsFalse(CardFaceRules.ShowsKnownMarker(CreateCard(true, true), phase));
    }

    [TestCase(true, GamePhase.ItemOpponent, true)]
    [TestCase(false, GamePhase.ItemOpponent, false)]
    [TestCase(true, GamePhase.ShowResult, false)]
    // 다음 판 분배 전까지 교환 여부에 앞 판 값이 남아 있어도 뽑기·분배 중에는 켜지 않는다
    [TestCase(true, GamePhase.Draw, false)]
    [TestCase(true, GamePhase.Distribute, false)]
    public void SwapMarker_ShownOnlyWhenSwappedBeforeJudgment(bool isSwapped, GamePhase phase, bool expected)
    {
        Assert.AreEqual(expected, CardFaceRules.ShowsSwapMarker(isSwapped, phase));
    }

    [TestCase(GamePhase.Draw, false)]
    [TestCase(GamePhase.Distribute, false)]
    [TestCase(GamePhase.ItemTurnPlayer, true)]
    [TestCase(GamePhase.ShowResult, true)]
    [TestCase(GamePhase.Settlement, false)]
    public void SlotCards_PresentFromDistributionToShowResult(GamePhase phase, bool expected)
    {
        Assert.AreEqual(expected, CardFaceRules.HasSlotCards(phase));
    }

    // 공개되면 양쪽 모두 아는 카드가 되어 앞면이 되고, 판정 전이면 눈 표시가 붙는다
    [Test]
    public void RevealedCard_ShowsFrontAndMarkerBeforeJudgment()
    {
        Card card = CreateCard(false, false);
        card.KnownByPlayer = true;
        card.KnownByDealer = true;

        Assert.IsTrue(CardFaceRules.ShowsFront(card));
        Assert.IsTrue(CardFaceRules.ShowsKnownMarker(card, GamePhase.Reveal));
    }

    private static Card CreateCard(bool knownByPlayer, bool knownByDealer)
    {
        return new Card(HandShape.Rock) { KnownByPlayer = knownByPlayer, KnownByDealer = knownByDealer };
    }
}
