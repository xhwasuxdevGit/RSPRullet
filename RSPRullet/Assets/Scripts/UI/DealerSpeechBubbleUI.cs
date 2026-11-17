using UnityEngine;

// 딜러 상태 말풍선 (UI·UX 화면 흐름 세부 기획서 2-2장 딜러 영역, 3-1장 판-03).
// 딜러가 고민 중 대기일 때만 "고민 중…" 말풍선을 보여준다. 1라운드 생략은 RSP-84, 튜토리얼 안내는 RSP-62에서 연결한다.
// 진행 흐름(GameFlow)에 변경 이벤트가 없어 매 프레임 상태를 읽고, 바뀌었을 때만 화면에 적용한다
public class DealerSpeechBubbleUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private GameObject _bubble;

    private GameFlow _flow;

    private void Awake()
    {
        _flow = _runner.Flow;
        _bubble.SetActive(false);
    }

    private void Update()
    {
        bool isThinking = _flow.IsWaitingForDealer;
        if (_bubble.activeSelf == isThinking)
        {
            return;
        }

        _bubble.SetActive(isThinking);
    }
}
