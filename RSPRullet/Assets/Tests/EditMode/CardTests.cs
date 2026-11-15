using NUnit.Framework;

public class CardTests
{
    [Test]
    public void Constructor_SetsShape_AndNobodyKnows()
    {
        var card = new Card(HandShape.Rock);

        Assert.AreEqual(HandShape.Rock, card.Shape);
        Assert.IsFalse(card.KnownByPlayer);
        Assert.IsFalse(card.KnownByDealer);
    }

    [Test]
    public void KnownFlags_CanBeSetIndependently()
    {
        var card = new Card(HandShape.Paper);

        card.KnownByPlayer = true;

        Assert.IsTrue(card.KnownByPlayer);
        Assert.IsFalse(card.KnownByDealer);
    }

    [Test]
    public void Swap_MovesKnownInfoWithCard()
    {
        var playerSlot = new Card(HandShape.Scissors) { KnownByPlayer = true };
        var dealerSlot = new Card(HandShape.Rock) { KnownByDealer = true };

        (playerSlot, dealerSlot) = (dealerSlot, playerSlot);

        Assert.AreEqual(HandShape.Rock, playerSlot.Shape);
        Assert.IsFalse(playerSlot.KnownByPlayer);
        Assert.IsTrue(playerSlot.KnownByDealer);

        Assert.AreEqual(HandShape.Scissors, dealerSlot.Shape);
        Assert.IsTrue(dealerSlot.KnownByPlayer);
        Assert.IsFalse(dealerSlot.KnownByDealer);
    }
}
