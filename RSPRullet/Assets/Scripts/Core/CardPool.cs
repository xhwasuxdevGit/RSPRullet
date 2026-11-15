using System.Collections.Generic;
using UnityEngine;

// 카드 셔플마다 쓰는 카드풀 생성 (코어 규칙 상세 기획서 7-2장 F1).
public static class CardPool
{
    private static readonly HandShape[] AllShapes = { HandShape.Scissors, HandShape.Rock, HandShape.Paper };

    // 3장이 될 손 모양을 무작위로 정하고, 나머지 두 손 모양은 2장씩 넣어 7장을 만든다
    public static List<Card> Create(System.Random random)
    {
        HandShape tripleShape = AllShapes[random.Next(AllShapes.Length)];
        var cards = new List<Card>(GameRules.CardPoolSize);

        foreach (HandShape shape in AllShapes)
        {
            int count = shape == tripleShape ? GameRules.TripleShapeCardCount : GameRules.OtherShapeCardCount;
            for (int i = 0; i < count; i++)
            {
                cards.Add(new Card(shape));
            }
        }

        Debug.Log($"[CardPool] 카드풀 생성: 3장인 손 모양 {tripleShape}, 총 {cards.Count}장");
        return cards;
    }
}
