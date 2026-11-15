using UnityEngine;

// 씬에서 설정 에셋을 받아 진행 흐름(GameFlow)을 만들고 라운드를 시작한다.
// 화면(디버그 UI, 이후 정식 UI)은 Flow로 상태를 읽고 입력을 넘긴다. 연출 대기 시간이 생기면 이 쪽이나 화면 쪽에서 맡는다
// 화면의 Awake보다 먼저 Flow를 만들도록 실행 순서를 앞당긴다
[DefaultExecutionOrder(-1)]
public class GameFlowRunner : MonoBehaviour
{
    [SerializeField] private RuleSettings _ruleSettings;
    [SerializeField] private RoundSettings _roundSettings;

    public GameFlow Flow { get; private set; }

    // 모든 Awake가 끝난 뒤 Start가 불리므로, 화면은 Awake에서 Flow.Logged를 구독하면 라운드 시작 로그부터 받는다
    private void Awake()
    {
        Flow = new GameFlow(_ruleSettings, _roundSettings, new System.Random());
    }

    private void Start()
    {
        Flow.StartRound();
    }
}
