using UnityEngine;
using UnityEngine.UI;

// 하단 HUD의 단계별 버튼과 입력 잠금 (UI·UX 화면 흐름 세부 기획서 6-1장 단계별 버튼, 6-2장 입력 잠금).
// 진행 흐름(GameFlow)의 단계 변경 알림을 받을 때만 켤 버튼을 다시 정한다.
// 버튼을 고르는 턴 플레이어·체력은 단계가 바뀔 때 함께 정해지므로(라운드 종료의 체력은 판정·정산에서 먼저 바뀜) 단계 알림만 받는다.
public class PhaseButtonsUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;

    [Header("단계별 버튼")]
    [SerializeField] private Button _drawCardButton;
    [SerializeField] private Button _keepButton;
    [SerializeField] private Button _giveButton;
    [SerializeField] private Button _itemDoneButton;
    [SerializeField] private Button _noItemButton;
    [SerializeField] private Button _nextShuffleButton;
    [SerializeField] private Button _nextRoundButton;
    [SerializeField] private Button _takeMedicineButton;
    [SerializeField] private Button _restartButton;

    [Header("입력 잠금 중 함께 잠그는 버튼")]
    [SerializeField] private Button _ruleButton;
    [SerializeField] private Button _settingsButton;

    private GameFlow _flow;
    private (Button button, PhaseButton flag)[] _phaseButtons;

    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.PhaseChanged += OnPhaseChanged;
        _phaseButtons = new[]
        {
            (_drawCardButton, PhaseButton.DrawCard),
            (_keepButton, PhaseButton.Keep),
            (_giveButton, PhaseButton.Give),
            (_itemDoneButton, PhaseButton.ItemDone),
            (_noItemButton, PhaseButton.NoItem),
            (_nextShuffleButton, PhaseButton.NextShuffle),
            (_nextRoundButton, PhaseButton.NextRound),
            (_takeMedicineButton, PhaseButton.TakeMedicine),
            (_restartButton, PhaseButton.Restart)
        };

        _drawCardButton.onClick.AddListener(OnDrawCard);
        _keepButton.onClick.AddListener(OnKeep);
        _giveButton.onClick.AddListener(OnGive);
        _nextShuffleButton.onClick.AddListener(OnNextShuffle);
        _itemDoneButton.onClick.AddListener(OnFinishItemPhase);
        _noItemButton.onClick.AddListener(OnFinishItemPhase);

        // 라운드 진행 버튼은 S3 이슈에서 동작을 연결한다. 그 전까지는 보이기만 하고 눌리지 않는다
        ButtonLock.SetLocked(_nextRoundButton, true);
        ButtonLock.SetLocked(_takeMedicineButton, true);
        ButtonLock.SetLocked(_restartButton, true);
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
        _drawCardButton.onClick.RemoveListener(OnDrawCard);
        _keepButton.onClick.RemoveListener(OnKeep);
        _giveButton.onClick.RemoveListener(OnGive);
        _nextShuffleButton.onClick.RemoveListener(OnNextShuffle);
        _itemDoneButton.onClick.RemoveListener(OnFinishItemPhase);
        _noItemButton.onClick.RemoveListener(OnFinishItemPhase);
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        PhaseButton buttons = GetCurrentButtons();
        foreach ((Button button, PhaseButton flag) in _phaseButtons)
        {
            button.gameObject.SetActive((buttons & flag) != 0);
        }

        bool isLocked = PhaseButtonRules.IsInputLocked(buttons);
        ButtonLock.SetLocked(_ruleButton, isLocked);
        ButtonLock.SetLocked(_settingsButton, isLocked);
    }

    // 라운드 번호(S3)가 생기기 전까지는 마지막 라운드 아님으로 둔다
    private PhaseButton GetCurrentButtons()
    {
        bool playerWonRound = _flow.GetHealth(Side.Player) > GameRules.MinHealth;
        return PhaseButtonRules.GetButtons(_flow.CurrentPhase, _flow.TurnPlayer,
            hasUsableItem: _flow.HasUsableItem(Side.Player), playerWonRound: playerWonRound, isFinalRound: false);
    }

    private void OnDrawCard()
    {
        _flow.DrawCard();
    }

    private void OnKeep()
    {
        _flow.Distribute(keep: true);
    }

    private void OnGive()
    {
        _flow.Distribute(keep: false);
    }

    private void OnNextShuffle()
    {
        _flow.Next();
    }

    // [아이템 사용 완료]와 [아이템 없음]은 둘 다 지금 아이템 단계를 끝낸다
    private void OnFinishItemPhase()
    {
        _flow.FinishItemPhase();
    }
}
