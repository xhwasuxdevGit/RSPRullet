using System.Collections.Generic;
using UnityEngine;

// 카드 셔플 한 번 동안의 테이블 상태: 카드 더미, 양쪽 카드 조합 칸, 턴 플레이어.
// 시작 카드와 선 정하기(7-2장 F4), 뽑기·분배·턴 교대(F5), 카드별 정보 공개 상태(F9)를 담당한다.
// 선 정하기에서 비기면 카드풀부터 다시 만들어야 하므로, 카드 셔플마다 새로 만든다.
public class ShuffleTable
{
    private readonly CardDeck _deck;

    // 카드 조합 칸 (기획서 1장 용어). 받은 순서대로 시작 카드 → 첫 번째 판 → 두 번째 판 카드가 쌓인다
    private readonly List<Card> _playerSlots = new List<Card>();
    private readonly List<Card> _dealerSlots = new List<Card>();

    public Side TurnPlayer { get; private set; }

    // 턴 플레이어가 뽑아서 혼자 확인 중인 카드. 분배 전에만 값이 있다
    public Card DrawnCard { get; private set; }

    public int RemainingDeckCount => _deck.RemainingCount;

    public ShuffleTable(CardDeck deck)
    {
        _deck = deck;
    }

    public static Side Opponent(Side side)
    {
        return side == Side.Player ? Side.Dealer : Side.Player;
    }

    // 바깥(UI, 딜러 AI, 테스트)에서 조합 칸을 볼 때 쓴다. 읽기 전용이라 바깥에서는 카드를 넣거나 뺄 수 없다
    public IReadOnlyList<Card> GetCombinationSlots(Side side)
    {
        return side == Side.Player ? _playerSlots : _dealerSlots;
    }

    // 더미 맨 위부터 플레이어 → 딜러 순서로 1장씩, 각자 조합 칸 첫 칸에 앞면으로 둔다
    public void DealStartCards()
    {
        PlaceFaceUp(Side.Player, _deck.Draw());
        PlaceFaceUp(Side.Dealer, _deck.Draw());
    }

    // 시작 카드로 가위바위보를 해 이긴 쪽을 첫 번째 판 턴 플레이어로 정한다. 대미지는 없다.
    // 비기면 false를 돌려준다. 이때는 카드풀부터 새로 만들고, 아이템은 지급하지 않는다 (예외 E1).
    public bool DecideFirstTurnPlayer()
    {
        MatchResult playerResult = HandShapeRules.Compare(_playerSlots[0].Shape, _dealerSlots[0].Shape);
        if (playerResult == MatchResult.Draw)
        {
            Debug.Log("[ShuffleTable] 선 정하기 무승부: 카드 셔플부터 다시");
            return false;
        }

        TurnPlayer = playerResult == MatchResult.Win ? Side.Player : Side.Dealer;
        Debug.Log($"[ShuffleTable] 선 정하기: 첫 번째 판 턴 플레이어 {TurnPlayer}");
        return true;
    }

    // 시계 이월로 선 정하기를 건너뛸 때, 시계를 쓴 쪽을 첫 번째 판 턴 플레이어로 둔다 (예외 E2, S3 시계 이슈에서 연결)
    public void SetFirstTurnPlayerByClock(Side clockUser)
    {
        TurnPlayer = clockUser;
    }

    // 5-1 뽑기: 턴 플레이어만 맨 위 카드를 안다
    public Card DrawForTurnPlayer()
    {
        DrawnCard = _deck.Draw();
        MarkKnownBy(DrawnCard, TurnPlayer);
        return DrawnCard;
    }

    // 5-2 분배: 가진다면 뽑은 카드가 턴 플레이어 자리로, 준다면 상대 자리로 간다.
    // 남은 자리에는 더미의 다음 카드가 엎어진 채 들어가며, 그 카드는 아무도 모른다.
    public void Distribute(bool keep)
    {
        Side receiver = keep ? TurnPlayer : Opponent(TurnPlayer);
        AddToCombination(receiver, DrawnCard);
        AddToCombination(Opponent(receiver), _deck.Draw());
        DrawnCard = null;
    }

    // 판이 끝나면 다음 판 턴 플레이어를 정한다. 시계를 쓰면 유지하고, 아니면 상대에게 넘어간다
    public void PassTurn(bool clockUsed)
    {
        if (clockUsed)
        {
            return;
        }

        TurnPlayer = Opponent(TurnPlayer);
    }

    // 5-5 카드 공개: 이번 판에 양쪽 자리에 들어간 카드(각 조합 칸의 마지막 카드)를 양쪽 모두 알게 된다
    public void RevealMatchCards()
    {
        RevealToBoth(_playerSlots[_playerSlots.Count - 1]);
        RevealToBoth(_dealerSlots[_dealerSlots.Count - 1]);
    }

    // 정산 1단계: 더미에 남은 1장을 꺼내 양쪽 모두에게 공개한다.
    // 꺼낸 카드는 테이블 어디에도 두지 않으므로 그대로 버려진다
    public Card RevealRemainingCard()
    {
        Card remaining = _deck.Draw();
        RevealToBoth(remaining);
        return remaining;
    }

    // viewer가 내용을 아는 테이블 위 카드 목록 (양쪽 조합 칸 + 뽑아서 확인 중인 카드).
    // 딜러 판단은 GetKnownCards(Side.Dealer)로 받은 카드만 써야 한다 (컨셉 기획서 7장 치트 금지)
    public List<Card> GetKnownCards(Side viewer)
    {
        var knownCards = new List<Card>();
        foreach (Card card in GetTableCards())
        {
            if (IsKnownBy(card, viewer))
            {
                knownCards.Add(card);
            }
        }
        return knownCards;
    }

    // 손 모양별로 아직 공개되지 않은 카드 수 (항상 공개되는 정보).
    // 더미에 남은 카드와, 테이블에 있지만 양쪽 모두가 알지는 않는 카드를 센다
    public int GetUnrevealedCardCount(HandShape shape)
    {
        int count = _deck.GetCardCount(shape);
        foreach (Card card in GetTableCards())
        {
            bool isRevealed = card.KnownByPlayer && card.KnownByDealer;
            if (card.Shape == shape && !isRevealed)
            {
                count++;
            }
        }
        return count;
    }

    // 더미 밖으로 나와 테이블에 있는 카드: 양쪽 조합 칸과, 분배 전이면 뽑은 카드
    private IEnumerable<Card> GetTableCards()
    {
        foreach (Card card in _playerSlots)
        {
            yield return card;
        }

        foreach (Card card in _dealerSlots)
        {
            yield return card;
        }

        if (DrawnCard != null)
        {
            yield return DrawnCard;
        }
    }

    // side의 조합 칸 맨 뒤에 카드를 넣는다. 조합 칸을 바꾸는 곳은 이 메서드 하나뿐이다
    private void AddToCombination(Side side, Card card)
    {
        List<Card> slots = side == Side.Player ? _playerSlots : _dealerSlots;
        slots.Add(card);
    }

    private void PlaceFaceUp(Side side, Card card)
    {
        RevealToBoth(card);
        AddToCombination(side, card);
    }

    private static void RevealToBoth(Card card)
    {
        card.KnownByPlayer = true;
        card.KnownByDealer = true;
    }

    private static bool IsKnownBy(Card card, Side side)
    {
        return side == Side.Player ? card.KnownByPlayer : card.KnownByDealer;
    }

    private static void MarkKnownBy(Card card, Side side)
    {
        if (side == Side.Player)
        {
            card.KnownByPlayer = true;
            return;
        }

        card.KnownByDealer = true;
    }
}
