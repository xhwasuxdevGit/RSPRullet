using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 플레이어용 또는 딜러용 아이템 칸과 아이템 사용 표시 (UI·UX 화면 흐름 세부 기획서 5-9장 아이템 칸, 5-3장 아이템 사용 표시).
// 같은 스크립트를 플레이어용·딜러용으로 두 번 붙이고 _side로 구분한다.
// 보유 아이템은 양쪽 모두 항상 보여 주고, 칸을 눌러 아이템을 쓰는 것은 플레이어용만 한다.
// 진행 흐름(GameFlow)의 단계 변경·보유 아이템 변경·아이템 사용 알림을 받을 때만 상태를 읽는다.
// 아이템을 써도 단계는 그대로라서 회색 표시와 사용 표시는 아이템 사용 알림으로 다시 정한다
public class ItemSlotsUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private Side _side;

    // 왼쪽부터 보유 칸. 가장 큰 보유 한도(3라운드 6칸)만큼 두고, 이번 라운드 보유 한도만큼만 보여 준다.
    // _slotButtons와 _slotIcons는 같은 순서다. 딜러용 칸은 누르지 않으므로 버튼을 항상 꺼 둔다
    [SerializeField] private Button[] _slotButtons;
    [SerializeField] private Image[] _slotIcons;

    // 이번 판에 쓴 아이템 아이콘 자리. 같은 종류는 한 판에 한 번만 쓰므로 3칸이면 된다
    [SerializeField] private Image[] _usedIcons;

    // 아이템이 없는 라운드(1라운드)에 함께 숨길 사용 표시 영역
    [SerializeField] private GameObject _usedArea;

    [Header("아이콘")]
    [SerializeField] private Sprite _coffeeIcon;
    [SerializeField] private Sprite _magnifierIcon;
    [SerializeField] private Sprite _clockIcon;

    // 아이템 단계에서 _side가 쓸 수 없는 아이템의 아이콘 색. 칸 바탕보다 짙은 색으로 구분한다
    [SerializeField] private Color _unusableColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    private GameFlow _flow;

    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.PhaseChanged += OnPhaseChanged;
        _flow.ItemsChanged += Refresh;
        _flow.ItemUsed += Refresh;

        if (_side != Side.Player) { return; }

        // 칸 번호를 넘기려고 칸마다 리스너를 따로 만든다. 지울 때는 이 칸 버튼의 리스너만 모두 지운다 (OnDestroy)
        for (int i = 0; i < _slotButtons.Length; i++)
        {
            int slotIndex = i;
            _slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
        }
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
        _flow.ItemsChanged -= Refresh;
        _flow.ItemUsed -= Refresh;

        foreach (Button button in _slotButtons)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        Refresh();
    }

    private void Refresh()
    {
        RefreshSlots();
        RefreshUsedIcons();
    }

    private void RefreshSlots()
    {
        IReadOnlyList<ItemType> items = _flow.Items.GetItems(_side);

        // 라운드 시작 지급은 첫 카드 셔플의 테이블이 만들어지기 전에 알림이 오므로,
        // 턴 플레이어가 필요한 판단(누를 수 있는지, 쓸 수 있는지)은 아이템 단계에서만 한다
        bool isItemPhase = _flow.CurrentPhase == GamePhase.ItemTurnPlayer || _flow.CurrentPhase == GamePhase.ItemOpponent;
        bool canInput = isItemPhase && CanPlayerUseItems();

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            _slotButtons[i].gameObject.SetActive(i < _flow.Items.Capacity);

            bool hasItem = i < items.Count;
            _slotIcons[i].enabled = hasItem;
            if (!hasItem)
            {
                _slotButtons[i].interactable = false;
                continue;
            }

            ItemType item = items[i];
            _slotIcons[i].sprite = GetIcon(item);

            // 쓸 수 없는 아이템은 아이템 단계 동안 짙은 색으로 표시한다 (UI·UX 화면 흐름 세부 기획서 5-9장 회색 표시).
            // 딜러 아이템도 이번 판에 같은 종류를 이미 썼거나 턴 플레이어가 아닐 때의 시계면 같은 기준으로 표시한다
            // 플레이어가 돋보기 뒤 바꿀지 고르는 동안에는 칸이 잠기므로, 그동안 내 칸도 짙은 색으로 표시한다
            bool isUsable = isItemPhase && _flow.CanUseItem(_side, item);
            bool isLockedByMagnifierChoice = _side == Side.Player && _flow.IsWaitingForMagnifierChoice;
            _slotButtons[i].interactable = canInput && isUsable;
            _slotIcons[i].color = isItemPhase && (!isUsable || isLockedByMagnifierChoice) ? _unusableColor : Color.white;
        }
    }

    // 이번 판에 _side가 쓴 아이템을 쓴 순서대로 보여 주고, 효과가 사라진 아이템은 지운다 (ItemUseDisplayRules)
    private void RefreshUsedIcons()
    {
        _usedArea.SetActive(_flow.Items.Capacity > 0);

        int shownCount = 0;
        foreach ((Side User, ItemType Item, bool Exchanged) use in _flow.Items.ItemUses)
        {
            if (use.User != _side || !ItemUseDisplayRules.IsShown(use.Item, _flow.CurrentPhase)) { continue; }
            if (shownCount >= _usedIcons.Length) { break; }

            _usedIcons[shownCount].sprite = GetIcon(use.Item);
            shownCount++;
        }

        for (int i = 0; i < _usedIcons.Length; i++)
        {
            _usedIcons[i].gameObject.SetActive(i < shownCount);
        }
    }

    // 플레이어가 지금 아이템 칸을 누를 수 있는지. 하단 버튼에 [아이템 사용 완료]·[아이템 없음]이 켜지는 때와 같다.
    // 돋보기 선택 중에는 그 버튼 대신 바꾼다·그대로 둔다가 켜지므로 칸도 잠긴다 (PhaseButtonRules)
    private bool CanPlayerUseItems()
    {
        if (_side != Side.Player) { return false; }

        PhaseButton buttons = PhaseButtonRules.GetButtons(_flow.CurrentPhase, _flow.Table.TurnPlayer,
            hasUsableItem: _flow.HasUsableItem(Side.Player), isWaitingForMagnifierChoice: _flow.IsWaitingForMagnifierChoice,
            playerWonRound: false, isFinalRound: false);
        return (buttons & (PhaseButton.ItemDone | PhaseButton.NoItem)) != 0;
    }

    private void OnSlotClicked(int slotIndex)
    {
        IReadOnlyList<ItemType> items = _flow.Items.GetItems(Side.Player);
        if (slotIndex >= items.Count) { return; }

        _flow.UseItem(items[slotIndex]);
    }

    private Sprite GetIcon(ItemType item)
    {
        switch (item)
        {
            case ItemType.Coffee:
                return _coffeeIcon;
            case ItemType.Magnifier:
                return _magnifierIcon;
            default:
                return _clockIcon;
        }
    }
}
