using System.Collections.Generic;
using System.Text;
using UnityEngine;

// S1 임시 디버그 UI (RSP-30). 정식 UI(S2) 전까지 진행 흐름(GameFlow)을 버튼으로 진행하고 상태를 글자로 확인한다.
// Canvas·Prefab 없이 스크립트 하나로 두려고 IMGUI(OnGUI)로 만든다. 정식 UI가 생기면 이 파일과 씬의 컴포넌트를 지운다.
// 카드 내용은 플레이어가 아는 카드만 보여준다 (게임 컨셉 기획서 6장 정보 공개 규칙)
public class FlowDebugUI : MonoBehaviour
{
    private const int MaxLogLines = 20;

    // 화면 높이 720 기준으로 그린 뒤 실제 해상도에 맞춰 확대한다
    private const float ReferenceHeight = 720f;
    private const float PanelWidth = 380f;
    private const float Margin = 10f;

    [SerializeField] private GameFlowRunner _runner;

    private readonly List<string> _logLines = new List<string>();
    private GameFlow _flow;

    // GameFlowRunner가 먼저 Awake에서 Flow를 만들고, 모든 Awake 뒤 Start에서 라운드를 시작하므로 첫 로그부터 받는다
    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.Logged += AddLog;
    }

    private void OnDestroy()
    {
        _flow.Logged -= AddLog;
    }

    private void OnGUI()
    {
        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = ReferenceHeight - Margin * 2f;

        GUILayout.BeginArea(new Rect(Margin, Margin, PanelWidth, height), GUI.skin.box);
        DrawStatus();
        GUILayout.Space(Margin);
        DrawButtons();
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(width - PanelWidth - Margin, Margin, PanelWidth, height), GUI.skin.box);
        GUILayout.Label("진행 로그");
        foreach (string line in _logLines)
        {
            GUILayout.Label(line);
        }
        GUILayout.EndArea();
    }

    private void DrawStatus()
    {
        GUILayout.Label($"단계: {_flow.CurrentPhase} ({_flow.MatchIndex + 1}번째 판)");
        GUILayout.Label($"턴 플레이어: {DisplayNames.GetSideName(_flow.TurnPlayer)}");
        GUILayout.Label($"체력: 플레이어 {_flow.GetHealth(Side.Player)} / 딜러 {_flow.GetHealth(Side.Dealer)}");
        GUILayout.Label($"카드 더미: {_flow.RemainingDeckCount}장");
        GUILayout.Label($"뽑은 카드: {GetDrawnCardText()}");
        GUILayout.Label($"딜러 조합 칸: {GetSlotsText(Side.Dealer)}");
        GUILayout.Label($"내 조합 칸: {GetSlotsText(Side.Player)}");
    }

    // 버튼은 지금 단계에서 받는 입력만 켠다. 꺼진 버튼을 눌러도 GameFlow가 입력을 무시한다
    private void DrawButtons()
    {
        GamePhase phase = _flow.CurrentPhase;

        GUI.enabled = phase == GamePhase.Draw && _flow.TurnPlayer == Side.Player;
        if (GUILayout.Button("카드 뽑기"))
        {
            _flow.DrawCard();
        }

        GUI.enabled = phase == GamePhase.Distribute;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("가진다"))
        {
            _flow.Distribute(keep: true);
        }
        if (GUILayout.Button("준다"))
        {
            _flow.Distribute(keep: false);
        }
        GUILayout.EndHorizontal();

        GUI.enabled = phase == GamePhase.ShowResult;
        if (GUILayout.Button("다음 판"))
        {
            _flow.Next();
        }

        GUI.enabled = phase == GamePhase.Settlement;
        if (GUILayout.Button("다음 카드 셔플"))
        {
            _flow.Next();
        }

        GUI.enabled = true;
    }

    private string GetDrawnCardText()
    {
        Card drawn = _flow.DrawnCard;
        if (drawn == null)
        {
            return "없음";
        }

        return drawn.KnownByPlayer ? DisplayNames.GetShapeName(drawn.Shape) : "? (딜러만 앎)";
    }

    // 받은 순서대로, 플레이어가 모르는 카드는 ?로 표시한다
    private string GetSlotsText(Side side)
    {
        var text = new StringBuilder();
        foreach (Card card in _flow.GetCombinationSlots(side))
        {
            text.Append(card.KnownByPlayer ? DisplayNames.GetShapeName(card.Shape) : "?");
            text.Append(' ');
        }
        return text.ToString();
    }

    private void AddLog(string message)
    {
        _logLines.Add(message);
        if (_logLines.Count > MaxLogLines)
        {
            _logLines.RemoveAt(0);
        }
    }
}
