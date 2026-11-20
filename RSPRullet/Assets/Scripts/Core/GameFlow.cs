using System;
using System.Collections.Generic;
using UnityEngine;

// 라운드 하나의 진행 흐름. 진행 단계, 테이블 상태, 체력, 딜러 AI, 아이템 상태를 묶고, 플레이어 입력과 딜러 고민 중 대기에서만 멈춘다.
// 이 클래스는 흐름(단계 진행, 입력 확인, 지급·다시 섞기·아이템 단계를 시작하는 시점, 기록·로그를 남기는 시점)만 정한다.
// 상태 계산과 변경 알림은 상태를 가진 클래스(TableState, HealthState, ItemState)가 맡고, 이 클래스는 알림을 화면에 넘긴다.
// 아이템 단계 안의 진행(차례, 아이템 입력, 돋보기 선택 대기)과 지급은 아이템 진행(ItemPhaseFlow)이 맡는다.
// 시계 효과는 아직 없어 시계 이월은 항상 없다.
public class GameFlow
{
    private readonly System.Random _random;
    private readonly PhaseMachine _phaseMachine = new PhaseMachine();
    private readonly HealthState _health;
    private readonly DealerAI _dealer;
    private readonly ItemState _items;
    private readonly ItemPhaseFlow _itemPhase;
    
    // 카드 셔플마다 새로 만든다
    private TableState _table;

    // 정산 결과 라운드가 끝나는지. 정산 확인 입력(Next)에서 진행 단계로 넘긴다
    private bool _isRoundOverAfterSettlement;

    public event Action<string> Logged;
    public event Action<string> JudgmentRecorded;

    // 카드 셔플이 시작될 때마다 보낸다. 선 정하기가 비겨 다시 섞을 때도 보낸다
    public event Action JudgmentRecordCleared;
    
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

    public int RoundNumber { get; }
    public int ShuffleCount { get; private set; }
    
    public int MatchStartDeckCount => _table.MatchStartDeckCount;
    public int MatchEndDeckCount => _table.MatchEndDeckCount;
    
    public bool IsWaitingForDealer => CurrentPhase == GamePhase.Distribute && TurnPlayer == Side.Dealer;

    // 이번 판에 양쪽이 쓴 아이템과 돋보기 뒤 교환 여부
    public IReadOnlyList<(Side User, ItemType Item, bool Exchanged)> ItemUses => _items.ItemUses;

    // 플레이어가 돋보기로 상대 카드를 본 뒤 바꿀지 그대로 둘지 고르는 중인지. 아이템 단계 안의 대기라 진행 단계는 그대로다.
    public bool IsWaitingForMagnifierChoice => _itemPhase.IsWaitingForMagnifierChoice;
    
    public bool IsMatchCardSwapped => _table.IsMatchCardSwapped;

    // 같은 시드의 난수를 넘기면 카드풀·더미 순서·딜러 선택·아이템 지급이 같게 나와 테스트에서 진행을 재현할 수 있다
    public GameFlow(RuleSettings rules, RoundSettings round, DealerSettings dealerSettings, ItemSettings itemSettings, System.Random random)
    {
        _random = random;
        RoundNumber = round.RoundNumber;
        _health = new HealthState(rules, round.StartHealth);
        _dealer = new DealerAI(rules, dealerSettings, random);
        _items = new ItemState(round.ItemCapacity);
        _itemPhase = new ItemPhaseFlow(_phaseMachine, _items, round, itemSettings, random);

        // 진행 단계 관리, 체력 상태, 아이템 상태, 아이템 진행은 이 클래스가 만들어 함께 사라지므로 구독을 해제하지 않는다
        _phaseMachine.PhaseChanged += OnPhaseMachineChanged;
        _health.Changed += () => HealthChanged?.Invoke();
        _items.Changed += () => ItemsChanged?.Invoke();
        _itemPhase.ItemUsed += () => ItemUsed?.Invoke();
        _itemPhase.Logged += Log;
    }

    public int GetHealth(Side side) => _health.GetHealth(side);

    // 보유 아이템은 양쪽에 항상 공개된다. 화면은 읽기만 하고, 바꾸는 것은 이 클래스의 입력과 지급뿐이다
    public IReadOnlyList<ItemType> GetItems(Side side) => _items.GetItems(side);

    // [아이템 사용 완료]·[아이템 없음] 버튼 전환에 쓴다
    public bool HasUsableItem(Side side) => _items.HasUsableItem(side, TurnPlayer);

    public IReadOnlyList<Card> GetCombinationSlots(Side side) => _table.GetCombinationSlots(side);

    // 이번 카드 셔플의 카드풀 구성. 카드 셔플 동안 바뀌지 않는다
    public int GetPoolCardCount(HandShape shape) => _table.GetPoolCardCount(shape);

    // 라운드 시작 → 첫 번째 카드 셔플 → 첫 번째 판의 입력 대기 지점까지 진행한다
    public bool StartRound()
    {
        if (!_phaseMachine.CompleteRoundStart())
        {
            return false;
        }

        Log($"라운드 시작: 양쪽 체력 {_health.GetHealth(Side.Player)}");
        
        _itemPhase.SupplyStartItems();
        StartShuffle();
        return true;
    }
    
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
    
    public bool UseItem(ItemType item) => _itemPhase.UseItem(item) || Ignore($"{nameof(UseItem)}({item})");

    public bool ChooseMagnifierExchange(bool exchange) => _itemPhase.ChooseMagnifierExchange(exchange) || Ignore($"{nameof(ChooseMagnifierExchange)}({exchange})");
    
    public bool FinishItemPhase()
    {
        if (!_itemPhase.FinishPlayerPhase())
        {
            return Ignore(nameof(FinishItemPhase));
        }

        ResolveMatchIfItemPhaseFinished();
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
        
        _itemPhase.SupplyShuffleItems();

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
    
    private void CreateTable()
    {
        if (_table != null)
        {
            _table.Changed -= OnTableChanged;
        }

        _table = new TableState(new CardDeck(CardPool.Create(_random), _random));
        _table.Changed += OnTableChanged;
    }
    
    private void BeginMatch()
    {
        _itemPhase.BeginMatch();

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
    
    private void StartItemPhase()
    {
        _itemPhase.Start(_table);
        ResolveMatchIfItemPhaseFinished();
    }

    private void ResolveMatchIfItemPhaseFinished()
    {
        if (_itemPhase.IsFinished)
        {
            ResolveMatch();
        }
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

    private void OnPhaseMachineChanged(GamePhase previous, GamePhase next)
    {
        if (next == GamePhase.CardShuffle)
        {
            return;
        }

        PhaseChanged?.Invoke(previous, next);
    }

    private void OnTableChanged() => CardsChanged?.Invoke();

    // 항상 false를 돌려준다. 입력 메서드는 "받았으면 true, 아니면 Ignore"를 return Ignore(…) 또는 … || Ignore(…)로 쓴다
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

    private void Record(string line) => JudgmentRecorded?.Invoke(line);
}
