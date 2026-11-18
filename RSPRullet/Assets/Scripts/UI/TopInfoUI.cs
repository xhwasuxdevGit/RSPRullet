using TMPro;
using UnityEngine;

// 화면 맨 위 상단 정보 UI: 라운드, 카드 섞기 번호, 몇 번째 판(정산), 턴 플레이어 (UI·UX 화면 흐름 세부 기획서 5-1장 상단 정보 UI).
// 시계 사용 표시(UI·UX 화면 흐름 세부 기획서 5-2장)는 아이템 이슈(S3)에서 붙인다.
// 진행 흐름(GameFlow)에 변경 이벤트가 없어 매 프레임 상태를 읽고, 값이 바뀌었을 때만 문구를 다시 만든다
public class TopInfoUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private TMP_Text _infoText;

    private GameFlow _flow;

    // 마지막으로 화면에 적용한 값. 셔플 번호를 0으로 시작해 첫 Update에서 반드시 적용된다 (StartRound 이후에는 1 이상)
    private int _shownShuffleCount;
    private bool _shownIsSettlement;
    private int _shownMatchIndex;
    private Side _shownTurnPlayer;

    private void Awake()
    {
        _flow = _runner.Flow;
    }

    private void Update()
    {
        int shuffleCount = _flow.ShuffleCount;
        GamePhase phase = _flow.CurrentPhase;
        bool isSettlement = phase == GamePhase.Settlement;
        int matchIndex = _flow.MatchIndex;
        Side turnPlayer = _flow.TurnPlayer;

        if (shuffleCount == _shownShuffleCount && isSettlement == _shownIsSettlement &&
            matchIndex == _shownMatchIndex && turnPlayer == _shownTurnPlayer)
        {
            return;
        }

        _shownShuffleCount = shuffleCount;
        _shownIsSettlement = isSettlement;
        _shownMatchIndex = matchIndex;
        _shownTurnPlayer = turnPlayer;

        _infoText.text = TopInfoRules.GetText(_flow.RoundNumber, shuffleCount, phase, matchIndex, turnPlayer);
    }
}
