using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CardPoolTests
{
    [Test]
    public void Create_MakesSevenCards_WithOneShapeThreeAndOthersTwo()
    {
        // 여러 시드로 반복해 3장인 손 모양이 바뀌어도 구성이 지켜지는지 확인
        for (int seed = 0; seed < 30; seed++)
        {
            var cards = CardPool.Create(new System.Random(seed));

            Assert.AreEqual(GameRules.CardPoolSize, cards.Count);

            var counts = cards.GroupBy(card => card.Shape).Select(group => group.Count()).OrderBy(count => count).ToArray();
            CollectionAssert.AreEqual(
                new[] { GameRules.OtherShapeCardCount, GameRules.OtherShapeCardCount, GameRules.TripleShapeCardCount },
                counts);
        }
    }

    [Test]
    public void Create_TripleShapeIsRandom_AllShapesAppear()
    {
        var tripleShapes = Enumerable.Range(0, 100)
            .Select(seed => CardPool.Create(new System.Random(seed))
                .GroupBy(card => card.Shape)
                .First(group => group.Count() == GameRules.TripleShapeCardCount)
                .Key)
            .Distinct()
            .ToList();

        Assert.AreEqual(3, tripleShapes.Count);
    }

    [Test]
    public void Create_WritesResultToLog()
    {
        LogAssert.Expect(LogType.Log, new Regex(@"^\[CardPool\] 카드풀 생성"));

        CardPool.Create(new System.Random(0));
    }
}
