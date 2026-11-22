using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀 화면 (RSP-80). [게임 시작]은 인트로 씬으로 가고, [게임 종료]는 확인 없이 바로 끝낸다
public class TitleUI : MonoBehaviour
{
    // 빌드 목록(File > Build Profiles)에 같은 이름의 씬이 있어야 한다
    private const string IntroSceneName = "Intro";

    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;

    private void Awake()
    {
        _startButton.onClick.AddListener(StartGame);
        _quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDestroy()
    {
        _startButton.onClick.RemoveListener(StartGame);
        _quitButton.onClick.RemoveListener(QuitGame);
    }

    private void StartGame()
    {
        SceneManager.LoadScene(IntroSceneName);
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
