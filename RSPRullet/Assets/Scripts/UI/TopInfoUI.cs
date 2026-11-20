using TMPro;
using UnityEngine;

// 화면 맨 위 상단 정보 UI: 라운드, 카드 섞기 번호, 몇 번째 판(정산), 턴 플레이어 (UI·UX 화면 흐름 세부 기획서 5-1장 상단 정보 UI)
// 와 시계를 쓴 쪽 (UI·UX 화면 흐름 세부 기획서 5-2장 시계 사용 표시).
// 카드 섞기 번호·판 번호·턴 플레이어는 진행 단계가 바뀔 때 함께 바뀌므로 진행 흐름(GameFlow)의 단계 변경 알림으로 문구를 다시 만든다.
// 시계는 아이템 단계 안에서 쓰여 단계가 바뀌지 않으므로 아이템 사용 알림도 받는다
public class TopInfoUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private TMP_Text _infoText;

    // 시계 사용 문구의 강조색
    [SerializeField] private Color _clockColor = new Color(1f, 0.8f, 0.3f);

    // 카드 섞기 번호(짙은 황금색)와 턴 플레이어 이름(플레이어 초록, 딜러 빨강)의 색
    [SerializeField] private Color _shuffleColor = new Color(0.85f, 0.65f, 0.15f);
    [SerializeField] private Color _playerTurnColor = new Color(0.4f, 0.85f, 0.4f);
    [SerializeField] private Color _dealerTurnColor = new Color(0.95f, 0.35f, 0.3f);

    private GameFlow _flow;

    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.PhaseChanged += OnPhaseChanged;
        _flow.ItemUsed += Refresh;
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
        _flow.ItemUsed -= Refresh;
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        Refresh();
    }

    private void Refresh()
    {
        GamePhase phase = _flow.CurrentPhase;
        string text = TopInfoRules.GetText(_flow.RoundNumber, _flow.ShuffleCount, phase, _flow.MatchIndex, _flow.Table.TurnPlayer,
            ColorUtility.ToHtmlStringRGB(_shuffleColor), ColorUtility.ToHtmlStringRGB(_playerTurnColor), ColorUtility.ToHtmlStringRGB(_dealerTurnColor));

        // 시계는 턴 플레이어만 쓰므로 한 판에 한쪽만 쓴다
        foreach (Side side in new[] { Side.Player, Side.Dealer })
        {
            if (ItemUseDisplayRules.IsShown(ItemType.Clock, phase) && _flow.Items.HasUsedItem(side, ItemType.Clock))
            {
                text = TopInfoRules.AppendClockText(text, side, ColorUtility.ToHtmlStringRGB(_clockColor));
            }
        }

        _infoText.text = text;
    }
}
