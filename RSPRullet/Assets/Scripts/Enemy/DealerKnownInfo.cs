using System.Collections.Generic;

// 딜러가 아는 정보 (딜러 AI 세부 기획서 1장 용어). 딜러 판단은 이 묶음만 받는다.
// 딜러가 모르는 카드의 손 모양은 만들 때부터 담지 않아, 판단 코드가 실수로 볼 수 없다 (딜러 AI 세부 기획서 2장 치트 금지).
// 카드 분배 판단(딜러 AI 세부 기획서 7-2장 D1)과 아이템·교환 판단(딜러 AI 세부 기획서 7-2장 D2·D3)에 함께 쓴다.
// 보유 아이템·사용 기록·턴 플레이어처럼 항상 공개되는 정보는 담지 않고, 판단 메서드가 따로 받는다.
// 이번 판 카드는 조합 칸의 마지막 칸이라 GetSlotShapes()로 읽는다. 돋보기 확인·교환 뒤에는 새로 만들어야 바뀐 내용이 담긴다
public class DealerKnownInfo
{
    private readonly List<HandShape?> _playerSlotShapes = new List<HandShape?>();
    private readonly List<HandShape?> _dealerSlotShapes = new List<HandShape?>();
    private readonly Dictionary<HandShape, int> _poolCardCounts = new Dictionary<HandShape, int>();
    private readonly Dictionary<HandShape, int> _unrevealedCardCounts = new Dictionary<HandShape, int>();
    private readonly Dictionary<HandShape, int> _unknownCardCounts = new Dictionary<HandShape, int>();

    // 이번 카드 셔플에서 몇 번째 판인지 (0: 1번째 판, 1: 2번째 판)
    public int MatchIndex { get; }

    // 딜러가 뽑아서 확인 중인 카드의 손 모양. 분배 뒤(아이템 단계)에는 뽑은 카드가 없어 null이다.
    // 분배 판단은 딜러가 뽑은 뒤에만 부르므로 값이 있다
    public HandShape? DrawnShape { get; }

    public DealerKnownInfo(TableState table, int matchIndex)
    {
        MatchIndex = matchIndex;
        DrawnShape = table.DrawnCard != null ? table.DrawnCard.Shape : (HandShape?)null;
        CopyShapesKnownByDealer(table.GetCombinationSlots(Side.Player), _playerSlotShapes);
        CopyShapesKnownByDealer(table.GetCombinationSlots(Side.Dealer), _dealerSlotShapes);

        foreach (HandShape shape in System.Enum.GetValues(typeof(HandShape)))
        {
            _poolCardCounts[shape] = table.GetPoolCardCount(shape);
            _unrevealedCardCounts[shape] = table.GetUnrevealedCardCount(shape);
            _unknownCardCounts[shape] = _unrevealedCardCounts[shape];
        }

        // 미공개 카드 중 딜러만 아는 카드(뽑은 카드 등)를 빼면 딜러가 모르는 카드가 남는다 (딜러 AI 세부 기획서 1장 용어)
        foreach (Card card in table.GetKnownCards(Side.Dealer))
        {
            if (!card.KnownByPlayer)
            {
                _unknownCardCounts[card.Shape]--;
            }
        }
    }

    // side의 카드 조합 칸을 받은 순서대로 돌려준다. 딜러가 모르는 칸은 null이다
    public IReadOnlyList<HandShape?> GetSlotShapes(Side side)
    {
        return side == Side.Player ? _playerSlotShapes : _dealerSlotShapes;
    }

    // 카드풀 구성: 이번 카드 셔플의 카드풀에서 shape인 카드 수
    public int GetPoolCardCount(HandShape shape)
    {
        return _poolCardCounts[shape];
    }

    // 손 모양별로 아직 양쪽 모두에게 공개되지 않은 카드 수
    public int GetUnrevealedCardCount(HandShape shape)
    {
        return _unrevealedCardCounts[shape];
    }

    // 손 모양별로 딜러가 모르는 카드 수 (미공개 카드에서 딜러가 아는 카드를 뺀 수)
    public int GetUnknownCardCount(HandShape shape)
    {
        return _unknownCardCounts[shape];
    }

    // 조합 칸의 카드를 받은 순서대로 손 모양으로 옮겨 적는다. 딜러가 모르는 카드는 null로 남겨,
    // 판단 코드가 모르는 카드의 손 모양을 읽을 수 없게 한다 (딜러 AI 세부 기획서 2장 치트 금지).
    // 칸 순서를 유지하므로 조합 판단에서 몇 번째 칸이 비었는지도 알 수 있다
    private static void CopyShapesKnownByDealer(IReadOnlyList<Card> slots, List<HandShape?> shapes)
    {
        foreach (Card card in slots)
        {
            shapes.Add(card.KnownByDealer ? card.Shape : (HandShape?)null);
        }
    }
}
