using System.Collections.Generic;
using UnityEngine;

// 테이블 가운데 카드 표시: 양쪽 카드 슬롯, 더미 위로 들어 올린 뽑은 카드, 두 슬롯 사이의 교환 표시
// (UI·UX 화면 흐름 세부 기획서 2-2장 카드 슬롯, 3-1장 판-02·판-03, 4장 카드 표시 규칙).
// 진행 흐름(GameFlow)에 변경 이벤트가 없어 매 프레임 상태를 읽어 CardFaceRules로 표시를 정한다.
// 카드 이동·뒤집기 연출은 RSP-63, 조합 칸 표시는 RSP-40에서 맡는다
public class TableCardsUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;

    [SerializeField] private CardView _playerSlot;
    [SerializeField] private CardView _dealerSlot;
    [SerializeField] private CardView _drawnCard;
    [SerializeField] private GameObject _swapMarker;

    [SerializeField] private Sprite _cardBack;
    [SerializeField] private Sprite _scissorsFront;
    [SerializeField] private Sprite _rockFront;
    [SerializeField] private Sprite _paperFront;

    private GameFlow _flow;

    private void Awake()
    {
        _flow = _runner.Flow;
    }

    private void Update()
    {
        GamePhase phase = _flow.CurrentPhase;

        ShowCard(_drawnCard, _flow.DrawnCard, phase);

        bool hasSlotCards = CardFaceRules.HasSlotCards(phase);
        ShowCard(_playerSlot, hasSlotCards ? GetLastCard(Side.Player) : null, phase);
        ShowCard(_dealerSlot, hasSlotCards ? GetLastCard(Side.Dealer) : null, phase);

        // 실제로 교환됐는지는 돋보기 기능(RSP-54)에서 분배 직후 위치와 비교해 넘긴다. 그 전까지는 교환이 일어나지 않는다
        bool isSwapped = false;
        _swapMarker.SetActive(CardFaceRules.ShowsSwapMarker(isSwapped, phase));
    }

    private void ShowCard(CardView view, Card card, GamePhase phase)
    {
        if (card == null)
        {
            view.Hide();
            return;
        }

        Sprite sprite = CardFaceRules.ShowsFront(card) ? GetFrontSprite(card.Shape) : _cardBack;
        view.Show(sprite, CardFaceRules.ShowsKnownMarker(card, phase));
    }

    // 카드 슬롯에 카드가 있는 동안 슬롯 카드는 카드 조합 칸의 마지막 카드다 (CardFaceRules.HasSlotCards)
    private Card GetLastCard(Side side)
    {
        IReadOnlyList<Card> slots = _flow.GetCombinationSlots(side);
        return slots[slots.Count - 1];
    }

    private Sprite GetFrontSprite(HandShape shape)
    {
        switch (shape)
        {
            case HandShape.Scissors:
                return _scissorsFront;
            case HandShape.Rock:
                return _rockFront;
            default:
                return _paperFront;
        }
    }
}
