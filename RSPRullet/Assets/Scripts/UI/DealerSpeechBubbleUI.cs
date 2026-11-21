using UnityEngine;

// 딜러 상태 말풍선 (UI·UX 화면 흐름 세부 기획서 2-2장 딜러 영역, 3-1장 판-03).
// 딜러가 고민 중 대기일 때만 "고민 중…" 말풍선을 보여주고, 1라운드에서는 보여주지 않는다 (DealerSpeechBubbleRules).
// 튜토리얼 안내는 RSP-62에서 연결한다.
// 고민 중 대기는 진행 단계와 턴 플레이어로 정해지므로 진행 흐름(GameFlow)의 단계 변경 알림을 받을 때만 다시 정한다
public class DealerSpeechBubbleUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private GameObject _bubble;

    private GameFlow _flow;

    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.PhaseChanged += OnPhaseChanged;
        _bubble.SetActive(false);
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        _bubble.SetActive(DealerSpeechBubbleRules.IsThinkingShown(_flow.IsWaitingForDealer, _flow.RoundNumber));
    }
}
