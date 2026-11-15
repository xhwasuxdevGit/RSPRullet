using System.Linq;
using NUnit.Framework;

public class RandomDealerTests
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

        bool distributed = new RandomDealer(new System.Random(1)).TryDistribute(table);

        Assert.IsFalse(distributed);
        Assert.AreSame(drawn, table.DrawnCard);
        Assert.AreEqual(1, table.GetCombinationSlots(Side.Player).Count);
        Assert.AreEqual(1, table.GetCombinationSlots(Side.Dealer).Count);
    }

    [Test]
    public void TryDistribute_BeforeDraw_DoesNothing()
    {
        var table = MakeTable(Side.Dealer);

        bool distributed = new RandomDealer(new System.Random(1)).TryDistribute(table);

        Assert.IsFalse(distributed);
        Assert.AreEqual(5, table.RemainingDeckCount);
    }

    [Test]
    public void TryDistribute_DealerTurn_DistributesDrawnCard()
    {
        var table = MakeTable(Side.Dealer);
        Card drawn = table.DrawForTurnPlayer();

        bool distributed = new RandomDealer(new System.Random(1)).TryDistribute(table);

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
            new RandomDealer(new System.Random(seed)).TryDistribute(table);
            return table.GetCombinationSlots(Side.Dealer)[1] == drawn;
        }).ToList();

        Assert.Contains(true, keptResults);
        Assert.Contains(false, keptResults);
    }
}
