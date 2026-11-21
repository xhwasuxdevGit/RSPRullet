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
    [SerializeField] private TutorialSettings _tutorialSettings;

    // 판 결과를 보여 주는 중인지와 남은 시간
    private bool _isShowingMatchResult;
    private float _matchResultTimeLeft;

    // 딜러가 분배하기 전에 기다리는 중인지와 남은 시간
    private bool _isDealerThinking;
    private float _dealerThinkingTimeLeft;

    // 딜러가 아이템을 쓴 뒤 기다리는 중인지와 남은 시간
    private bool _isDealerItemWaiting;
    private float _dealerItemWaitTimeLeft;

    // 라운드를 넘어 이어지는 진행 상태 (라운드와 게임 진행 세부 기획서 4-2장 원칙).
    // static 값은 씬을 다시 불러와도 남아 있어 다음 라운드로 이어지고, Play를 새로 시작하면 처음 값(1라운드)으로 돌아간다.
    // (Project Settings > Editor > Enter Play Mode Settings를 바꾸면 Play를 새로 시작해도 값이 남을 수 있다)
    private static RoundProgress _progress = new RoundProgress();

    public GameFlow Flow { get; private set; }

    // 1라운드 튜토리얼 안내. 말풍선 화면은 지금 안내를 읽고 클릭을 넘기며, 이 쪽은 클릭을 기다리는 동안 자동 진행 화면을 넘기지 않는다
    public TutorialGuide Guide { get; private set; }

    // 라운드가 끝났을 때 하단 버튼 대신 게임 클리어·게임 오버 팝업을 보일지 정한다 (UI·UX 화면 흐름 세부 기획서 3-1장 종료-03·04)
    public bool IsFinalRound => _progress.IsFinalRound;

    // 게임 클리어·게임 오버 팝업에 보여 줄 응급약 복용 횟수 (라운드와 게임 진행 세부 기획서 6장 라운드 종료 표시 내용)
    public int MedicineCount => _progress.MedicineCount;

    // 모든 Awake가 끝난 뒤 Start가 불리므로, 화면은 Awake에서 Flow.Logged를 구독하면 라운드 시작 로그부터 받는다
    private void Awake()
    {
        RoundSettings round = _roundSettings[_progress.RoundNumber - 1];
        Flow = new GameFlow(_ruleSettings, round, _dealerSettings, _itemSettings, new System.Random());
        Guide = new TutorialGuide(Flow, _tutorialSettings);
    }

    private void Start()
    {
        Flow.StartRound();
    }

    // [다음 라운드]: 라운드를 이겼고 마지막 라운드가 아니면 라운드 번호를 올리고 씬을 다시 불러온다
    // (라운드와 게임 진행 세부 기획서 8-2장 R-F5 다음 라운드)
    public bool StartNextRound()
    {
        bool playerWonRound = Flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
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

    // [응급약 복용]: 라운드를 졌고 마지막 라운드가 아니면 응급약 복용 횟수를 올리고 같은 라운드로 씬을 다시 불러온다.
    // 체력·아이템·시계 이월·카드 셔플은 새로 만드는 GameFlow가 라운드 시작 상태로 돌린다
    // (라운드와 게임 진행 세부 기획서 8-2장 R-F4 응급약 복용, 4-1장 값별 처리)
    public bool TakeMedicine()
    {
        bool playerWonRound = Flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
        if (Flow.CurrentPhase != GamePhase.RoundEnd || playerWonRound)
        {
            return false;
        }

        if (!_progress.TakeMedicine())
        {
            return false;
        }

        SceneManager.LoadScene(gameObject.scene.buildIndex);
        return true;
    }

    // [다시하기]: 마지막 라운드가 끝났으면 이겼든 졌든 1라운드·응급약 복용 0회로 돌리고 씬을 다시 불러온다
    // (라운드와 게임 진행 세부 기획서 8-2장 R-F1 새 게임, 3장 게임 진행 흐름)
    public bool RestartGame()
    {
        if (Flow.CurrentPhase != GamePhase.RoundEnd || !_progress.IsFinalRound)
        {
            return false;
        }

        _progress.StartNewGame();
        SceneManager.LoadScene(gameObject.scene.buildIndex);
        return true;
    }

    private void Update()
    {
        UpdateDealerThinking();
        UpdateDealerItemWait();
        UpdateMatchResult();
    }

    // 딜러는 대기 시간이 지나야 뽑아 둔 카드를 분배한다 (연출·아트·사운드 세부 기획서 7-3장).
    // 대기 중에는 켤 단계별 버튼이 없어 입력이 잠긴다 (PhaseButtonRules).
    // 이번 대기에 튜토리얼 안내를 띄웠으면 대기 시간이 지나도 플레이어가 클릭할 때까지 기다린다 (튜토리얼(1라운드) 세부 기획서 2-3장)
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
        if (!TutorialGuideRules.CanAdvance(_dealerThinkingTimeLeft <= 0f, Guide.IsWaitingForClick))
        {
            return;
        }

        _isDealerThinking = false;
        Flow.PlayDealerTurn();
    }

    // 딜러가 쓴 아이템과 교환 표시가 보이도록, 아이템을 쓴 뒤 대기 시간이 지나야 다음으로 넘어간다
    // (UI·UX 화면 흐름 세부 기획서 3-1장 판-06). 대기 시간은 딜러 고민 중과 같은 범위를 쓴다 (연출·아트·사운드 세부 기획서 7-3장).
    // 대기 중에는 켤 단계별 버튼이 없어 입력이 잠긴다 (PhaseButtonRules)
    private void UpdateDealerItemWait()
    {
        if (!Flow.IsWaitingForDealerItems)
        {
            _isDealerItemWaiting = false;
            return;
        }

        if (!_isDealerItemWaiting)
        {
            _isDealerItemWaiting = true;
            _dealerItemWaitTimeLeft = Random.Range(
                _displayTimeSettings.DealerThinkingMinSeconds, _displayTimeSettings.DealerThinkingMaxSeconds);
            return;
        }

        _dealerItemWaitTimeLeft -= Time.deltaTime;
        if (_dealerItemWaitTimeLeft > 0f)
        {
            return;
        }

        _isDealerItemWaiting = false;
        Flow.FinishDealerItemPhase();
    }

    // 판 결과는 버튼 없이 표시 시간이 지나면 다음으로 넘어간다 (UI·UX 화면 흐름 세부 기획서 6-1장, 3-2장 읽는 시간).
    // 이번 판 결과에 튜토리얼 안내를 띄웠으면 표시 시간이 지나도 플레이어가 클릭할 때까지 기다린다 (튜토리얼(1라운드) 세부 기획서 2-3장)
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
        if (!TutorialGuideRules.CanAdvance(_matchResultTimeLeft <= 0f, Guide.IsWaitingForClick))
        {
            return;
        }

        _isShowingMatchResult = false;
        Flow.Next();
    }
}
