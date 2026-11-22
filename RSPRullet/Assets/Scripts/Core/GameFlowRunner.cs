using UnityEngine;
using UnityEngine.SceneManagement;

// 씬에서 설정 에셋을 받아 진행 흐름(GameFlow)을 만들고 라운드를 시작한다.
// 화면은 Flow로 상태를 읽고 입력을 넘긴다. 연출 대기 시간이 생기면 이 쪽이나 화면 쪽에서 맡는다
// 화면의 Awake보다 먼저 Flow를 만들도록 실행 순서를 앞당긴다
// 다음 라운드는 씬을 다시 불러와 시작한다. 화면이 Awake에서 새 Flow를 다시 구독하고 자기 표시 상태도 처음부터 만들기 때문이다
[DefaultExecutionOrder(-1)]
public class GameFlowRunner : MonoBehaviour
{
    // 게임 씬을 떠날 때 불러올 씬 이름. 빌드 목록(File > Build Profiles)에 같은 이름의 씬이 있어야 한다 (RSP-80)
    private const string TitleSceneName = "Title";
    private const string EndingSceneName = "Ending";

    [SerializeField] private RuleSettings _ruleSettings;

    // 1·2·3라운드 순서로 넣는다. 지금 라운드 번호에 맞는 설정으로 GameFlow를 만든다
    [SerializeField] private RoundSettings[] _roundSettings = new RoundSettings[RoundProgress.RoundCount];
    [SerializeField] private DealerSettings _dealerSettings;
    [SerializeField] private ItemSettings _itemSettings;
    [SerializeField] private DisplayTimeSettings _displayTimeSettings;
    [SerializeField] private TutorialSettings _tutorialSettings;

    // 진행 안내 배너가 떠 있는 동안에는 자동 진행 남은 시간을 줄이지 않고, 꺼진 뒤부터 센다 (RSP-86)
    [SerializeField] private BannerUI _banner;

    // 응급약 복용은 효과음을 재생한 뒤, 다시하기는 화면을 어둡게 한 뒤 씬을 다시 불러온다 (RSP-91)
    [SerializeField] private ScreenFadeUI _screenFade;

    // 씬을 다시 불러오기 전에 배경음악 위치를 남겨, 다음 라운드 씬이 이어서 재생하게 한다 (RSP-74)
    [SerializeField] private SoundPlayer _sound;

    // 카드풀을 공개하는 중인지와 남은 시간
    private bool _isShowingCardPool;
    private float _cardPoolTimeLeft;

    // 선 정하기 결과를 보여 주는 중인지와 남은 시간
    private bool _isShowingDecideFirst;
    private float _decideFirstTimeLeft;

    // 판 결과를 보여 주는 중인지와 남은 시간
    private bool _isShowingMatchResult;
    private float _matchResultTimeLeft;

    // 정산 결과를 보여 주는 중인지와 남은 시간
    private bool _isShowingSettlement;
    private float _settlementTimeLeft;

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

    // 다음 라운드·응급약 복용·다시하기로 게임 씬을 다시 불러오는 중인지 (RSP-80).
    // 다시 불러온 씬에서는 1라운드 시작 전 딜러 대사를 띄우지 않는다. 인트로에서 들어오거나 Play를 바로 시작하면 false라 대사를 띄운다
    private static bool _isReloadingScene;

    // 이번 씬에서 1라운드 시작 배너 뒤에 딜러 대사를 띄울지 (RSP-80)
    private bool _showsPrologue;

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

        _showsPrologue = !_isReloadingScene && _progress.RoundNumber == TutorialGuideRules.TutorialRoundNumber;
        _isReloadingScene = false;
    }

    // 게임 씬에 처음 들어온 1라운드면 라운드를 시작하기 전에 딜러 대사를 안내 대기열에 먼저 넣는다.
    // 말풍선은 라운드 시작 배너가 사라진 뒤 보이고, 카드풀 안내는 대사 뒤에 이어진다.
    // 카드풀 공개는 안내 클릭을 기다리는 동안 넘어가지 않으므로 대사를 다 넘길 때까지 카드풀이 놓인 테이블에서 멈춘다 (UpdateCardPool)
    private void Start()
    {
        if (_showsPrologue) { Guide.ShowPrologue(); }

        Flow.StartRound();
    }

    // [다음 라운드]: 라운드를 이겼고 마지막 라운드가 아니면 라운드 번호를 올리고 씬을 다시 불러온다
    // (라운드와 게임 진행 세부 기획서 8-2장 R-F5 다음 라운드).
    // [다음 라운드] 버튼은 라운드 전환 화면(ScreenFadeUI)이 화면을 어둡게 하고 안내를 닫은 뒤에 켜지므로 바로 불러온다 (RSP-91)
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

        ReloadScene();
        return true;
    }

    // [응급약 복용]: 라운드를 졌고 마지막 라운드가 아니면 응급약 복용 횟수를 올리고 같은 라운드로 씬을 다시 불러온다.
    // 체력·아이템·시계 이월·카드 셔플은 새로 만드는 GameFlow가 라운드 시작 상태로 돌린다.
    // [응급약 복용] 버튼은 라운드 전환 화면이 화면을 어둡게 한 뒤에 켜지므로, 약 먹는 효과음만 재생하고 불러온다 (RSP-91)
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

        _screenFade.PlayMedicineThen(ReloadScene);
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
        _screenFade.FadeOutThen(ReloadScene);
        return true;
    }

    // [메인 메뉴로]·[타이틀로]: 1라운드·응급약 복용 0회로 돌리고 화면을 어둡게 한 뒤 타이틀 씬을 불러온다 (RSP-80).
    // 설정 팝업의 [타이틀로]는 라운드 도중에도 누를 수 있으므로 진행 단계를 확인하지 않는다
    public void GoToTitle()
    {
        LeaveGame(TitleSceneName);
    }

    // [엔딩으로]: 마지막 라운드를 이겼으면 1라운드·응급약 복용 0회로 돌리고 화면을 어둡게 한 뒤 엔딩 씬을 불러온다 (RSP-80)
    public bool GoToEnding()
    {
        bool playerWonRound = Flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
        if (Flow.CurrentPhase != GamePhase.RoundEnd || !_progress.IsFinalRound || !playerWonRound)
        {
            return false;
        }

        LeaveGame(EndingSceneName);
        return true;
    }

    // 게임 씬을 떠나면 다음에 게임 씬으로 돌아올 때 새 게임으로 시작해야 하므로 진행 상태를 먼저 돌린다
    private void LeaveGame(string sceneName)
    {
        _progress.StartNewGame();
        _screenFade.FadeOutThen(() => SceneManager.LoadScene(sceneName));
    }

    private void ReloadScene()
    {
        _isReloadingScene = true;
        _sound.KeepBgmPosition();
        SceneManager.LoadScene(gameObject.scene.buildIndex);
    }

    private void Update()
    {
        UpdateCardPool();
        UpdateDecideFirst();
        UpdateDealerThinking();
        UpdateDealerItemWait();
        UpdateMatchResult();
        UpdateSettlement();
    }

    // 카드 셔플의 카드풀 공개는 버튼 없이 표시 시간이 지나면 시작 카드를 나눠 카드 섞기 연출로 넘어간다
    // (연출·아트·사운드 세부 기획서 3-1장 카드 셔플 연출, UI·UX 화면 흐름 세부 기획서 3-2장 읽는 시간).
    // 카드 섞기 연출이 끝났을 때의 진행은 카드 셔플 화면(CardShuffleUI)이 넣는다.
    // 카드풀 안내나 튜토리얼 종료 안내가 클릭을 기다리면 표시 시간이 지나도 플레이어가 클릭할 때까지 기다린다 (튜토리얼(1라운드) 세부 기획서 2-3장)
    private void UpdateCardPool()
    {
        if (!Flow.IsShowingCardPool)
        {
            _isShowingCardPool = false;
            return;
        }

        if (!_isShowingCardPool)
        {
            _isShowingCardPool = true;
            _cardPoolTimeLeft = _displayTimeSettings.CardPoolSeconds;
            return;
        }

        if (_banner.IsShowing) { return; }

        _cardPoolTimeLeft -= Time.deltaTime;
        if (!TutorialGuideRules.CanAdvance(_cardPoolTimeLeft <= 0f, Guide.IsWaitingForClick))
        {
            return;
        }

        _isShowingCardPool = false;
        Flow.Next();
    }

    // 선 정하기 결과는 버튼 없이 표시 시간이 지나면 첫 번째 판으로, 비겼으면 카드풀 공개로 넘어간다.
    // 선 정하기 안내가 클릭을 기다리면 표시 시간이 지나도 플레이어가 클릭할 때까지 기다린다 (튜토리얼(1라운드) 세부 기획서 2-3장)
    private void UpdateDecideFirst()
    {
        if (Flow.CurrentPhase != GamePhase.DecideFirst)
        {
            _isShowingDecideFirst = false;
            return;
        }

        if (!_isShowingDecideFirst)
        {
            _isShowingDecideFirst = true;
            _decideFirstTimeLeft = _displayTimeSettings.DecideFirstSeconds;
            return;
        }

        if (_banner.IsShowing) { return; }

        _decideFirstTimeLeft -= Time.deltaTime;
        if (!TutorialGuideRules.CanAdvance(_decideFirstTimeLeft <= 0f, Guide.IsWaitingForClick))
        {
            return;
        }

        _isShowingDecideFirst = false;
        Flow.Next();
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

        if (_banner.IsShowing) { return; }

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

        if (_banner.IsShowing) { return; }

        _matchResultTimeLeft -= Time.deltaTime;
        if (!TutorialGuideRules.CanAdvance(_matchResultTimeLeft <= 0f, Guide.IsWaitingForClick))
        {
            return;
        }

        _isShowingMatchResult = false;
        Flow.Next();
    }

    // 정산 결과도 버튼 없이 표시 시간이 지나면 라운드 종료 또는 [다음 카드 셔플] 대기로 넘어간다
    // (UI·UX 화면 흐름 세부 기획서 3-1장 정산-01, 3-2장 읽는 시간). 표시 중에는 켤 단계별 버튼이 없어 입력이 잠긴다 (PhaseButtonRules).
    // 정산 안내가 클릭을 기다리면 표시 시간이 지나도 플레이어가 클릭할 때까지 기다린다 (튜토리얼(1라운드) 세부 기획서 2-3장)
    private void UpdateSettlement()
    {
        if (Flow.CurrentPhase != GamePhase.Settlement)
        {
            _isShowingSettlement = false;
            return;
        }

        // 정산 단계에 막 들어와 카드 정산 배너를 보여 주는 중이면, 배너가 꺼진 뒤 정산 결과를 반영하게 한다.
        // 결과를 반영한 뒤부터 정산 표시 시간을 센다
        if (Flow.IsWaitingSettlementReveal)
        {
            if (!_banner.IsShowing)
            {
                // 정산 결과가 판정 기록에 적히는 순간 계산대 소리를 낸다 (RSP-74)
                _sound.PlayCashRegister();
                Flow.Next();
            }

            return;
        }

        if (!_isShowingSettlement)
        {
            _isShowingSettlement = true;
            _settlementTimeLeft = _displayTimeSettings.SettlementSeconds;
            return;
        }

        if (_banner.IsShowing) { return; }

        _settlementTimeLeft -= Time.deltaTime;
        if (!TutorialGuideRules.CanAdvance(_settlementTimeLeft <= 0f, Guide.IsWaitingForClick))
        {
            return;
        }

        _isShowingSettlement = false;
        Flow.Next();
    }
}
