using NUnit.Framework;

// 단계별 버튼과 입력 잠금 규칙 (UI·UX 화면 흐름 세부 기획서 6-1장 단계별 버튼, 6-2장 입력 잠금)
public class PhaseButtonRulesTests
{
    [TestCase(Side.Player, PhaseButton.DrawCard)]
    [TestCase(Side.Dealer, PhaseButton.None)]
    public void Draw_OnlyPlayerTurnShowsDrawCard(Side turnPlayer, PhaseButton expected)
    {
        Assert.AreEqual(expected, GetButtons(GamePhase.Draw, turnPlayer));
    }

    [TestCase(Side.Player, PhaseButton.Keep | PhaseButton.Give)]
    [TestCase(Side.Dealer, PhaseButton.None)]
    public void Distribute_OnlyPlayerTurnShowsKeepAndGive(Side turnPlayer, PhaseButton expected)
    {
        Assert.AreEqual(expected, GetButtons(GamePhase.Distribute, turnPlayer));
    }

    // 턴 플레이어 아이템 단계는 턴 플레이어가, 상대 아이템 단계는 상대가 입력한다
    [TestCase(GamePhase.ItemTurnPlayer, Side.Player, true, PhaseButton.ItemDone)]
    [TestCase(GamePhase.ItemTurnPlayer, Side.Player, false, PhaseButton.NoItem)]
    [TestCase(GamePhase.ItemTurnPlayer, Side.Dealer, true, PhaseButton.None)]
    [TestCase(GamePhase.ItemOpponent, Side.Dealer, true, PhaseButton.ItemDone)]
    [TestCase(GamePhase.ItemOpponent, Side.Dealer, false, PhaseButton.NoItem)]
    [TestCase(GamePhase.ItemOpponent, Side.Player, true, PhaseButton.None)]
    public void ItemPhase_PlayerSeesItemDoneOrNoItem(GamePhase phase, Side turnPlayer, bool hasUsableItem, PhaseButton expected)
    {
        PhaseButton buttons = PhaseButtonRules.GetButtons(phase, turnPlayer, hasUsableItem,
            isWaitingForMagnifierChoice: false, playerWonRound: false, isFinalRound: false);

        Assert.AreEqual(expected, buttons);
    }

    // 돋보기 선택 중에는 쓸 수 있는 아이템이 남아 있어도 바꾼다·그대로 둔다만 켠다
    [TestCase(GamePhase.ItemTurnPlayer, Side.Player, true)]
    [TestCase(GamePhase.ItemTurnPlayer, Side.Player, false)]
    [TestCase(GamePhase.ItemOpponent, Side.Dealer, true)]
    public void MagnifierChoice_ShowsExchangeAndKeepCardsOnly(GamePhase phase, Side turnPlayer, bool hasUsableItem)
    {
        PhaseButton buttons = PhaseButtonRules.GetButtons(phase, turnPlayer, hasUsableItem,
            isWaitingForMagnifierChoice: true, playerWonRound: false, isFinalRound: false);

        Assert.AreEqual(PhaseButton.Exchange | PhaseButton.KeepCards, buttons);
    }

    // 정산 결과 표시 중에는 버튼 없이 입력을 잠그고, 다음 카드 셔플 대기에서 [다음 카드 셔플]을 켠다
    [Test]
    public void Settlement_LocksInput_ThenWaitShowsNextShuffle()
    {
        PhaseButton settlementButtons = GetButtons(GamePhase.Settlement, Side.Player);
        Assert.AreEqual(PhaseButton.None, settlementButtons);
        Assert.IsTrue(PhaseButtonRules.IsInputLocked(settlementButtons));

        Assert.AreEqual(PhaseButton.NextShuffle, GetButtons(GamePhase.SettlementWait, Side.Player));
    }

    [TestCase(true, false, PhaseButton.NextRound)]
    [TestCase(false, false, PhaseButton.TakeMedicine)]
    [TestCase(true, true, PhaseButton.None)]
    [TestCase(false, true, PhaseButton.None)]
    public void RoundEnd_ShowsButtonByResultAndRound(bool playerWonRound, bool isFinalRound, PhaseButton expected)
    {
        PhaseButton buttons = PhaseButtonRules.GetButtons(GamePhase.RoundEnd, Side.Player, hasUsableItem: false,
            isWaitingForMagnifierChoice: false, playerWonRound, isFinalRound);

        Assert.AreEqual(expected, buttons);
    }

    // 마지막 라운드가 끝나면 팝업만 누를 수 있도록 규칙·설정 버튼까지 잠근다
    [TestCase(true)]
    [TestCase(false)]
    public void FinalRoundEnd_LocksInput(bool playerWonRound)
    {
        PhaseButton buttons = PhaseButtonRules.GetButtons(GamePhase.RoundEnd, Side.Player, hasUsableItem: false,
            isWaitingForMagnifierChoice: false, playerWonRound, isFinalRound: true);

        Assert.IsTrue(PhaseButtonRules.IsInputLocked(buttons));
    }

    // 판-07 승패 판정 표시를 포함한 자동 진행 단계는 버튼이 없다
    [TestCase(GamePhase.RoundStart)]
    [TestCase(GamePhase.CardShuffle)]
    [TestCase(GamePhase.StartCardDeal)]
    [TestCase(GamePhase.DecideFirst)]
    [TestCase(GamePhase.Reveal)]
    [TestCase(GamePhase.Judge)]
    [TestCase(GamePhase.ShowResult)]
    public void AutoPhase_HasNoButtons(GamePhase phase)
    {
        Assert.AreEqual(PhaseButton.None, GetButtons(phase, Side.Player));
    }

    [Test]
    public void IsInputLocked_TrueOnlyWhenNoButtons()
    {
        Assert.IsTrue(PhaseButtonRules.IsInputLocked(PhaseButton.None));
        Assert.IsFalse(PhaseButtonRules.IsInputLocked(PhaseButton.DrawCard));
        Assert.IsFalse(PhaseButtonRules.IsInputLocked(PhaseButton.Keep | PhaseButton.Give));
    }

    private static PhaseButton GetButtons(GamePhase phase, Side turnPlayer)
    {
        return PhaseButtonRules.GetButtons(phase, turnPlayer, hasUsableItem: false,
            isWaitingForMagnifierChoice: false, playerWonRound: false, isFinalRound: false);
    }
}
