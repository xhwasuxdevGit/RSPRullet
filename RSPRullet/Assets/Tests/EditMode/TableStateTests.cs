using System.Linq;
using NUnit.Framework;

public class TableStateTests
{
    // 셔플이 순서를 바꾸지 않도록 항상 마지막 칸을 고르는 난수.
    // 피셔-예이츠에서 j = i가 되어 넣은 순서 그대로 더미가 만들어진다
    private class KeepOrderRandom : System.Random
    {
        public override int Next(int maxValue)
        {
            return maxValue - 1;
        }
    }

    // 넣은 순서대로 맨 위부터 나오는 테이블을 만든다
    private static TableState MakeTable(params HandShape[] shapesFromTop)
    {
        var cards = shapesFromTop.Select(shape => new Card(shape));
        return new TableState(new CardDeck(cards, new KeepOrderRandom()));
    }

    // 7장 더미: 시작 카드(플레이어 가위, 딜러 보) 뒤로 바위 · 가위 · 보 · 바위 · 가위
    private static TableState MakeTableWithPlayerFirst()
    {
        var table = MakeTable(
            HandShape.Scissors, HandShape.Paper,
            HandShape.Rock, HandShape.Scissors, HandShape.Paper, HandShape.Rock, HandShape.Scissors);
        table.DealStartCards();
        table.DecideFirstTurnPlayer();
        return table;
    }

    // ---- RSP-23 시작 카드와 선 정하기 ----

    [Test]
    public void DealStartCards_PutsOneFaceUpCardInEachFirstSlot()
    {
        var table = MakeTable(HandShape.Rock, HandShape.Paper, HandShape.Scissors);

        table.DealStartCards();

        Assert.AreEqual(HandShape.Rock, table.GetCombinationSlots(Side.Player)[0].Shape);
        Assert.AreEqual(HandShape.Paper, table.GetCombinationSlots(Side.Dealer)[0].Shape);
        foreach (Card card in table.GetCombinationSlots(Side.Player).Concat(table.GetCombinationSlots(Side.Dealer)))
        {
            Assert.IsTrue(card.KnownByPlayer);
            Assert.IsTrue(card.KnownByDealer);
        }
        Assert.AreEqual(1, table.RemainingDeckCount);
    }

    [TestCase(HandShape.Rock, HandShape.Scissors, Side.Player)]
    [TestCase(HandShape.Rock, HandShape.Paper, Side.Dealer)]
    public void DecideFirstTurnPlayer_WinnerBecomesTurnPlayer(HandShape playerShape, HandShape dealerShape, Side expected)
    {
        var table = MakeTable(playerShape, dealerShape);
        table.DealStartCards();

        bool decided = table.DecideFirstTurnPlayer();

        Assert.IsTrue(decided);
        Assert.AreEqual(expected, table.TurnPlayer);
    }

    [Test]
    public void DecideFirstTurnPlayer_Draw_ReturnsFalse()
    {
        var table = MakeTable(HandShape.Paper, HandShape.Paper);
        table.DealStartCards();

        Assert.IsFalse(table.DecideFirstTurnPlayer());
    }

    [Test]
    public void SetFirstTurnPlayerByClock_SetsClockUser()
    {
        var table = MakeTable(HandShape.Paper, HandShape.Paper);
        table.DealStartCards();

        table.SetFirstTurnPlayerByClock(Side.Dealer);

        Assert.AreEqual(Side.Dealer, table.TurnPlayer);
    }

    // ---- RSP-24 뽑기와 분배 ----

    [Test]
    public void DrawForTurnPlayer_OnlyTurnPlayerKnowsTopCard()
    {
        var table = MakeTableWithPlayerFirst();

        Card drawn = table.DrawForTurnPlayer();

        Assert.AreEqual(HandShape.Rock, drawn.Shape);
        Assert.IsTrue(drawn.KnownByPlayer);
        Assert.IsFalse(drawn.KnownByDealer);
    }

    [Test]
    public void Distribute_Keep_DrawnCardToTurnPlayer_NextCardFaceDownToOpponent()
    {
        var table = MakeTableWithPlayerFirst();
        Card drawn = table.DrawForTurnPlayer();

        table.Distribute(keep: true);

        Card dealerCard = table.GetCombinationSlots(Side.Dealer)[1];
        Assert.AreSame(drawn, table.GetCombinationSlots(Side.Player)[1]);
        Assert.AreEqual(HandShape.Scissors, dealerCard.Shape);
        Assert.IsFalse(dealerCard.KnownByPlayer);
        Assert.IsFalse(dealerCard.KnownByDealer);
        Assert.IsNull(table.DrawnCard);
    }

    [Test]
    public void Distribute_Give_DrawnCardToOpponent_NextCardFaceDownToTurnPlayer()
    {
        var table = MakeTableWithPlayerFirst();
        Card drawn = table.DrawForTurnPlayer();

        table.Distribute(keep: false);

        Card playerCard = table.GetCombinationSlots(Side.Player)[1];
        Assert.AreSame(drawn, table.GetCombinationSlots(Side.Dealer)[1]);
        Assert.IsFalse(playerCard.KnownByPlayer);
        Assert.IsFalse(playerCard.KnownByDealer);
        // 받은 상대(딜러)는 뽑은 카드를 모른다
        Assert.IsFalse(drawn.KnownByDealer);
    }

    [TestCase(false, Side.Dealer)]
    [TestCase(true, Side.Player)]
    public void PassTurn_SwitchesUnlessClockUsed(bool clockUsed, Side expected)
    {
        var table = MakeTableWithPlayerFirst();

        table.PassTurn(clockUsed);

        Assert.AreEqual(expected, table.TurnPlayer);
    }

    [Test]
    public void TwoMatches_FillThreeSlotsEach_AndLeaveOneCard()
    {
        var table = MakeTableWithPlayerFirst();

        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        Assert.AreEqual(3, table.RemainingDeckCount);

        table.PassTurn(clockUsed: false);
        Card secondDrawn = table.DrawForTurnPlayer();
        table.Distribute(keep: false);

        // 두 번째 판은 딜러가 턴 플레이어이므로 딜러만 뽑은 카드를 알고, 준 카드는 플레이어 자리로 간다
        Assert.IsTrue(secondDrawn.KnownByDealer);
        Assert.IsFalse(secondDrawn.KnownByPlayer);
        Assert.AreSame(secondDrawn, table.GetCombinationSlots(Side.Player)[2]);
        Assert.AreEqual(3, table.GetCombinationSlots(Side.Player).Count);
        Assert.AreEqual(3, table.GetCombinationSlots(Side.Dealer).Count);
        Assert.AreEqual(1, table.RemainingDeckCount);
    }

    // ---- RSP-28 카드별 정보 공개 상태 ----

    [Test]
    public void RevealMatchCards_BothKnowBothMatchCards()
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);

        table.RevealMatchCards();

        foreach (Card card in new[] { table.GetCombinationSlots(Side.Player)[1], table.GetCombinationSlots(Side.Dealer)[1] })
        {
            Assert.IsTrue(card.KnownByPlayer);
            Assert.IsTrue(card.KnownByDealer);
        }
    }

    [Test]
    public void RevealRemainingCard_LastCardKnownByBoth_AndLeavesDeck()
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.PassTurn(clockUsed: false);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);

        Card remaining = table.RevealRemainingCard();

        Assert.AreEqual(HandShape.Scissors, remaining.Shape);
        Assert.IsTrue(remaining.KnownByPlayer);
        Assert.IsTrue(remaining.KnownByDealer);
        Assert.AreEqual(0, table.RemainingDeckCount);
    }

    [Test]
    public void GetKnownCards_EachSideSeesOnlyWhatItKnows()
    {
        var table = MakeTableWithPlayerFirst();
        Card drawn = table.DrawForTurnPlayer();

        // 플레이어가 뽑아 확인 중: 플레이어만 그 카드를 안다
        CollectionAssert.Contains(table.GetKnownCards(Side.Player), drawn);
        CollectionAssert.DoesNotContain(table.GetKnownCards(Side.Dealer), drawn);

        table.Distribute(keep: true);
        Card faceDown = table.GetCombinationSlots(Side.Dealer)[1];

        // 딜러는 양쪽 시작 카드만 안다. 자기 자리의 엎어진 카드도 모른다
        var dealerKnown = table.GetKnownCards(Side.Dealer);
        CollectionAssert.AreEquivalent(
            new[] { table.GetCombinationSlots(Side.Player)[0], table.GetCombinationSlots(Side.Dealer)[0] },
            dealerKnown);
        CollectionAssert.DoesNotContain(table.GetKnownCards(Side.Player), faceDown);

        table.RevealMatchCards();

        Assert.AreEqual(4, table.GetKnownCards(Side.Dealer).Count);
        Assert.AreEqual(4, table.GetKnownCards(Side.Player).Count);
    }

    [Test]
    public void GetUnrevealedCardCount_CountsDeckAndCardsNotKnownByBoth()
    {
        // 카드풀: 가위 3 · 바위 2 · 보 2. 시작 카드 가위·보가 공개된 상태
        var table = MakeTableWithPlayerFirst();
        AssertUnrevealed(table, scissors: 2, rock: 2, paper: 1);

        // 플레이어가 뽑아 가진 바위와 딜러 자리의 엎어진 가위는 아직 한쪽 또는 양쪽이 모르므로 미공개
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        AssertUnrevealed(table, scissors: 2, rock: 2, paper: 1);

        table.RevealMatchCards();
        AssertUnrevealed(table, scissors: 1, rock: 1, paper: 1);
    }

    private static void AssertUnrevealed(TableState table, int scissors, int rock, int paper)
    {
        Assert.AreEqual(scissors, table.GetUnrevealedCardCount(HandShape.Scissors));
        Assert.AreEqual(rock, table.GetUnrevealedCardCount(HandShape.Rock));
        Assert.AreEqual(paper, table.GetUnrevealedCardCount(HandShape.Paper));
    }

    // 카드풀 구성은 카드를 나누고 뽑아도 카드 셔플 시작 때 값(가위 3 · 바위 2 · 보 2) 그대로다
    [Test]
    public void GetPoolCardCount_KeepsShuffleStartComposition_AfterDraws()
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);

        Assert.AreEqual(3, table.GetPoolCardCount(HandShape.Scissors));
        Assert.AreEqual(2, table.GetPoolCardCount(HandShape.Rock));
        Assert.AreEqual(2, table.GetPoolCardCount(HandShape.Paper));
    }

    // ---- RSP-88 상태 변경 알림과 판 정보 ----

    // 카드 변경 알림: 시작 카드 1, 뽑기 1, 분배 1, 판 카드 공개 1, 남은 카드 공개 1. 턴 교대와 판 시작은 카드를 바꾸지 않아 알리지 않는다
    [Test]
    public void Changed_NotifiesOncePerCardOperation()
    {
        var table = MakeTable(
            HandShape.Scissors, HandShape.Paper,
            HandShape.Rock, HandShape.Scissors, HandShape.Paper, HandShape.Rock, HandShape.Scissors);
        int changedCount = 0;
        table.Changed += () => changedCount++;

        table.DealStartCards();
        Assert.AreEqual(1, changedCount);
        table.DecideFirstTurnPlayer();
        table.BeginMatch();
        table.DrawForTurnPlayer();
        Assert.AreEqual(2, changedCount);
        table.Distribute(keep: true);
        Assert.AreEqual(3, changedCount);
        table.RevealMatchCards();
        Assert.AreEqual(4, changedCount);
        table.PassTurn(clockUsed: false);
        Assert.AreEqual(4, changedCount);

        table.BeginMatch();
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.RevealMatchCards();
        table.RevealRemainingCard();
        Assert.AreEqual(8, changedCount);
    }

    // 판 카드 수: 첫 번째 판 5장 → 3장, 두 번째 판 3장 → 1장. 남은 카드를 공개해도 두 번째 판 값이 남는다
    [Test]
    public void MatchDeckCount_RecordedAtBeginMatch_KeptThroughSettlement()
    {
        var table = MakeTableWithPlayerFirst();

        table.BeginMatch();
        Assert.AreEqual(5, table.MatchStartDeckCount);
        Assert.AreEqual(3, table.MatchEndDeckCount);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.PassTurn(clockUsed: false);

        table.BeginMatch();
        table.DrawForTurnPlayer();
        table.Distribute(keep: false);
        table.RevealRemainingCard();

        Assert.AreEqual(3, table.MatchStartDeckCount);
        Assert.AreEqual(1, table.MatchEndDeckCount);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Distribute_RecordsLastDistributionKept(bool keep)
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();

        table.Distribute(keep);

        Assert.AreEqual(keep, table.LastDistributionKept);
    }

    // 시작 카드는 조합 칸 첫 카드, 이번 판 카드는 마지막 카드다
    [Test]
    public void GetStartCardAndMatchCard_ReturnFirstAndLastSlotCards()
    {
        var table = MakeTableWithPlayerFirst();
        Card drawn = table.DrawForTurnPlayer();
        table.Distribute(keep: true);

        Assert.AreEqual(HandShape.Scissors, table.GetStartCard(Side.Player).Shape);
        Assert.AreEqual(HandShape.Paper, table.GetStartCard(Side.Dealer).Shape);
        Assert.AreSame(drawn, table.GetMatchCard(Side.Player));
        Assert.AreSame(table.GetCombinationSlots(Side.Dealer)[1], table.GetMatchCard(Side.Dealer));
    }

    // 첫 번째 판 플레이어가 바위를 가지고, 두 번째 판 딜러가 보를 주면
    // 조합 칸은 플레이어 가위 → 바위 → 보, 딜러 보 → 가위 → 바위가 된다. 받은 순서대로 판정한다
    [Test]
    public void JudgeCombination_JudgesThreeSlotsInReceivedOrder()
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.PassTurn(clockUsed: false);
        table.DrawForTurnPlayer();
        table.Distribute(keep: false);

        Assert.AreEqual(CombinationRules.Judge(HandShape.Scissors, HandShape.Rock, HandShape.Paper), table.JudgeCombination(Side.Player));
        Assert.AreEqual(CombinationRules.Judge(HandShape.Paper, HandShape.Scissors, HandShape.Rock), table.JudgeCombination(Side.Dealer));
    }

    // ---- RSP-54 돋보기 ----

    // 플레이어가 턴 플레이어로 뽑은 바위를 가지면, 딜러 자리의 엎어진 가위를 돋보기로 본 플레이어만 알게 된다
    [Test]
    public void PeekOpponentMatchCard_OnlyUserLearnsOpponentCard()
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);

        Card peeked = table.PeekOpponentMatchCard(Side.Player);

        Assert.AreSame(table.GetMatchCard(Side.Dealer), peeked);
        Assert.IsTrue(peeked.KnownByPlayer);
        Assert.IsFalse(peeked.KnownByDealer);
    }

    // 코어 규칙 상세 기획서 5-3장 경우 A~D. 플레이어가 턴 플레이어이고, 돋보기 확인 뒤 교환한 결과의
    // 양쪽 자리 카드를 각각 플레이어·딜러가 아는지 확인한다. 아는 사람 정보는 카드를 따라 옮겨진다
    [TestCase(true, Side.Player, true, false, true, false)]    // A: 가진 뒤 턴 플레이어가 교환
    [TestCase(false, Side.Player, true, false, false, false)]  // B: 준 뒤 턴 플레이어가 교환
    [TestCase(true, Side.Dealer, false, false, true, true)]    // C: 가진 뒤 상대가 교환
    [TestCase(false, Side.Dealer, true, false, false, true)]   // D: 준 뒤 상대가 교환
    public void PeekAndSwap_KnownInfoMovesWithCards(bool keep, Side user,
        bool playerSlotKnownByPlayer, bool playerSlotKnownByDealer, bool dealerSlotKnownByPlayer, bool dealerSlotKnownByDealer)
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep);
        Card playerCardBefore = table.GetMatchCard(Side.Player);
        Card dealerCardBefore = table.GetMatchCard(Side.Dealer);

        table.PeekOpponentMatchCard(user);
        table.SwapMatchCards();

        Card playerCard = table.GetMatchCard(Side.Player);
        Card dealerCard = table.GetMatchCard(Side.Dealer);
        Assert.AreSame(dealerCardBefore, playerCard);
        Assert.AreSame(playerCardBefore, dealerCard);
        Assert.AreEqual(playerSlotKnownByPlayer, playerCard.KnownByPlayer);
        Assert.AreEqual(playerSlotKnownByDealer, playerCard.KnownByDealer);
        Assert.AreEqual(dealerSlotKnownByPlayer, dealerCard.KnownByPlayer);
        Assert.AreEqual(dealerSlotKnownByDealer, dealerCard.KnownByDealer);
    }

    // 상대가 다시 교환하면 카드가 원래 자리로 돌아오고 교환 표시 대상이 아니다 (아이템 세부 기획서 5장 I1·I2)
    [Test]
    public void SwapTwice_ReturnsCards_AndIsNotSwapped()
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        Card playerCardBefore = table.GetMatchCard(Side.Player);
        Assert.IsFalse(table.IsMatchCardSwapped);

        table.SwapMatchCards();
        Assert.IsTrue(table.IsMatchCardSwapped);

        table.SwapMatchCards();
        Assert.IsFalse(table.IsMatchCardSwapped);
        Assert.AreSame(playerCardBefore, table.GetMatchCard(Side.Player));
    }

    // 교환 여부는 판마다 분배 직후 위치를 기준으로 하므로, 다음 판 분배 뒤에는 다시 false다
    [Test]
    public void IsMatchCardSwapped_ResetsAtNextDistribute()
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.SwapMatchCards();
        table.PassTurn(clockUsed: false);

        table.DrawForTurnPlayer();
        table.Distribute(keep: true);

        Assert.IsFalse(table.IsMatchCardSwapped);
    }

    // 확인으로 새로 알게 된 정보가 있을 때와 교환할 때만 카드 변경을 알린다. 이미 알던 카드를 보면 알리지 않는다 (경우 B)
    [Test]
    public void PeekAndSwap_NotifyOnlyWhenCardInfoChanges()
    {
        var table = MakeTableWithPlayerFirst();
        table.DrawForTurnPlayer();
        table.Distribute(keep: false);
        int changedCount = 0;
        table.Changed += () => changedCount++;

        table.PeekOpponentMatchCard(Side.Player);
        Assert.AreEqual(0, changedCount);

        table.PeekOpponentMatchCard(Side.Dealer);
        Assert.AreEqual(1, changedCount);

        table.SwapMatchCards();
        Assert.AreEqual(2, changedCount);
    }
}
