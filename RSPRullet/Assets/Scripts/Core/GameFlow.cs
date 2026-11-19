using System;
using System.Collections.Generic;
using UnityEngine;

// 라운드 하나의 진행 흐름. 진행 단계, 테이블 상태, 체력, 딜러 AI, 아이템 상태를 묶고,
// 플레이어 입력과 딜러 고민 중 대기에서만 멈춘다. 화면은 참조하지 않으며, 화면은 이벤트를 받을 때만 상태를 다시 읽는다.
// 이 클래스는 흐름(단계 진행, 입력 확인, 지급·다시 섞기 시점, 아이템 단계 차례, 기록·로그를 남기는 시점)만 정한다.
// 상태 계산과 변경 알림은 상태를 가진 클래스(TableState, HealthState, ItemState)가 맡고, 이 클래스는 알림을 화면에 넘긴다.
// 시계 효과와 딜러의 아이템 판단은 아직 없어, 시계 이월은 항상 없고 딜러는 아이템 단계를 바로 끝낸다.
public class GameFlow
{
    private readonly System.Random _random;
    private readonly PhaseMachine _phaseMachine = new PhaseMachine();
    private readonly HealthState _health;
    private readonly DealerAI _dealer;
    private readonly ItemState _items;
    private readonly ItemSettings _itemSettings;

    // 라운드 시작 돋보기 수와 카드 셔플당 지급 수를 읽는다
    private readonly RoundSettings _round;

    // 카드 셔플마다 새로 만든다
    private TableState _table;

    // 정산 결과 라운드가 끝나는지. 정산 확인 입력(Next)에서 진행 단계로 넘긴다
    private bool _isRoundOverAfterSettlement;

    public event Action<string> Logged;
    public event Action<string> JudgmentRecorded;

    // 카드 셔플이 시작될 때마다 보낸다. 선 정하기가 비겨 다시 섞을 때도 보낸다
    public event Action JudgmentRecordCleared;

    // 카드 셔플 단계로 들어가는 알림은 보내지 않는다. 라운드 첫 카드 셔플은 테이블 상태가 없어 턴 플레이어를 읽을 수 없다
    public event Action<GamePhase, GamePhase> PhaseChanged;

    // 아래 알림은 상태가 실제로 바뀌었을 때만 보낸다. 카드풀 정보는 판 시작 때만 정해져 그때 보낸다.
    // 카드·체력·아이템 알림은 각 상태 클래스의 알림을 그대로 넘긴다
    public event Action CardsChanged;
    public event Action DeckChanged;
    public event Action HealthChanged;
    public event Action ItemsChanged;

    // 아이템을 썼을 때와, 돋보기 뒤 교환 여부를 골랐을 때 보낸다. 둘 다 사용 기록(ItemUses)이 공개하는 정보가 정해지는 시점이다
    public event Action ItemUsed;

    public GamePhase CurrentPhase => _phaseMachine.CurrentPhase;
    public int MatchIndex => _phaseMachine.MatchIndex;
    public Side TurnPlayer => _table.TurnPlayer;
    public Card DrawnCard => _table.DrawnCard;
    public int RemainingDeckCount => _table.RemainingDeckCount;

    // 상단 정보의 라운드 번호와 카드 셔플 번호.
    // 카드 셔플 번호는 카드 셔플을 시작할 때 1씩 늘리고, 선 정하기가 비겨 다시 섞을 때는 같은 카드 셔플이라 늘리지 않는다
    public int RoundNumber { get; }
    public int ShuffleCount { get; private set; }

    // 카드풀 정보의 "n장 → n장". 테이블 상태가 판 시작 때 기록한 값이다
    public int MatchStartDeckCount => _table.MatchStartDeckCount;
    public int MatchEndDeckCount => _table.MatchEndDeckCount;

    // 딜러가 턴 플레이어인 판에서 뽑은 카드를 보며 분배를 고민하는 중인지.
    // 딜러는 판 시작 때 먼저 뽑아 두므로 분배 단계에서 기다린다. 대기 시간은 GameFlowRunner가 재고, 끝나면 PlayDealerTurn을 부른다
    public bool IsWaitingForDealer => CurrentPhase == GamePhase.Distribute && TurnPlayer == Side.Dealer;

    // 이번 판에 양쪽이 쓴 아이템과 돋보기 뒤 교환 여부
    public IReadOnlyList<(Side User, ItemType Item, bool Exchanged)> ItemUses => _items.ItemUses;

    // 플레이어가 돋보기로 상대 카드를 본 뒤 바꿀지 그대로 둘지 고르는 중인지. 아이템 단계 안의 대기라 진행 단계는 그대로다.
    // 고르는 동안에는 다른 아이템 입력을 받지 않고, ChooseMagnifierExchange만 받는다
    public bool IsWaitingForMagnifierChoice { get; private set; }

    // 이번 판 카드가 분배 직후와 다른 자리에 있는지 (교환 표시). 두 번 교환해 원래 자리로 돌아오면 false다
    public bool IsMatchCardSwapped => _table.IsMatchCardSwapped;

    // 같은 시드의 난수를 넘기면 카드풀·더미 순서·딜러 선택·아이템 지급이 같게 나와 테스트에서 진행을 재현할 수 있다
    public GameFlow(RuleSettings rules, RoundSettings round, DealerSettings dealerSettings, ItemSettings itemSettings, System.Random random)
    {
        _random = random;
        _round = round;
        _itemSettings = itemSettings;
        RoundNumber = round.RoundNumber;
        _health = new HealthState(rules, round.StartHealth);
        _dealer = new DealerAI(rules, dealerSettings, random);
        _items = new ItemState(round.ItemCapacity);

        // 진행 단계 관리, 체력 상태, 아이템 상태는 이 클래스가 만들어 함께 사라지므로 구독을 해제하지 않는다
        _phaseMachine.PhaseChanged += OnPhaseMachineChanged;
        _health.Changed += OnHealthChanged;
        _items.Changed += OnItemsChanged;
    }

    public int GetHealth(Side side)
    {
        return _health.GetHealth(side);
    }

    // 보유 아이템은 양쪽에 항상 공개된다. 화면은 읽기만 하고, 바꾸는 것은 이 클래스의 입력과 지급뿐이다
    public IReadOnlyList<ItemType> GetItems(Side side)
    {
        return _items.GetItems(side);
    }

    // [아이템 사용 완료]·[아이템 없음] 버튼 전환에 쓴다
    public bool HasUsableItem(Side side)
    {
        return _items.HasUsableItem(side, TurnPlayer);
    }

    public IReadOnlyList<Card> GetCombinationSlots(Side side)
    {
        return _table.GetCombinationSlots(side);
    }

    // 이번 카드 셔플의 카드풀 구성. 카드 셔플 동안 바뀌지 않는다
    public int GetPoolCardCount(HandShape shape)
    {
        return _table.GetPoolCardCount(shape);
    }

    // 라운드 시작 → 첫 번째 카드 셔플 → 첫 번째 판의 입력 대기 지점까지 진행한다
    public bool StartRound()
    {
        if (!_phaseMachine.CompleteRoundStart())
        {
            return false;
        }

        Log($"라운드 시작: 양쪽 체력 {_health.GetHealth(Side.Player)}");

        // 라운드 시작 지급 (코어 규칙 상세 기획서 2-1장 ① 라운드 시작, 아이템 세부 기획서 4-2장 지급 시점)
        ItemSupply.SupplyStartItems(_items, _round.StartMagnifierCount);
        LogHeldItems("라운드 시작 지급");
        StartShuffle();
        return true;
    }

    // 플레이어가 턴 플레이어일 때 카드 더미 맨 위 카드를 뽑는다
    public bool DrawCard()
    {
        if (CurrentPhase != GamePhase.Draw || TurnPlayer != Side.Player)
        {
            return Ignore(nameof(DrawCard));
        }

        _table.DrawForTurnPlayer();
        _phaseMachine.CompleteDraw();
        Log($"뽑은 카드:{DisplayNames.GetShapeName(_table.DrawnCard.Shape)} (플레이어만 앎)");
        return true;
    }

    // 플레이어가 뽑은 카드를 가지거나(keep: true) 딜러에게 준다(keep: false). 이어서 아이템 단계로 간다
    public bool Distribute(bool keep)
    {
        if (CurrentPhase != GamePhase.Distribute || TurnPlayer != Side.Player)
        {
            return Ignore(nameof(Distribute));
        }

        _table.Distribute(keep);
        Log(keep ? "플레이어가 카드를 가졌다" : "플레이어가 카드를 딜러에게 줬다");
        _phaseMachine.CompleteDistribute();
        StartItemPhase();
        return true;
    }

    // 플레이어의 아이템 단계에서 아이템 하나를 쓴다. 쓸 수 없는 아이템이면 무시한다.
    // 보유 칸에서 빼고 사용 기록에 남긴다. 커피는 판정 때 이 사용 기록을 읽어 효과를 낸다.
    // 돋보기는 딜러 카드를 플레이어에게만 보여 주고, 바꿀지 고르는 입력(ChooseMagnifierExchange)을 기다린다
    public bool UseItem(ItemType item)
    {
        if (!IsPlayerItemPhase() || !_items.TryUseItem(Side.Player, item, TurnPlayer))
        {
            return Ignore($"{nameof(UseItem)}({item})");
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
    // 어느 쪽이든 교환 여부는 공개되고, 플레이어 아이템 단계의 입력 대기로 돌아간다
    public bool ChooseMagnifierExchange(bool exchange)
    {
        if (!IsWaitingForMagnifierChoice)
        {
            return Ignore($"{nameof(ChooseMagnifierExchange)}({exchange})");
        }

        IsWaitingForMagnifierChoice = false;
        ApplyMagnifierChoice(Side.Player, exchange);
        ItemUsed?.Invoke();
        return true;
    }

    // 플레이어의 [아이템 사용 완료] 또는 [아이템 없음] 입력. 상대 단계나 카드 공개로 넘어간다
    public bool FinishItemPhase()
    {
        if (!IsPlayerItemPhase())
        {
            return Ignore(nameof(FinishItemPhase));
        }

        _phaseMachine.CompleteItemUse();
        ContinueItemPhase();
        return true;
    }

    // 딜러 차례 판에서 고민 중 대기가 끝나면 딜러가 이미 뽑아 둔 카드를 분배한 뒤 아이템 단계로 간다
    public bool PlayDealerTurn()
    {
        if (!IsWaitingForDealer)
        {
            return Ignore(nameof(PlayDealerTurn));
        }

        _dealer.TryDistribute(_table, MatchIndex);

        // 가졌는지 줬는지는 항상 공개되는 정보다. 카드 내용은 딜러만 안다
        Log(_table.LastDistributionKept ? "딜러가 카드를 가졌다" : "딜러가 카드를 플레이어에게 줬다");
        _phaseMachine.CompleteDistribute();
        StartItemPhase();
        return true;
    }

    // 판 결과 확인 또는 정산 확인 뒤 다음으로 넘어간다 (판 결과는 GameFlowRunner의 자동 진행, 정산은 다음 카드 셔플 버튼)
    public bool Next()
    {
        if (CurrentPhase == GamePhase.ShowResult)
        {
            FinishMatch();
            return true;
        }

        if (CurrentPhase == GamePhase.Settlement)
        {
            _phaseMachine.CompleteSettlement(_isRoundOverAfterSettlement);
            if (CurrentPhase == GamePhase.RoundEnd)
            {
                LogRoundEnd();
                return true;
            }

            StartShuffle();
            return true;
        }

        return Ignore(nameof(Next));
    }

    // 카드풀 생성 → 시작 카드 → 선 정하기. 선 정하기에서 비기면 카드풀부터 다시 만든다
    private void StartShuffle()
    {
        ShuffleCount++;

        // 카드 셔플 지급은 재섞기 반복 밖에서 한 번만 한다. 선 정하기가 비겨 다시 섞을 때는 지급하지 않는다 (코어 규칙 상세 기획서 6장 예외 E1)
        ItemSupply.SupplyShuffleItems(_items, _round.ItemsPerShuffle, _itemSettings, _random);
        LogHeldItems("카드 셔플 지급");

        bool isDecided;
        do
        {
            JudgmentRecordCleared?.Invoke();
            CreateTable();
            _phaseMachine.CompleteCardShuffle();

            _table.DealStartCards();
            HandShape playerStartShape = _table.GetStartCard(Side.Player).Shape;
            HandShape dealerStartShape = _table.GetStartCard(Side.Dealer).Shape;
            Log(FlowLogText.GetStartCards(playerStartShape, dealerStartShape));
            _phaseMachine.CompleteStartCardDeal(hasClockCarryOver: false);

            isDecided = _table.DecideFirstTurnPlayer();
            Record(JudgmentRecordRules.GetFirstTurnText(playerStartShape, dealerStartShape));
            _phaseMachine.CompleteDecideFirst(isDraw: !isDecided);
            if (!isDecided)
            {
                Log("선 정하기 무승부: 다시 섞습니다");
            }
        }
        while (!isDecided);

        Log($"선 정하기: {DisplayNames.GetSideName(TurnPlayer)} 선");
        BeginMatch();
    }

    // 새 테이블 상태를 만들고 카드 변경 알림을 구독한다.
    // 이전 테이블은 버려지므로 구독을 해제해 둔다
    private void CreateTable()
    {
        if (_table != null)
        {
            _table.Changed -= OnTableChanged;
        }

        _table = new TableState(new CardDeck(CardPool.Create(_random), _random));
        _table.Changed += OnTableChanged;
    }

    // 판 시작. 플레이어 차례면 카드 뽑기 입력을 기다린다.
    // 딜러 차례면 딜러가 바로 뽑고, 고민 중 대기 뒤의 PlayDealerTurn을 기다린다
    private void BeginMatch()
    {
        // 같은 종류는 한 판에 한 번이므로 판이 바뀌면 양쪽 사용 기록을 비운다
        _items.ClearItemUses();

        // 딜러 차례면 아래에서 바로 뽑으므로 뽑기 전에 판 카드 수를 기록한다
        _table.BeginMatch();
        DeckChanged?.Invoke();
        Log($"{MatchIndex + 1}번째 판: {DisplayNames.GetSideName(TurnPlayer)} 차례");
        if (TurnPlayer != Side.Dealer)
        {
            return;
        }

        _table.DrawForTurnPlayer();
        _phaseMachine.CompleteDraw();
        Log("딜러가 카드를 뽑았다 (딜러만 앎)");
    }

    // 분배 뒤 아이템 단계 시작. 턴 플레이어 → 상대 순서로 한 번씩 쓰고, 번갈아 주고받지 않는다.
    // 보유 한도가 0인 라운드(1라운드)는 아이템이 없어 아이템 단계를 건너뛴다
    private void StartItemPhase()
    {
        if (_items.Capacity == 0)
        {
            _phaseMachine.CompleteItemUse();
            _phaseMachine.CompleteItemUse();
            ResolveMatch();
            return;
        }

        ContinueItemPhase();
    }

    // 지금 아이템 단계가 플레이어 차례면 입력(UseItem, FinishItemPhase)을 기다린다.
    // 딜러 차례는 바로 끝낸다. 딜러의 아이템 판단이 아직 없어, 쓸 수 있는 아이템이 있어도 쓰지 않는다.
    // 두 단계가 모두 끝나면 카드 공개로 간다
    private void ContinueItemPhase()
    {
        while (CurrentPhase == GamePhase.ItemTurnPlayer || CurrentPhase == GamePhase.ItemOpponent)
        {
            if (GetItemPhaseSide() == Side.Player)
            {
                return;
            }

            Log("딜러가 아이템 단계를 끝냈다");
            _phaseMachine.CompleteItemUse();
        }

        ResolveMatch();
    }

    // 지금 아이템 단계에서 아이템을 쓰는 쪽: 턴 플레이어 단계면 턴 플레이어, 상대 단계면 그 상대
    private Side GetItemPhaseSide()
    {
        return CurrentPhase == GamePhase.ItemTurnPlayer ? TurnPlayer : TableState.Opponent(TurnPlayer);
    }

    // 플레이어가 아이템 입력(UseItem, FinishItemPhase)을 할 수 있는지. 돋보기 선택 중에는 선택부터 끝내야 한다
    private bool IsPlayerItemPhase()
    {
        bool isItemPhase = CurrentPhase == GamePhase.ItemTurnPlayer || CurrentPhase == GamePhase.ItemOpponent;
        return isItemPhase && GetItemPhaseSide() == Side.Player && !IsWaitingForMagnifierChoice;
    }

    // 돋보기 확인: user가 상대 자리의 이번 판 카드를 알게 된다. 본 카드 내용은 쓴 사람만 안다 (아이템 세부 기획서 2-2장 돋보기).
    // 딜러의 아이템 판단(RSP-58)도 같은 처리를 쓰도록 쓴 쪽을 받는다
    private void PeekWithMagnifier(Side user)
    {
        Card peeked = _table.PeekOpponentMatchCard(user);
        Side opponent = TableState.Opponent(user);
        Log($"{DisplayNames.GetSideName(user)}가 돋보기로 {DisplayNames.GetSideName(opponent)} 카드를 확인했다: " +
            $"{DisplayNames.GetShapeName(peeked.Shape)} ({DisplayNames.GetSideName(user)}만 앎)");
    }

    // 돋보기 선택 결과를 반영한다. 교환하면 카드와 아는 사람 정보가 함께 옮겨지고, 사용 기록에 교환을 남긴다.
    // 판정은 공개 시점에 각자 자리에 있는 카드로 하므로 교환된 카드가 판정과 조합 칸에 쓰인다 (코어 규칙 상세 기획서 6장 예외 E7)
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

    // 카드 공개 → 판정까지 진행하고, 판 결과 확인 입력을 기다린다
    private void ResolveMatch()
    {
        _table.RevealMatchCards();
        _phaseMachine.CompleteReveal();

        HandShape playerShape = _table.GetMatchCard(Side.Player).Shape;
        HandShape dealerShape = _table.GetMatchCard(Side.Dealer).Shape;
        // 커피는 카드가 아니라 쓴 사람에게 붙으므로, 카드가 교환되어도 쓴 사람의 사용 기록으로 판단한다 (아이템 세부 기획서 2-1장 커피)
        bool playerUsedCoffee = _items.HasUsedItem(Side.Player, ItemType.Coffee);
        bool dealerUsedCoffee = _items.HasUsedItem(Side.Dealer, ItemType.Coffee);
        MatchResult playerResult = _health.ApplyMatch(playerShape, dealerShape, playerUsedCoffee, dealerUsedCoffee);
        Record(JudgmentRecordRules.GetMatchText(MatchIndex, playerShape, dealerShape,
            _health.GetLastChange(Side.Player), _health.GetLastChange(Side.Dealer)));
        Log(FlowLogText.GetMatchResult(MatchIndex, playerShape, dealerShape, playerResult,
            _health.GetHealth(Side.Player), _health.GetHealth(Side.Dealer)));
        _phaseMachine.CompleteJudge();
    }

    // 판 결과 확인 뒤: 턴 교대 후 다음 판, 정산, 라운드 종료 중 하나로 간다
    private void FinishMatch()
    {
        _table.PassTurn(clockUsed: false);
        _phaseMachine.CompleteShowResult(_health.IsAnyoneAtZero);

        switch (CurrentPhase)
        {
            case GamePhase.Draw:
                BeginMatch();
                break;
            case GamePhase.Settlement:
                ApplySettlement();
                break;
            case GamePhase.RoundEnd:
                LogRoundEnd();
                break;
        }
    }

    // 남은 1장 공개 → 양쪽 조합 효과 동시 적용. 정산 확인 입력을 기다린다
    private void ApplySettlement()
    {
        Card remaining = _table.RevealRemainingCard();
        Log($"정산: 남은 카드 {DisplayNames.GetShapeName(remaining.Shape)} 공개");
        Record(JudgmentRecordRules.GetRemainingCardText(remaining.Shape));

        CombinationType playerCombination = _table.JudgeCombination(Side.Player);
        CombinationType dealerCombination = _table.JudgeCombination(Side.Dealer);
        _isRoundOverAfterSettlement = _health.ApplySettlement(playerCombination, dealerCombination);
        Record(JudgmentRecordRules.GetCombinationText(playerCombination, dealerCombination,
            _health.GetLastChange(Side.Player), _health.GetLastChange(Side.Dealer)));
        Log(FlowLogText.GetSettlement(playerCombination, dealerCombination,
            _health.GetHealth(Side.Player), _health.GetHealth(Side.Dealer)));
    }

    private void LogRoundEnd()
    {
        Side winner = _health.GetHealth(Side.Player) == GameRules.MinHealth ? Side.Dealer : Side.Player;
        Log($"라운드 종료: {DisplayNames.GetSideName(winner)} 승리");
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

    private void OnPhaseMachineChanged(GamePhase previous, GamePhase next)
    {
        if (next == GamePhase.CardShuffle)
        {
            return;
        }

        PhaseChanged?.Invoke(previous, next);
    }

    private void OnTableChanged()
    {
        CardsChanged?.Invoke();
    }

    private void OnHealthChanged()
    {
        HealthChanged?.Invoke();
    }

    private void OnItemsChanged()
    {
        ItemsChanged?.Invoke();
    }

    private bool Ignore(string input)
    {
        Debug.Log($"[GameFlow] 현재 단계 {CurrentPhase}에 맞지 않는 입력 무시: {input}");
        return false;
    }

    private void Log(string message)
    {
        Debug.Log($"[GameFlow] {message}");
        Logged?.Invoke(message);
    }

    private void Record(string line)
    {
        JudgmentRecorded?.Invoke(line);
    }
}
