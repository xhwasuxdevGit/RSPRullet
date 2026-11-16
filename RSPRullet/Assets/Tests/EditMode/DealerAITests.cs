using System.Linq;
using NUnit.Framework;

public class DealerAITests
{
    // 시작 카드를 나눈 뒤 firstTurnPlayer를 첫 번째 판 턴 플레이어로 둔 테이블
    private static ShuffleTable MakeTable(Side firstTurnPlayer)
    {
        var table = new ShuffleTable(new CardDeck(CardPool.Create(new System.Random(1)), new System.Random(1)));
        table.DealStartCards();
        table.SetFirstTurnPlayerByClock(firstTurnPlayer);
        return table;
    }

    [Test]
    public void TryDistribute_PlayerTurn_DoesNothing()
    {
        var table = MakeTable(Side.Player);
        Card drawn = table.DrawForTurnPlayer();

        bool distributed = new DealerAI(new System.Random(1)).TryDistribute(table, 0);

        Assert.IsFalse(distributed);
        Assert.AreSame(drawn, table.DrawnCard);
        Assert.AreEqual(1, table.GetCombinationSlots(Side.Player).Count);
        Assert.AreEqual(1, table.GetCombinationSlots(Side.Dealer).Count);
    }

    [Test]
    public void TryDistribute_BeforeDraw_DoesNothing()
    {
        var table = MakeTable(Side.Dealer);

        bool distributed = new DealerAI(new System.Random(1)).TryDistribute(table, 0);

        Assert.IsFalse(distributed);
        Assert.AreEqual(5, table.RemainingDeckCount);
    }

    [Test]
    public void TryDistribute_DealerTurn_DistributesDrawnCard()
    {
        var table = MakeTable(Side.Dealer);
        Card drawn = table.DrawForTurnPlayer();

        bool distributed = new DealerAI(new System.Random(1)).TryDistribute(table, 0);

        Assert.IsTrue(distributed);
        Assert.IsNull(table.DrawnCard);
        Assert.AreEqual(2, table.GetCombinationSlots(Side.Player).Count);
        Assert.AreEqual(2, table.GetCombinationSlots(Side.Dealer).Count);
        bool kept = table.GetCombinationSlots(Side.Dealer)[1] == drawn;
        bool given = table.GetCombinationSlots(Side.Player)[1] == drawn;
        Assert.IsTrue(kept ^ given);
    }

    [Test]
    public void TryDistribute_ChoosesBothKeepAndGive_AcrossSeeds()
    {
        // 시드 20개 중 가진다와 준다가 모두 한 번 이상 나오면 무작위로 고르는 것으로 본다
        var keptResults = Enumerable.Range(0, 20).Select(seed =>
        {
            var table = MakeTable(Side.Dealer);
            Card drawn = table.DrawForTurnPlayer();
            new DealerAI(new System.Random(seed)).TryDistribute(table, 0);
            return table.GetCombinationSlots(Side.Dealer)[1] == drawn;
        }).ToList();

        Assert.Contains(true, keptResults);
        Assert.Contains(false, keptResults);
    }

    // 1번째 판: 플레이어가 뽑아 가진다 → 2번째 판: 딜러가 뽑은 상태.
    // 플레이어가 가진 카드(플레이어만 앎)와 딜러 자리에 엎어진 카드(아무도 모름)는 딜러가 모르는 카드다
    private static ShuffleTable MakeSecondMatchDealerDrawn()
    {
        var table = MakeTable(Side.Player);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.PassTurn(clockUsed: false);
        table.DrawForTurnPlayer();
        return table;
    }

    [Test]
    public void DealerKnownInfo_UnknownSlotCards_AreHidden()
    {
        var table = MakeSecondMatchDealerDrawn();

        var knownInfo = new DealerKnownInfo(table, matchIndex: 1);

        // 시작 카드는 앞면이라 딜러도 알고, 1번째 판에 들어간 카드는 아직 공개 전이라 모른다
        Assert.AreEqual(table.GetCombinationSlots(Side.Player)[0].Shape, knownInfo.GetSlotShapes(Side.Player)[0]);
        Assert.AreEqual(table.GetCombinationSlots(Side.Dealer)[0].Shape, knownInfo.GetSlotShapes(Side.Dealer)[0]);
        Assert.IsNull(knownInfo.GetSlotShapes(Side.Player)[1]);
        Assert.IsNull(knownInfo.GetSlotShapes(Side.Dealer)[1]);
    }

    [Test]
    public void DealerKnownInfo_HasDrawnShapeMatchIndexAndPoolComposition()
    {
        var table = MakeSecondMatchDealerDrawn();

        var knownInfo = new DealerKnownInfo(table, matchIndex: 1);

        Assert.AreEqual(table.DrawnCard.Shape, knownInfo.DrawnShape);
        Assert.AreEqual(1, knownInfo.MatchIndex);
        var poolCounts = new[] { HandShape.Scissors, HandShape.Rock, HandShape.Paper }.Select(knownInfo.GetPoolCardCount).ToList();
        Assert.AreEqual(GameRules.CardPoolSize, poolCounts.Sum());
        Assert.AreEqual(1, poolCounts.Count(count => count == GameRules.TripleShapeCardCount));
    }
}
