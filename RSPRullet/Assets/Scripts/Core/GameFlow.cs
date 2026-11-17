using System;
using System.Collections.Generic;
using UnityEngine;

// 라운드 하나의 진행 흐름. 진행 단계(PhaseMachine), 셔플 테이블(ShuffleTable), 체력(HealthState), 딜러 AI(DealerAI)를 한 흐름으로 묶는다.
// 플레이어 입력이 필요한 지점(뽑기, 분배, 판 결과 확인, 정산 확인)과 딜러 차례 시작(고민 중 대기)에서만 멈추고, 나머지 단계는 시간 없이 바로 진행한다.
// 연출 대기 시간은 화면 쪽이 맡는다 (기술 문서 04. 진행 단계 관리 3장 설계 의도).
// 화면을 참조하지 않으며, 화면은 상태 조회와 Logged 이벤트만 쓴다.
// 라운드마다 새로 만든다. S1 범위라 아이템 단계는 바로 통과하고, 시계 이월은 항상 없음으로 둔다.
public class GameFlow
{
    private readonly System.Random _random;
    private readonly PhaseMachine _phaseMachine = new PhaseMachine();
    private readonly HealthState _health;
    private readonly DealerAI _dealer;

    // 카드 셔플마다 새로 만든다. StartRound 이후에 값이 있다
    private ShuffleTable _table;

    // 정산 결과 라운드가 끝나는지. 정산 확인 입력(Next)에서 진행 단계로 넘긴다
    private bool _isRoundOverAfterSettlement;

    // 진행 로그 한 줄 (화면의 진행 로그 표시용)
    public event Action<string> Logged;

    public GamePhase CurrentPhase => _phaseMachine.CurrentPhase;
    public int MatchIndex => _phaseMachine.MatchIndex;
    public Side TurnPlayer => _table.TurnPlayer;
    public Card DrawnCard => _table.DrawnCard;
    public int RemainingDeckCount => _table.RemainingDeckCount;

    // 카드풀 정보의 "n장 → n장" (UI·UX 화면 흐름 세부 기획서 5-4장 카드풀).
    // 판 시작 때 더미 장수를 기록해 두므로, 정산 중에는 두 번째 판의 값("3장 → 1장")이 그대로 남는다
    public int MatchStartDeckCount { get; private set; }
    public int MatchEndDeckCount => MatchStartDeckCount - CardsPerMatch;

    // 한 판에 더미에서 나가는 카드: 뽑은 카드 1장 + 남은 자리에 들어가는 더미의 다음 카드 1장 (ShuffleTable.Distribute)
    private const int CardsPerMatch = 2;

    // 딜러가 턴 플레이어인 판에서, 딜러가 뽑은 카드를 확인하며 분배를 고민하는 중인지 (UI·UX 화면 흐름 세부 기획서 3-1장 판-03).
    // 딜러는 판 시작 때 먼저 뽑아 두므로 분배 단계에서 기다린다. 고민 중 대기 시간은 화면 쪽(GameFlowRunner)이 재고, 끝나면 PlayDealerTurn을 부른다
    public bool IsWaitingForDealer => CurrentPhase == GamePhase.Distribute && TurnPlayer == Side.Dealer;

    // 같은 시드의 난수를 넘기면 카드풀·더미 순서·딜러 선택이 같게 나와 테스트에서 진행을 재현할 수 있다
    public GameFlow(RuleSettings rules, RoundSettings round, DealerSettings dealerSettings, System.Random random)
    {
        _random = random;
        _health = new HealthState(rules, round.StartHealth);
        _dealer = new DealerAI(rules, dealerSettings, random);
    }

    public int GetHealth(Side side)
    {
        return _health.GetHealth(side);
    }

    public IReadOnlyList<Card> GetCombinationSlots(Side side)
    {
        return _table.GetCombinationSlots(side);
    }

    // 이번 카드 셔플의 카드풀 구성 (항상 공개되는 정보). 카드 셔플 동안 바뀌지 않는다
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
        Log($"뽑은 카드: {DisplayNames.GetShapeName(_table.DrawnCard.Shape)} (플레이어만 앎)");
        return true;
    }

    // 플레이어가 뽑은 카드를 가지거나(keep: true) 딜러에게 준다(keep: false). 이어서 판정까지 진행한다
    public bool Distribute(bool keep)
    {
        if (CurrentPhase != GamePhase.Distribute || TurnPlayer != Side.Player)
        {
            return Ignore(nameof(Distribute));
        }

        _table.Distribute(keep);
        Log(keep ? "플레이어가 카드를 가졌다" : "플레이어가 카드를 딜러에게 줬다");
        _phaseMachine.CompleteDistribute();
        ResolveMatch();
        return true;
    }

    // 딜러 차례 판에서 고민 중 대기가 끝나면 딜러가 이미 뽑아 둔 카드를 분배한 뒤 판정까지 진행한다
    public bool PlayDealerTurn()
    {
        if (!IsWaitingForDealer)
        {
            return Ignore(nameof(PlayDealerTurn));
        }

        // 분배하면 DrawnCard가 비므로 먼저 받아 둔다
        Card drawnCard = _table.DrawnCard;
        _dealer.TryDistribute(_table, MatchIndex);

        // 가졌는지 줬는지는 항상 공개되는 정보다. 카드 내용은 딜러만 안다
        IReadOnlyList<Card> dealerSlots = _table.GetCombinationSlots(Side.Dealer);
        bool dealerKept = dealerSlots[dealerSlots.Count - 1] == drawnCard;
        Log(dealerKept ? "딜러가 카드를 가졌다" : "딜러가 카드를 플레이어에게 줬다");
        _phaseMachine.CompleteDistribute();
        ResolveMatch();
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

    // 카드풀 생성 → 시작 카드 → 선 정하기. 선 정하기에서 비기면 카드풀부터 다시 만든다 (코어 규칙 상세 기획서 6장 예외 E1)
    private void StartShuffle()
    {
        bool isDecided;
        do
        {
            _table = new ShuffleTable(new CardDeck(CardPool.Create(_random), _random));
            _phaseMachine.CompleteCardShuffle();

            _table.DealStartCards();
            Log($"시작 카드: 플레이어 {GetSlotShapeName(Side.Player, 0)}, 딜러 {GetSlotShapeName(Side.Dealer, 0)}");
            _phaseMachine.CompleteStartCardDeal(hasClockCarryOver: false);

            isDecided = _table.DecideFirstTurnPlayer();
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

    // 판 시작. 플레이어 차례면 카드 뽑기 입력을 기다린다.
    // 딜러 차례면 딜러가 바로 뽑고, 뽑은 카드를 보여 주는 고민 중 대기 뒤의 PlayDealerTurn을 기다린다 (UI·UX 화면 흐름 세부 기획서 3-1장 판-03)
    private void BeginMatch()
    {
        // 딜러 차례면 아래에서 바로 뽑으므로 뽑기 전에 기록한다
        MatchStartDeckCount = _table.RemainingDeckCount;
        Log($"{MatchIndex + 1}번째 판: {DisplayNames.GetSideName(TurnPlayer)} 차례");
        if (TurnPlayer != Side.Dealer)
        {
            return;
        }

        _table.DrawForTurnPlayer();
        _phaseMachine.CompleteDraw();
        Log("딜러가 카드를 뽑았다 (딜러만 앎)");
    }

    // 아이템 단계 통과 → 카드 공개 → 판정까지 진행하고, 판 결과 확인 입력을 기다린다
    private void ResolveMatch()
    {
        // S1은 아이템이 없어 턴 플레이어와 상대의 아이템 단계를 바로 끝낸다
        _phaseMachine.CompleteItemUse();
        _phaseMachine.CompleteItemUse();

        _table.RevealMatchCards();
        _phaseMachine.CompleteReveal();

        HandShape playerShape = GetLastSlotShape(Side.Player);
        HandShape dealerShape = GetLastSlotShape(Side.Dealer);
        MatchResult playerResult = _health.ApplyMatch(playerShape, dealerShape);
        Log($"{MatchIndex + 1}번째 판: {DisplayNames.GetShapeName(playerShape)} vs {DisplayNames.GetShapeName(dealerShape)} → {GetResultName(playerResult)} " +
            $"(체력 플레이어 {_health.GetHealth(Side.Player)} / 딜러 {_health.GetHealth(Side.Dealer)})");
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

    // 남은 1장 공개 → 양쪽 조합 효과 동시 적용 (코어 규칙 상세 기획서 4-3장 정산 순서). 정산 확인 입력을 기다린다
    private void ApplySettlement()
    {
        Card remaining = _table.RevealRemainingCard();
        Log($"정산: 남은 카드 {DisplayNames.GetShapeName(remaining.Shape)} 공개");

        CombinationType playerCombination = JudgeCombination(Side.Player);
        CombinationType dealerCombination = JudgeCombination(Side.Dealer);
        _isRoundOverAfterSettlement = _health.ApplySettlement(playerCombination, dealerCombination);
        Log($"정산: 플레이어 {playerCombination}, 딜러 {dealerCombination} " +
            $"(체력 플레이어 {_health.GetHealth(Side.Player)} / 딜러 {_health.GetHealth(Side.Dealer)})");
    }

    private CombinationType JudgeCombination(Side side)
    {
        return CombinationRules.Judge(
            GetSlotShape(side, 0),
            GetSlotShape(side, 1),
            GetSlotShape(side, 2));
    }

    private void LogRoundEnd()
    {
        Side winner = _health.GetHealth(Side.Player) == GameRules.MinHealth ? Side.Dealer : Side.Player;
        Log($"라운드 종료: {DisplayNames.GetSideName(winner)} 승리");
    }

    private HandShape GetSlotShape(Side side, int index)
    {
        return _table.GetCombinationSlots(side)[index].Shape;
    }

    private HandShape GetLastSlotShape(Side side)
    {
        return GetSlotShape(side, _table.GetCombinationSlots(side).Count - 1);
    }

    private string GetSlotShapeName(Side side, int index)
    {
        return DisplayNames.GetShapeName(GetSlotShape(side, index));
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

    private static string GetResultName(MatchResult playerResult)
    {
        switch (playerResult)
        {
            case MatchResult.Win:
                return "플레이어 승리";
            case MatchResult.Lose:
                return "딜러 승리";
            default:
                return "무승부";
        }
    }
}
