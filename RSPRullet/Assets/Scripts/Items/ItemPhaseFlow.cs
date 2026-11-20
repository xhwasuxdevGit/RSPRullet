using System;

// 아이템 단계에서는 아이템을 쓰는 쪽의 차례, 입력 확인, 돋보기 선택 대기, 아이템 효과 적용 호출을 정하고, 플레이어 차례에서만 입력을 기다린다.
// 딜러가 무엇을 쓸지는 딜러 AI(DealerAI)가 정하고, 쓴 결과의 적용(돋보기 확인·교환, 시계)은 이 클래스가 양쪽 공통으로 맡는다.
// 시계 효과는 아이템 단계 밖(판이 끝날 때, 카드 셔플 시작 때)에 적용되므로, 게임 진행이 그 시점에 부른다.
// 딜러 차례는 입력을 기다리지 않고, 딜러 AI가 끝내기를 돌려줄 때까지 판단한 아이템을 하나씩 쓴다
public class ItemPhaseFlow
{
    // 진행 단계 관리, 아이템 상태, 딜러 AI는 게임 진행이 만들어 함께 쓴다
    private readonly PhaseMachine _phaseMachine;
    private readonly ItemState _items;
    private readonly DealerAI _dealer;
    private readonly RoundSettings _round;
    private readonly ItemSettings _itemSettings;
    private readonly Random _random;

    // 테이블 상태는 카드 셔플마다 새로 만들어지므로 아이템 단계를 시작할 때 받는다
    private TableState _table;

    // 두 번째 판에 쓴 시계의 이월. 다음 카드 셔플에서 선 정하기를 건너뛸 때 쓰고 비운다.
    // 이 클래스는 라운드마다 새로 만들어지므로 라운드가 끝나면 이월도 사라진다 (아이템 세부 기획서 2-3장 시계)
    private bool _hasClockCarryOver;
    private Side _clockCarryOverUser;

    public event Action<string> Logged;

    public event Action ItemUsed;

    public bool IsWaitingForMagnifierChoice { get; private set; }

    // 플레이어가 아이템 입력(UseItem, FinishPlayerPhase)을 할 수 있는지. 돋보기 선택 중에는 선택부터 끝내야 한다
    public bool IsPlayerInputPhase => IsItemPhase() && GetItemPhaseSide() == Side.Player && !IsWaitingForMagnifierChoice;

    // 두 아이템 단계가 모두 끝나 카드 공개로 갈 차례인지
    public bool IsFinished => _phaseMachine.CurrentPhase == GamePhase.Reveal;

    // 같은 시드의 난수를 넘기면 카드 셔플 지급이 같게 나와 테스트에서 진행을 재현할 수 있다
    public ItemPhaseFlow(PhaseMachine phaseMachine, ItemState items, DealerAI dealer, RoundSettings round, ItemSettings itemSettings, Random random)
    {
        _phaseMachine = phaseMachine;
        _items = items;
        _dealer = dealer;
        _round = round;
        _itemSettings = itemSettings;
        _random = random;
    }

    public void SupplyStartItems()
    {
        ItemSupply.SupplyStartItems(_items, _round.StartMagnifierCount);
        LogHeldItems("라운드 시작 지급");
    }

    public void SupplyShuffleItems()
    {
        ItemSupply.SupplyShuffleItems(_items, _round.ItemsPerShuffle, _itemSettings, _random);
        LogHeldItems("카드 셔플 지급");
    }

    public void BeginMatch() => _items.ClearItemUses();

    public void Start(TableState table)
    {
        _table = table;
        if (_items.Capacity == 0)
        {
            _phaseMachine.CompleteItemUse();
            _phaseMachine.CompleteItemUse();
            return;
        }

        Continue();
    }

    public bool UseItem(ItemType item)
    {
        if (!IsPlayerInputPhase || !_items.TryUseItem(Side.Player, item, _table.TurnPlayer))
        {
            return false;
        }

        Log($"플레이어가 {DisplayNames.GetItemName(item)}를 썼다");
        if (item == ItemType.Magnifier)
        {
            PeekWithMagnifier(Side.Player);
            IsWaitingForMagnifierChoice = true;
        }

        ItemUsed?.Invoke();
        return true;
    }

    // 돋보기 선택 입력: 바꾼다(exchange: true)면 양쪽 이번 판 카드를 맞바꾸고, 그대로 둔다(false)면 카드는 그대로다.
    public bool ChooseMagnifierExchange(bool exchange)
    {
        if (!IsWaitingForMagnifierChoice)
        {
            return false;
        }

        IsWaitingForMagnifierChoice = false;
        ApplyMagnifierChoice(Side.Player, exchange);
        ItemUsed?.Invoke();
        return true;
    }

    // 플레이어의 [아이템 사용 완료] 또는 [아이템 없음] 입력. 상대 단계로 넘어가거나 IsFinished가 된다.
    public bool FinishPlayerPhase()
    {
        if (!IsPlayerInputPhase)
        {
            return false;
        }

        _phaseMachine.CompleteItemUse();
        Continue();
        return true;
    }

    // 판 결과 확인 뒤, 턴을 넘기기 전에 부른다. 이번 판 턴 플레이어가 시계를 썼는지 돌려주고,
    // 두 번째 판이었다면 다음 카드 셔플로 이월한다 (아이템 세부 기획서 5장 I6)
    public bool ApplyClockAtMatchEnd()
    {
        Side turnPlayer = _table.TurnPlayer;
        if (!_items.HasUsedItem(turnPlayer, ItemType.Clock)) { return false; }

        if (_phaseMachine.MatchIndex == GameRules.MatchesPerShuffle - 1)
        {
            _hasClockCarryOver = true;
            _clockCarryOverUser = turnPlayer;
            Log($"시계 이월: 다음 카드 셔플은 {DisplayNames.GetSideName(turnPlayer)} 선");
            return true;
        }

        Log($"시계: 다음 판도 {DisplayNames.GetSideName(turnPlayer)} 차례");
        return true;
    }

    // 카드 셔플의 시작 카드 배분 뒤에 부른다. 이월이 있으면 시계를 쓴 쪽을 첫 번째 판 턴 플레이어로 두고 이월을 비운 뒤 true를 돌려준다
    public bool ApplyClockCarryOver(TableState table)
    {
        if (!_hasClockCarryOver) { return false; }

        _hasClockCarryOver = false;
        table.SetFirstTurnPlayerByClock(_clockCarryOverUser);
        return true;
    }

    // 지금 아이템 단계가 플레이어 차례면 입력을 기다린다. 딜러 차례는 바로 진행해 끝낸다
    private void Continue()
    {
        while (IsItemPhase())
        {
            if (GetItemPhaseSide() == Side.Player)
            {
                return;
            }

            PlayDealerItemPhase();
            _phaseMachine.CompleteItemUse();
        }
    }

    // 딜러 아이템 단계. 쓸 수 있는 아이템이 없으면 판단 없이 넘긴다 (아이템 세부 기획서 3-3장).
    // 있으면 딜러 AI가 끝내기(null)를 돌려줄 때까지 하나씩 쓴다. 쓴 아이템과 돋보기로 알게 된 카드가 다음 판단에 반영되도록
    // 판단할 때마다 딜러가 아는 정보를 새로 만든다
    private void PlayDealerItemPhase()
    {
        if (!_items.HasUsableItem(Side.Dealer, _table.TurnPlayer))
        {
            Log("딜러가 쓸 수 있는 아이템이 없어 아이템 단계를 넘겼다");
            return;
        }

        ItemType? item = ChooseDealerItem();
        while (item != null && UseDealerItem(item.Value))
        {
            item = ChooseDealerItem();
        }

        Log("딜러가 아이템 단계를 끝냈다");
    }

    private ItemType? ChooseDealerItem()
    {
        return _dealer.ChooseItem(new DealerKnownInfo(_table, _phaseMachine.MatchIndex), _items, _table.TurnPlayer);
    }

    // 딜러가 고른 아이템을 쓴다. 돋보기면 확인한 뒤 바로 교환 판단까지 한다.
    // 쓰지 못하면 false를 돌려줘, 같은 아이템을 계속 고르는 반복을 막는다
    private bool UseDealerItem(ItemType item)
    {
        if (!_items.TryUseItem(Side.Dealer, item, _table.TurnPlayer)) { return false; }

        Log($"딜러가 {DisplayNames.GetItemName(item)}를 썼다");
        if (item == ItemType.Magnifier)
        {
            PeekWithMagnifier(Side.Dealer);
            bool exchange = _dealer.ChooseExchange(new DealerKnownInfo(_table, _phaseMachine.MatchIndex), _items);
            ApplyMagnifierChoice(Side.Dealer, exchange);
        }

        ItemUsed?.Invoke();
        return true;
    }

    private bool IsItemPhase() => _phaseMachine.CurrentPhase == GamePhase.ItemTurnPlayer || _phaseMachine.CurrentPhase == GamePhase.ItemOpponent;

    // 지금 아이템 단계에서 아이템을 쓰는 쪽: 턴 플레이어 단계면 턴 플레이어, 상대 단계면 그 상대
    private Side GetItemPhaseSide() => _phaseMachine.CurrentPhase == GamePhase.ItemTurnPlayer ? _table.TurnPlayer : TableState.Opponent(_table.TurnPlayer);

    // 돋보기 확인: user가 상대 자리의 이번 판 카드를 알게 된다. 본 카드 내용은 쓴 사람만 안다 (아이템 세부 기획서 2-2장 돋보기).
    private void PeekWithMagnifier(Side user)
    {
        Card peeked = _table.PeekOpponentMatchCard(user);
        Side opponent = TableState.Opponent(user);
        Log($"{DisplayNames.GetSideName(user)}가 돋보기로 {DisplayNames.GetSideName(opponent)} 카드를 확인했다: " +
            $"{DisplayNames.GetShapeName(peeked.Shape)} ({DisplayNames.GetSideName(user)}만 앎)");
    }

    private void ApplyMagnifierChoice(Side user, bool exchange)
    {
        if (!exchange)
        {
            Log($"{DisplayNames.GetSideName(user)}가 카드를 그대로 뒀다");
            return;
        }

        _table.SwapMatchCards();
        _items.MarkExchanged(user);
        Log($"{DisplayNames.GetSideName(user)}가 돋보기로 카드를 교환했다");
    }

    // 지급 뒤 양쪽 보유 아이템을 로그로 남긴다. 아이템이 없는 라운드(1라운드)는 남기지 않는다
    private void LogHeldItems(string reason)
    {
        if (_items.Capacity == 0)
        {
            return;
        }

        Log(FlowLogText.GetHeldItems(reason, _items.GetItems(Side.Player), _items.GetItems(Side.Dealer)));
    }

    private void Log(string message) => Logged?.Invoke(message);
}
