using System.Collections.Generic;
using UnityEngine;

// 카드 셔플 한 번 동안의 테이블 상태: 카드 더미, 양쪽 카드 조합 칸, 턴 플레이어.
// 시작 카드와 선 정하기(7-2장 F4), 뽑기·분배·턴 교대(F5)를 담당한다.
// 선 정하기에서 비기면 카드풀부터 다시 만들어야 하므로, 카드 셔플마다 새로 만든다.
public class ShuffleTable
{
    private readonly CardDeck _deck;
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

    // 카드 조합 칸 (받은 순서: 시작 카드 → 첫 번째 판 → 두 번째 판)
    public IReadOnlyList<Card> GetSlots(Side side)
    {
        return SlotsOf(side);
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
        SlotsOf(receiver).Add(DrawnCard);
        SlotsOf(Opponent(receiver)).Add(_deck.Draw());
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

    private List<Card> SlotsOf(Side side)
    {
        return side == Side.Player ? _playerSlots : _dealerSlots;
    }

    private void PlaceFaceUp(Side side, Card card)
    {
        card.KnownByPlayer = true;
        card.KnownByDealer = true;
        SlotsOf(side).Add(card);
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
