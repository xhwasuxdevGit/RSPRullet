using System;
using System.Collections.Generic;

// 1라운드 튜토리얼 안내의 지금 상태. 진행 흐름(GameFlow)의 단계 변경 알림을 받아 띄울 안내를 정하고, 이미 띄운 단계를 기록한다
// (튜토리얼(1라운드) 세부 기획서 6-3장 TU-F1~F3).
// GameFlowRunner가 만들고, 말풍선 화면은 지금 안내와 클릭 대기 여부를 읽고 클릭을 넘긴다.
// GameFlowRunner는 클릭을 기다리는 동안 자동 진행 화면을 넘기지 않는다.
// 라운드마다 씬을 다시 불러와 새로 만들므로, 응급약 복용이나 새 게임으로 1라운드를 다시 하면 기록이 비어 안내가 처음부터 나온다.
// 카드 셔플은 화면에서 멈추지 않고 한 번에 첫 판까지 진행되어 카드풀·선 정하기·딜러 분배 안내가 한꺼번에 나온다.
// 그래서 클릭 대기 안내가 떠 있는 동안 새로 나온 안내는 대기열에 넣고, 클릭할 때마다 하나씩 띄운다
public class TutorialGuide
{
    private readonly GameFlow _flow;
    private readonly TutorialSettings _settings;
    private readonly HashSet<TutorialStep> _shownSteps = new HashSet<TutorialStep>();
    private readonly Queue<TutorialGuideEntry> _pendingEntries = new Queue<TutorialGuideEntry>();

    // 마지막으로 알림을 받은 단계. 진행 흐름(GameFlow)은 카드 셔플 단계를 알리지 않아서,
    // 정산 다음 알림의 이전 단계가 정산이 아니라 카드 셔플로 온다. 그래서 정산에서 넘어갔는지는 이 값으로 확인한다
    private GamePhase _lastPhase = GamePhase.RoundStart;

    // 지금 말풍선에 띄운 안내. 없으면 null이다
    public TutorialGuideEntry CurrentEntry { get; private set; }

    // 지금 띄운 안내가 플레이어의 클릭을 기다리는 중인지. 말풍선의 클릭 아이콘이 이 값을 따른다.
    // 클릭 대기 안내는 말풍선과 아이콘이 함께 뜨고, 클릭(Confirm)하면 함께 사라지며 대기열의 다음 안내가 뜬다.
    // 클릭하기 전에 단계가 바뀌어도 그 안내가 남아 있는 동안은 계속 기다린다
    public bool IsWaitingForClick { get; private set; }

    // 지금 안내나 클릭 대기 여부가 바뀌었을 때 보낸다
    public event Action Changed;

    // 진행 흐름과 함께 만들어 함께 사라지므로 구독을 해제하지 않는다
    public TutorialGuide(GameFlow flow, TutorialSettings settings)
    {
        _flow = flow;
        _settings = settings;
        _flow.PhaseChanged += OnPhaseChanged;
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        GamePhase lastPhase = _lastPhase;
        _lastPhase = next;
        if (_flow.RoundNumber != TutorialGuideRules.TutorialRoundNumber) { return; }

        bool isChanged = false;
        if (TutorialGuideRules.ShouldClearGuide(CurrentEntry?.Step, lastPhase, next))
        {
            _pendingEntries.Clear();
            isChanged = Show(null);
        }

        TutorialGuideEntry newEntry = FindNewEntry(next);
        if (newEntry != null)
        {
            _shownSteps.Add(newEntry.Step);
            if (IsWaitingForClick) { _pendingEntries.Enqueue(newEntry); }
            else { isChanged |= Show(newEntry); }
        }

        if (isChanged) { Changed?.Invoke(); }
    }

    // 플레이어가 안내를 읽고 클릭했다. 말풍선과 클릭 아이콘을 함께 지우고, 대기열에 안내가 있으면 다음 안내를 띄운다.
    // 클릭을 기다리는 중이 아니면 무시한다
    public void Confirm()
    {
        if (!IsWaitingForClick) { return; }

        Show(_pendingEntries.Count > 0 ? _pendingEntries.Dequeue() : null);
        Changed?.Invoke();
    }

    // 말풍선에 띄울 안내를 바꾼다. 없으면 null. 바뀐 것이 있으면 true를 돌려준다
    private bool Show(TutorialGuideEntry entry)
    {
        bool isWaiting = entry != null && entry.WaitsForClick;
        if (entry == CurrentEntry && isWaiting == IsWaitingForClick) { return false; }

        CurrentEntry = entry;
        IsWaitingForClick = isWaiting;
        return true;
    }

    // 이번 화면에서 처음 띄울 안내. 이미 띄운 단계거나 안내가 없는 화면이면 null이다
    private TutorialGuideEntry FindNewEntry(GamePhase next)
    {
        TutorialStep? step = TutorialGuideRules.GetStep(next, _flow.Table.TurnPlayer, _flow.ShuffleCount, HasPlayerWon());
        if (step == null || _shownSteps.Contains(step.Value)) { return null; }

        return _settings.GetEntry(step.Value);
    }

    private bool HasPlayerWon()
    {
        return _flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
    }
}
