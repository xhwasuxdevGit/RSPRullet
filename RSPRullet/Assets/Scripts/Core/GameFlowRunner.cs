using UnityEngine;
using UnityEngine.SceneManagement;

// 씬에서 설정 에셋을 받아 진행 흐름(GameFlow)을 만들고 라운드를 시작한다.
// 화면은 Flow로 상태를 읽고 입력을 넘긴다. 연출 대기 시간이 생기면 이 쪽이나 화면 쪽에서 맡는다
// 화면의 Awake보다 먼저 Flow를 만들도록 실행 순서를 앞당긴다
// 다음 라운드는 씬을 다시 불러와 시작한다. 화면이 Awake에서 새 Flow를 다시 구독하고 자기 표시 상태도 처음부터 만들기 때문이다
[DefaultExecutionOrder(-1)]
public class GameFlowRunner : MonoBehaviour
{
    [SerializeField] private RuleSettings _ruleSettings;

    // 1·2·3라운드 순서로 넣는다. 지금 라운드 번호에 맞는 설정으로 GameFlow를 만든다
    [SerializeField] private RoundSettings[] _roundSettings = new RoundSettings[RoundProgress.RoundCount];
    [SerializeField] private DealerSettings _dealerSettings;
    [SerializeField] private ItemSettings _itemSettings;
    [SerializeField] private DisplayTimeSettings _displayTimeSettings;

    // 판 결과를 보여 주는 중인지와 남은 시간
    private bool _isShowingMatchResult;
    private float _matchResultTimeLeft;

    // 딜러가 분배하기 전에 기다리는 중인지와 남은 시간
    private bool _isDealerThinking;
    private float _dealerThinkingTimeLeft;

    // 라운드를 넘어 이어지는 진행 상태 (라운드와 게임 진행 세부 기획서 4-2장 원칙).
    // static 값은 씬을 다시 불러와도 남아 있어 다음 라운드로 이어지고, Play를 새로 시작하면 처음 값(1라운드)으로 돌아간다.
    // (Project Settings > Editor > Enter Play Mode Settings를 바꾸면 Play를 새로 시작해도 값이 남을 수 있다)
    private static RoundProgress _progress = new RoundProgress();

    public GameFlow Flow { get; private set; }

    // 라운드가 끝났을 때 [다음 라운드] 대신 [다시하기]를 보일지 정한다 (UI·UX 화면 흐름 세부 기획서 3-1장 종료-03·04)
    public bool IsFinalRound => _progress.IsFinalRound;

    // 모든 Awake가 끝난 뒤 Start가 불리므로, 화면은 Awake에서 Flow.Logged를 구독하면 라운드 시작 로그부터 받는다
    private void Awake()
    {
        RoundSettings round = _roundSettings[_progress.RoundNumber - 1];
        Flow = new GameFlow(_ruleSettings, round, _dealerSettings, _itemSettings, new System.Random());
    }

    private void Start()
    {
        Flow.StartRound();
    }

    // [다음 라운드]: 라운드를 이겼고 마지막 라운드가 아니면 라운드 번호를 올리고 씬을 다시 불러온다
    // (라운드와 게임 진행 세부 기획서 8-2장 R-F5 다음 라운드)
    public bool StartNextRound()
    {
        bool playerWonRound = Flow.GetHealth(Side.Player) > GameRules.MinHealth;
        if (Flow.CurrentPhase != GamePhase.RoundEnd || !playerWonRound)
        {
            return false;
        }

        if (!_progress.AdvanceRound())
        {
            return false;
        }

        SceneManager.LoadScene(gameObject.scene.buildIndex);
        return true;
    }

    private void Update()
    {
        UpdateDealerThinking();
        UpdateMatchResult();
    }

    // 딜러는 대기 시간이 지나야 뽑아 둔 카드를 분배한다 (연출·아트·사운드 세부 기획서 7-3장).
    // 대기 중에는 켤 단계별 버튼이 없어 입력이 잠긴다 (PhaseButtonRules)
    private void UpdateDealerThinking()
    {
        if (!Flow.IsWaitingForDealer)
        {
            _isDealerThinking = false;
            return;
        }

        if (!_isDealerThinking)
        {
            _isDealerThinking = true;
            _dealerThinkingTimeLeft = Random.Range(
                _displayTimeSettings.DealerThinkingMinSeconds, _displayTimeSettings.DealerThinkingMaxSeconds);
            return;
        }

        _dealerThinkingTimeLeft -= Time.deltaTime;
        if (_dealerThinkingTimeLeft > 0f)
        {
            return;
        }

        _isDealerThinking = false;
        Flow.PlayDealerTurn();
    }

    // 판 결과는 버튼 없이 표시 시간이 지나면 다음으로 넘어간다 (UI·UX 화면 흐름 세부 기획서 6-1장, 3-2장 읽는 시간)
    private void UpdateMatchResult()
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
