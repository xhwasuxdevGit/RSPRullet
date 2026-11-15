using System.Linq;
using NUnit.Framework;

public class ShuffleTableTests
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
    private static ShuffleTable MakeTable(params HandShape[] shapesFromTop)
    {
        var cards = shapesFromTop.Select(shape => new Card(shape));
        return new ShuffleTable(new CardDeck(cards, new KeepOrderRandom()));
    }

    // 7장 더미: 시작 카드(플레이어 가위, 딜러 보) 뒤로 바위 · 가위 · 보 · 바위 · 가위
    private static ShuffleTable MakeTableWithPlayerFirst()
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

    private static void AssertUnrevealed(ShuffleTable table, int scissors, int rock, int paper)
    {
        Assert.AreEqual(scissors, table.GetUnrevealedCardCount(HandShape.Scissors));
        Assert.AreEqual(rock, table.GetUnrevealedCardCount(HandShape.Rock));
        Assert.AreEqual(paper, table.GetUnrevealedCardCount(HandShape.Paper));
    }
}
