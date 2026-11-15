using System.Collections.Generic;

// 섞인 뒤 아직 나눠주지 않은 카드 더미 (코어 규칙 상세 기획서 7-2장 F2).
// 같은 시드의 난수를 넘기면 같은 순서가 나오므로 테스트에서 상황을 재현할 수 있다.
public class CardDeck
{
    // 0번 칸을 더미의 맨 위로 본다
    private readonly List<Card> _cards;

    public int RemainingCount => _cards.Count;

    public CardDeck(IEnumerable<Card> cards, System.Random random)
    {
        _cards = new List<Card>(cards);
        Shuffle(_cards, random);
    }

    // 맨 위 카드를 한 장 꺼낸다
    public Card Draw()
    {
        Card top = _cards[0];
        _cards.RemoveAt(0);
        return top;
    }

    // 피셔-예이츠 셔플: 뒤 칸부터 아직 확정되지 않은 칸 중 하나와 맞바꿔, 모든 순서가 같은 확률로 나온다
    private static void Shuffle(List<Card> cards, System.Random random)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
