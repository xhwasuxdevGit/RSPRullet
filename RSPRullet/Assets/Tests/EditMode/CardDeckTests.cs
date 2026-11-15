using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class CardDeckTests
{
    private static List<Card> MakePool()
    {
        var shapes = new[]
        {
            HandShape.Scissors, HandShape.Scissors, HandShape.Scissors,
            HandShape.Rock, HandShape.Rock,
            HandShape.Paper, HandShape.Paper
        };
        return shapes.Select(shape => new Card(shape)).ToList();
    }

    private static List<Card> DrawAll(CardDeck deck)
    {
        var drawn = new List<Card>();
        while (deck.RemainingCount > 0)
        {
            drawn.Add(deck.Draw());
        }
        return drawn;
    }

    [Test]
    public void Constructor_KeepsAllSevenCards()
    {
        var pool = MakePool();
        var deck = new CardDeck(pool, new System.Random(1));

        Assert.AreEqual(7, deck.RemainingCount);
        CollectionAssert.AreEquivalent(pool, DrawAll(deck));
    }

    [Test]
    public void Draw_TakesOneCard_AndReducesRemainingCount()
    {
        var deck = new CardDeck(MakePool(), new System.Random(1));

        deck.Draw();
        deck.Draw();

        Assert.AreEqual(5, deck.RemainingCount);
    }

    [Test]
    public void SameSeed_GivesSameOrder()
    {
        var first = DrawAll(new CardDeck(MakePool(), new System.Random(42))).Select(card => card.Shape).ToList();
        var second = DrawAll(new CardDeck(MakePool(), new System.Random(42))).Select(card => card.Shape).ToList();

        CollectionAssert.AreEqual(first, second);
    }

    [Test]
    public void DifferentSeeds_ShuffleOrder()
    {
        // 원래 순서와 다른 순서가 한 번이라도 나오면 섞인 것으로 본다
        var original = MakePool().Select(card => card.Shape).ToList();

        bool shuffled = Enumerable.Range(0, 20).Any(seed =>
            !DrawAll(new CardDeck(MakePool(), new System.Random(seed))).Select(card => card.Shape).SequenceEqual(original));

        Assert.IsTrue(shuffled);
    }
}
