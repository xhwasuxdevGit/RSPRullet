using UnityEngine;
using UnityEngine.UI;

// 하단 HUD의 단계별 버튼과 입력 잠금 (UI·UX 화면 흐름 세부 기획서 6-1장 단계별 버튼, 6-2장 입력 잠금).
// 진행 흐름(GameFlow)의 단계 변경 알림과 아이템 사용 알림을 받을 때만 켤 버튼을 다시 정한다.
// 버튼을 고르는 턴 플레이어·체력은 단계가 바뀔 때 함께 정해지므로(라운드 종료의 체력은 판정·정산에서 먼저 바뀜) 단계 알림으로 충분하다.
// 아이템을 쓰거나 돋보기 선택을 마쳐도 단계는 그대로라서, 돋보기 선택 버튼과 [아이템 사용 완료]·[아이템 없음] 전환은 아이템 사용 알림으로 다시 정한다
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
    [SerializeField] private Button _exchangeButton;
    [SerializeField] private Button _keepCardsButton;

    [Header("입력 잠금 중 함께 잠그는 버튼")]
    [SerializeField] private Button _ruleButton;
    [SerializeField] private Button _settingsButton;

    private GameFlow _flow;
    private (Button button, PhaseButton flag)[] _phaseButtons;

    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.PhaseChanged += OnPhaseChanged;
        _flow.ItemUsed += RefreshButtons;
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
            (_exchangeButton, PhaseButton.Exchange),
            (_keepCardsButton, PhaseButton.KeepCards)
        };

        _drawCardButton.onClick.AddListener(OnDrawCard);
        _keepButton.onClick.AddListener(OnKeep);
        _giveButton.onClick.AddListener(OnGive);
        _nextShuffleButton.onClick.AddListener(OnNextShuffle);
        _itemDoneButton.onClick.AddListener(OnFinishItemPhase);
        _noItemButton.onClick.AddListener(OnFinishItemPhase);
        _nextRoundButton.onClick.AddListener(OnNextRound);
        _takeMedicineButton.onClick.AddListener(OnTakeMedicine);
        _exchangeButton.onClick.AddListener(OnExchange);
        _keepCardsButton.onClick.AddListener(OnKeepCards);
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
        _flow.ItemUsed -= RefreshButtons;
        _drawCardButton.onClick.RemoveListener(OnDrawCard);
        _keepButton.onClick.RemoveListener(OnKeep);
        _giveButton.onClick.RemoveListener(OnGive);
        _nextShuffleButton.onClick.RemoveListener(OnNextShuffle);
        _itemDoneButton.onClick.RemoveListener(OnFinishItemPhase);
        _noItemButton.onClick.RemoveListener(OnFinishItemPhase);
        _nextRoundButton.onClick.RemoveListener(OnNextRound);
        _takeMedicineButton.onClick.RemoveListener(OnTakeMedicine);
        _exchangeButton.onClick.RemoveListener(OnExchange);
        _keepCardsButton.onClick.RemoveListener(OnKeepCards);
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        RefreshButtons();
    }

    private void RefreshButtons()
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

    private PhaseButton GetCurrentButtons()
    {
        bool playerWonRound = _flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
        return PhaseButtonRules.GetButtons(_flow.CurrentPhase, _flow.Table.TurnPlayer,
            hasUsableItem: _flow.HasUsableItem(Side.Player), isWaitingForMagnifierChoice: _flow.IsWaitingForMagnifierChoice,
            playerWonRound: playerWonRound, isFinalRound: _runner.IsFinalRound);
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

    private void OnNextRound()
    {
        _runner.StartNextRound();
    }

    private void OnTakeMedicine()
    {
        _runner.TakeMedicine();
    }

    private void OnFinishItemPhase()
    {
        _flow.FinishItemPhase();
    }

    private void OnExchange()
    {
        _flow.ChooseMagnifierExchange(exchange: true);
    }

    private void OnKeepCards()
    {
        _flow.ChooseMagnifierExchange(exchange: false);
    }
}
