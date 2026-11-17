using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 한쪽(플레이어 또는 딜러)의 카드 조합 칸: 받은 순서대로 3칸, 칸 사이 이김·짐 화살표, 두 칸이 찼을 때 가능 패턴 안내
// (UI·UX 화면 흐름 세부 기획서 5-6장 조합 칸, 4-2장 표시 범위).
// 조합 칸 카드는 항상 공개된 카드라 앞면으로만 보여 주고 눈 표시를 붙이지 않는다.
// 진행 흐름(GameFlow)에 변경 이벤트가 없어 매 프레임 상태를 읽는다. 바꾸는 값이 고정 문구·그림뿐이라 할당은 없다.
// 정산 때의 패턴 이름 표시는 RSP-71, 카드가 조합 칸으로 옮겨지는 연출은 RSP-63에서 맡는다
public class CombinationSlotsUI : MonoBehaviour
{
    private const string AttackHint = "공격형 풀세트 가능";
    private const string HealHint = "회복형 풀세트 가능";
    private const string TripleHint = "트리플 가능";

    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private Side _side;

    // 왼쪽부터 받은 순서(시작 카드 → 첫 번째 판 → 두 번째 판)의 3칸
    [SerializeField] private SpriteRenderer[] _slotCards;

    // 칸 사이 화살표. 0번은 1·2칸 사이, 1번은 2·3칸 사이.
    // 아래쪽 화살표 그림(Kenney Game Icons down) 하나를 쓰고, 위쪽은 위아래로 뒤집어 보여 준다
    [SerializeField] private SpriteRenderer[] _links;
    [SerializeField] private TMP_Text _hintText;

    // 카드 앞면 그림을 가진 테이블 카드 표시. 그림 연결을 한 곳에만 둔다
    [SerializeField] private TableCardsUI _tableCards;

    // 위쪽(앞 카드를 이김)은 대미지 암시 빨강, 아래쪽(앞 카드에 짐)은 회복 암시 초록 (UI·UX 화면 흐름 세부 기획서 5-6장 조합 칸)
    [SerializeField] private Color _winColor = new Color(0.85f, 0.25f, 0.25f);
    [SerializeField] private Color _loseColor = new Color(0.25f, 0.7f, 0.35f);

    private GameFlow _flow;

    private void Awake()
    {
        _flow = _runner.Flow;
    }

    private void Update()
    {
        IReadOnlyList<Card> slots = _flow.GetCombinationSlots(_side);
        int shownCount = CombinationSlotRules.GetShownCount(slots.Count, _flow.CurrentPhase);

        for (int i = 0; i < _slotCards.Length; i++)
        {
            bool isShown = i < shownCount;
            _slotCards[i].gameObject.SetActive(isShown);
            if (isShown)
            {
                _slotCards[i].sprite = _tableCards.GetFrontSprite(slots[i].Shape);
            }
        }

        // 화살표는 이김·짐 관계일 때만 켠다. 마지막 칸으로 가는 화살표는 조합이 완성될 때만 켠다 (CombinationSlotRules.GetShownLink)
        for (int i = 0; i < _links.Length; i++)
        {
            MatchResult link = CombinationSlotRules.GetShownLink(slots, shownCount, i);
            bool isShown = link != MatchResult.Draw;
            _links[i].gameObject.SetActive(isShown);
            if (isShown)
            {
                ShowLink(_links[i], link);
            }
        }

        bool showsHint = shownCount == CombinationSlotRules.HintCardCount;
        _hintText.gameObject.SetActive(showsHint);
        if (showsHint)
        {
            _hintText.text = GetHint(CombinationSlotRules.GetPossibleCombination(slots[0].Shape, slots[1].Shape));
        }
    }

    private void ShowLink(SpriteRenderer link, MatchResult result)
    {
        bool isWin = result == MatchResult.Win;
        link.flipY = isWin;
        link.color = isWin ? _winColor : _loseColor;
    }

    private string GetHint(CombinationType possible)
    {
        switch (possible)
        {
            case CombinationType.AttackFullSet:
                return AttackHint;
            case CombinationType.HealFullSet:
                return HealHint;
            default:
                return TripleHint;
        }
    }
}
