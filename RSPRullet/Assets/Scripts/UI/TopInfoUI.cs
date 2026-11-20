using TMPro;
using UnityEngine;

// 화면 맨 위 상단 정보 UI: 라운드, 카드 섞기 번호, 몇 번째 판(정산), 턴 플레이어 (UI·UX 화면 흐름 세부 기획서 5-1장 상단 정보 UI).
// 시계 사용 표시(UI·UX 화면 흐름 세부 기획서 5-2장)는 아이템 이슈(S3)에서 붙인다.
// 카드 섞기 번호·판 번호·턴 플레이어는 진행 단계가 바뀔 때 함께 바뀌므로 진행 흐름(GameFlow)의 단계 변경 알림을 받을 때만 문구를 다시 만든다
public class TopInfoUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private TMP_Text _infoText;

    private GameFlow _flow;

    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.PhaseChanged += OnPhaseChanged;
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        _infoText.text = TopInfoRules.GetText(_flow.RoundNumber, _flow.ShuffleCount, next, _flow.MatchIndex, _flow.Table.TurnPlayer);
    }
}
