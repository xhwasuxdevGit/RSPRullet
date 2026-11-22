using UnityEngine;
using UnityEngine.UI;

// 설정 팝업과 게임 종료 확인 팝업 (UI·UX 화면 흐름 세부 기획서 6-4장 설정 팝업).
// 설정 버튼의 잠금은 PhaseButtonsUI가 맡고, 이 클래스는 팝업 열기·닫기와 타이틀로 가기·게임 종료만 맡는다.
public class SettingsPopupUI : MonoBehaviour
{
    // [타이틀로]를 누르면 진행 상태를 새 게임으로 돌리고 타이틀 씬으로 간다 (RSP-80)
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private Button _settingsButton;

    [Header("설정 팝업")]
    [SerializeField] private GameObject _settingsPopup;
    [SerializeField] private Button _titleButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _closeButton;

    [Header("게임 종료 확인 팝업")]
    [SerializeField] private GameObject _quitConfirmPopup;
    [SerializeField] private Button _confirmQuitButton;
    [SerializeField] private Button _cancelQuitButton;

    private void Awake()
    {
        _settingsButton.onClick.AddListener(OpenSettings);
        _quitButton.onClick.AddListener(OpenQuitConfirm);
        _closeButton.onClick.AddListener(CloseSettings);
        _confirmQuitButton.onClick.AddListener(QuitGame);
        _cancelQuitButton.onClick.AddListener(CloseQuitConfirm);
        _titleButton.onClick.AddListener(GoToTitle);

        _settingsPopup.SetActive(false);
        _quitConfirmPopup.SetActive(false);
    }

    private void OnDestroy()
    {
        _settingsButton.onClick.RemoveListener(OpenSettings);
        _quitButton.onClick.RemoveListener(OpenQuitConfirm);
        _closeButton.onClick.RemoveListener(CloseSettings);
        _confirmQuitButton.onClick.RemoveListener(QuitGame);
        _cancelQuitButton.onClick.RemoveListener(CloseQuitConfirm);
        _titleButton.onClick.RemoveListener(GoToTitle);
    }

    // 팝업을 닫아 두어야 화면이 어두워지는 동안 팝업이 검은 화면 위에 남지 않는다
    private void GoToTitle()
    {
        _settingsPopup.SetActive(false);
        _runner.GoToTitle();
    }

    private void OpenSettings()
    {
        _settingsPopup.SetActive(true);
    }

    private void CloseSettings()
    {
        _settingsPopup.SetActive(false);
    }

    // 확인 팝업은 설정 팝업 위에 띄우고, 취소하면 설정 팝업으로 돌아간다
    private void OpenQuitConfirm()
    {
        _quitConfirmPopup.SetActive(true);
    }

    private void CloseQuitConfirm()
    {
        _quitConfirmPopup.SetActive(false);
    }

    // 에디터에서는 Application.Quit이 동작하지 않으므로 플레이 모드를 끈다
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
