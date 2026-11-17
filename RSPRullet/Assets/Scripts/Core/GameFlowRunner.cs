using UnityEngine;

// 씬에서 설정 에셋을 받아 진행 흐름(GameFlow)을 만들고 라운드를 시작한다.
// 화면(디버그 UI, 이후 정식 UI)은 Flow로 상태를 읽고 입력을 넘긴다. 연출 대기 시간이 생기면 이 쪽이나 화면 쪽에서 맡는다
// 화면의 Awake보다 먼저 Flow를 만들도록 실행 순서를 앞당긴다
[DefaultExecutionOrder(-1)]
public class GameFlowRunner : MonoBehaviour
{
    [SerializeField] private RuleSettings _ruleSettings;
    [SerializeField] private RoundSettings _roundSettings;
    [SerializeField] private DealerSettings _dealerSettings;
    [SerializeField] private DisplayTimeSettings _displayTimeSettings;

    // 판-07 승패 판정 표시를 기다리는 중인지와 남은 시간
    private bool _isShowingMatchResult;
    private float _matchResultTimeLeft;

    public GameFlow Flow { get; private set; }

    // 모든 Awake가 끝난 뒤 Start가 불리므로, 화면은 Awake에서 Flow.Logged를 구독하면 라운드 시작 로그부터 받는다
    private void Awake()
    {
        Flow = new GameFlow(_ruleSettings, _roundSettings, _dealerSettings, new System.Random());
    }

    private void Start()
    {
        Flow.StartRound();
    }

    // 판-07은 버튼 없이 표시 시간이 지나면 넘어간다 (UI·UX 화면 흐름 세부 기획서 6-1장, 3-2장 읽는 시간).
    // Next() 뒤 딜러 차례 판이 바로 판정까지 진행되면 다시 판 결과 확인 단계가 되므로, 다음 프레임에 시간을 새로 잰다
    private void Update()
    {
        if (Flow.CurrentPhase != GamePhase.ShowResult)
        {
            _isShowingMatchResult = false;
            return;
        }

        if (!_isShowingMatchResult)
        {
            _isShowingMatchResult = true;
            _matchResultTimeLeft = _displayTimeSettings.MatchResultSeconds;
            return;
        }

        _matchResultTimeLeft -= Time.deltaTime;
        if (_matchResultTimeLeft > 0f)
        {
            return;
        }

        _isShowingMatchResult = false;
        Flow.Next();
    }
}
