using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임 클리어·게임 오버 팝업 (UI·UX 화면 흐름 세부 기획서 6-5장 게임 클리어·게임 오버 팝업, 라운드와 게임 진행 세부 기획서 6장 라운드 종료 표시 내용).
// 마지막 라운드가 끝나면 이겼는지 졌는지에 맞춰 한 팝업의 제목과 왼쪽 버튼만 바꿔 연다.
// 팝업이 떠 있는 동안 하단 버튼과 규칙·설정 버튼 잠금은 PhaseButtonsUI가 맡고, 이 클래스는 팝업 표시와 [다시하기]만 맡는다
public class GameEndPopupUI : MonoBehaviour
{
    private const string ClearTitle = "게임 클리어";
    private const string GameOverTitle = "게임 오버";

    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private GameObject _popup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _medicineText;

    [Header("버튼")]
    [SerializeField] private Button _restartButton;
    // 게임 클리어 때만 보인다. 엔딩 화면이 생길 때까지 비활성이다
    [SerializeField] private Button _endingButton;
    // 게임 오버 때만 보인다. 메인 메뉴(RSP-80)가 생길 때까지 비활성이다
    [SerializeField] private Button _mainMenuButton;

    private GameFlow _flow;

    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.PhaseChanged += OnPhaseChanged;
        _restartButton.onClick.AddListener(OnRestart);

        ButtonLock.SetLocked(_endingButton, true);
        ButtonLock.SetLocked(_mainMenuButton, true);
        _popup.SetActive(false);
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
        _restartButton.onClick.RemoveListener(OnRestart);
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        if (next != GamePhase.RoundEnd || !_runner.IsFinalRound)
        {
            return;
        }

        bool playerWonRound = _flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
        _titleText.text = playerWonRound ? ClearTitle : GameOverTitle;
        _medicineText.text = $"응급약 복용 {_runner.MedicineCount}회";
        _endingButton.gameObject.SetActive(playerWonRound);
        _mainMenuButton.gameObject.SetActive(!playerWonRound);
        _popup.SetActive(true);
    }

    private void OnRestart()
    {
        _runner.RestartGame();
    }
}
