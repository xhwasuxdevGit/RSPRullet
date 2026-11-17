using UnityEngine;
using UnityEngine.UI;

// 하단 HUD의 단계별 버튼과 입력 잠금 (UI·UX 화면 흐름 세부 기획서 6-1장 단계별 버튼, 6-2장 입력 잠금).
// 진행 흐름(GameFlow)에 변경 이벤트가 없어 매 프레임 켤 버튼을 계산하고, 바뀌었을 때만 화면에 적용한다.
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

    // 첫 프레임에 반드시 화면에 적용되도록 어떤 조합과도 다른 값으로 시작한다
    private PhaseButton _appliedButtons = (PhaseButton)(-1);

    private void Awake()
    {
        _flow = _runner.Flow;
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

        // 아이템 단계와 라운드 진행은 S3 이슈에서 동작을 연결한다. 그 전까지는 보이기만 하고 눌리지 않는다
        ButtonLock.SetLocked(_itemDoneButton, true);
        ButtonLock.SetLocked(_noItemButton, true);
        ButtonLock.SetLocked(_nextRoundButton, true);
        ButtonLock.SetLocked(_takeMedicineButton, true);
        ButtonLock.SetLocked(_restartButton, true);
    }

    private void OnDestroy()
    {
        _drawCardButton.onClick.RemoveListener(OnDrawCard);
        _keepButton.onClick.RemoveListener(OnKeep);
        _giveButton.onClick.RemoveListener(OnGive);
        _nextShuffleButton.onClick.RemoveListener(OnNextShuffle);
    }

    private void Update()
    {
        PhaseButton buttons = GetCurrentButtons();
        if (buttons == _appliedButtons)
        {
            return;
        }

        _appliedButtons = buttons;
        foreach ((Button button, PhaseButton flag) in _phaseButtons)
        {
            button.gameObject.SetActive((buttons & flag) != 0);
        }

        bool isLocked = PhaseButtonRules.IsInputLocked(buttons);
        ButtonLock.SetLocked(_ruleButton, isLocked);
        ButtonLock.SetLocked(_settingsButton, isLocked);
    }

    // 아이템 보유(S3)와 라운드 번호(S3)가 생기기 전까지는 아이템 없음, 마지막 라운드 아님으로 둔다
    private PhaseButton GetCurrentButtons()
    {
        bool playerWonRound = _flow.GetHealth(Side.Player) > GameRules.MinHealth;
        return PhaseButtonRules.GetButtons(_flow.CurrentPhase, _flow.TurnPlayer,
            hasUsableItem: false, playerWonRound: playerWonRound, isFinalRound: false);
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
}
