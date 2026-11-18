using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// 테이블 위 판정 기록: 선 정하기·판정·정산 결과를 한 줄씩 쌓아 보여 준다 (UI·UX 화면 흐름 세부 기획서 5-5장 판정 기록).
// 기획서는 2D 화면 UI로 정했지만, 이후 종이·기록지 에셋으로 바꾸려고 카드처럼 테이블 위에 놓인 3D 물체로 둔다 (RSP-41).
// 최근 결과는 글자 색 그대로, 지난 결과는 옅게(불투명도를 낮춰) 표시한다. 결과 하나가 여러 줄이어도 한 묶음으로 같은 농도를 쓴다.
// 한 카드 셔플의 결과는 5개(선 정하기 1, 판정 2, 정산 2)라 넘침 처리는 두지 않는다.
// 진행 흐름(GameFlow)의 판정 기록 이벤트로 결과를 받고, 카드 셔플이 시작될 때 비운다
public class JudgmentRecordUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private TMP_Text _recordText;

    // 지난 결과의 불투명도 (0~1). 최근 결과는 글자 색 그대로 보여 준다
    [SerializeField, Range(0f, 1f)] private float _pastLineAlpha = 0.7f;

    private readonly List<string> _lines = new List<string>();
    private readonly StringBuilder _builder = new StringBuilder();
    private GameFlow _flow;

    // GameFlowRunner가 먼저 Awake에서 Flow를 만들고, 모든 Awake 뒤 Start에서 라운드를 시작하므로 첫 선 정하기 줄부터 받는다
    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.JudgmentRecorded += AddLine;
        _flow.JudgmentRecordCleared += Clear;
        Refresh();
    }

    private void OnDestroy()
    {
        _flow.JudgmentRecorded -= AddLine;
        _flow.JudgmentRecordCleared -= Clear;
    }

    private void AddLine(string line)
    {
        _lines.Add(line);
        Refresh();
    }

    private void Clear()
    {
        _lines.Clear();
        Refresh();
    }

    private void Refresh()
    {
        // TextMeshPro의 <alpha> 태그는 16진수 두 자리 불투명도를 받는다
        string pastAlpha = Mathf.RoundToInt(_pastLineAlpha * 255f).ToString("X2");
        int lastIndex = _lines.Count - 1;

        _builder.Clear();
        for (int i = 0; i < _lines.Count; i++)
        {
            if (i > 0)
            {
                _builder.Append('\n');
            }

            _builder.Append(i == lastIndex ? "<alpha=#FF>" : $"<alpha=#{pastAlpha}>");
            // 결과 하나 안의 줄바꿈은 세로 탭(\v)으로 바꾼다. TextMeshPro는 \v를 줄바꿈으로 그리되 문단 간격(paragraphSpacing)을
            // 붙이지 않으므로, 결과 안의 두 줄은 붙이고 결과 사이(\n)만 벌릴 수 있다
            _builder.Append(_lines[i].Replace('\n', '\v'));
        }

        _recordText.text = _builder.ToString();
    }
}
