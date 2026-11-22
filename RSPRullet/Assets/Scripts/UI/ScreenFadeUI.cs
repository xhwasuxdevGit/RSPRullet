using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 라운드 전환 화면 (RSP-91). 씬을 다시 불러오는 순간을 검은 화면으로 가린다.
// 라운드 클리어: 클리어 배너가 남아 있는 채로 화면이 어두워짐 → 배너가 사라지고 딜러 안내 말풍선 → 클릭하면 [다음 라운드] 버튼.
// 라운드 패배: 같은 방식으로 체력이 0이 되면 자동으로 어두워지고 [응급약 복용] 버튼을 켠다. 누르면 효과음을 재생한 뒤 씬을 불러온다 (PlayMedicineThen).
// 다시하기: 버튼을 누르면 화면이 어두워진 뒤 씬을 불러온다 (FadeOutThen).
// 새 씬은 검은 화면으로 시작해 밝아진다. Play를 처음 시작할 때도 같다.
// 검은 이미지는 하단 HUD보다 아래 층에 두어 [다음 라운드]·[응급약 복용] 버튼이 검은 화면 위에 보이게 한다.
// 검은 이미지가 조금이라도 보이는 동안에는 클릭을 막고, 어두워지는 중이거나 안내를 기다리는 동안에는 IsBusy로 단계별 버튼을 잠근다
public class ScreenFadeUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private BannerUI _banner;
    [SerializeField] private DisplayTimeSettings _displayTimeSettings;

    // 1라운드 클리어 안내는 튜토리얼 승리 안내 문구를 그대로 쓴다
    [SerializeField] private TutorialSettings _tutorialSettings;

    // 화면 전체를 덮는 검은 이미지의 투명도
    [SerializeField] private CanvasGroup _fadeGroup;

    // 검은 화면의 딜러 안내 말풍선. 화면 어디를 눌러도 읽었다고 보고 닫는다
    [SerializeField] private GameObject _guideBubble;
    [SerializeField] private TMP_Text _guideText;
    [SerializeField] private Button _guideClickCatcher;
    [SerializeField] private GameObject _guideClickIcon;

    // 2라운드 클리어 뒤 안내 문구. TMP 태그와 줄바꿈을 쓸 수 있다
    [SerializeField, TextArea(2, 6)] private string _round2ClearText;

    // 응급약 복용 효과음. 목록 순서대로 이어서 재생한다 (약 포장 소리 → 삼키는 소리)
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _medicineClips;

    // 어두워지는 중이거나 안내 클릭을 기다리는 중인지. 단계별 버튼은 이 동안 잠긴다
    public bool IsBusy { get; private set; }

    public event Action BusyChanged;

    private void Awake()
    {
        _banner.Held += OnBannerHeld;
        _guideClickCatcher.onClick.AddListener(OnGuideClicked);
        ShowGuide(null);
        SetDarkness(1f);
    }

    private void Start()
    {
        StartCoroutine(Fade(1f, 0f, _displayTimeSettings.ScreenFadeInSeconds));
    }

    private void OnDestroy()
    {
        _banner.Held -= OnBannerHeld;
        _guideClickCatcher.onClick.RemoveListener(OnGuideClicked);
    }

    // 화면을 어둡게 한 뒤 onDark를 부른다. 다시하기에서 씬을 불러오기 전에 쓴다
    public void FadeOutThen(Action onDark)
    {
        SetBusy(true);
        StartCoroutine(FadeOutRoutine(onDark));
    }

    private IEnumerator FadeOutRoutine(Action onDark)
    {
        yield return StartCoroutine(Fade(_fadeGroup.alpha, 1f, _displayTimeSettings.ScreenFadeOutSeconds));
        onDark();
    }

    // 라운드 승패 배너를 다 보여 줬으면 화면을 어둡게 하고, 배너를 지운 뒤 안내를 띄운다
    private void OnBannerHeld()
    {
        SetBusy(true);
        StartCoroutine(RoundEndRoutine());
    }

    private IEnumerator RoundEndRoutine()
    {
        yield return StartCoroutine(Fade(_fadeGroup.alpha, 1f, _displayTimeSettings.ScreenFadeOutSeconds));
        _banner.ReleaseHeld();
        while (_banner.IsShowing) { yield return null; }

        string text = GetRoundEndText();
        if (text == null)
        {
            SetBusy(false);
            yield break;
        }

        ShowGuide(text);
    }

    // 1라운드는 튜토리얼 승리·응급약 안내(TutorialGuideRules.GetRoundEndStep), 2라운드 클리어는 2라운드 클리어 문구다.
    // 안내가 없으면(2라운드 패배, 응급약을 먹은 뒤의 1라운드 패배) null을 돌려주고 바로 버튼을 켠다
    private string GetRoundEndText()
    {
        bool playerWonRound = _runner.Flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
        if (_runner.Flow.RoundNumber != TutorialGuideRules.TutorialRoundNumber)
        {
            return playerWonRound ? _round2ClearText : null;
        }

        TutorialStep? step = TutorialGuideRules.GetRoundEndStep(playerWonRound, _runner.MedicineCount);
        return step == null ? null : _tutorialSettings.GetEntry(step.Value)?.Text;
    }

    // 응급약 복용: 이미 어두운 화면에서 효과음을 순서대로 재생한 뒤 onDone을 부른다
    public void PlayMedicineThen(Action onDone)
    {
        SetBusy(true);
        StartCoroutine(MedicineRoutine(onDone));
    }

    private IEnumerator MedicineRoutine(Action onDone)
    {
        foreach (AudioClip clip in _medicineClips)
        {
            _audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
        }

        onDone();
    }

    private void OnGuideClicked()
    {
        ShowGuide(null);
        SetBusy(false);
    }

    // text가 null이면 말풍선과 클릭 영역을 끈다
    private void ShowGuide(string text)
    {
        bool isShown = text != null;
        _guideBubble.SetActive(isShown);
        _guideClickCatcher.gameObject.SetActive(isShown);
        _guideClickIcon.SetActive(isShown);
        if (isShown) { _guideText.text = text; }
    }

    private IEnumerator Fade(float fromDarkness, float toDarkness, float seconds)
    {
        // 남은 변화량에 비례해 시간을 줄여, 중간에서 이어 가도 같은 빠르기로 바뀐다
        float duration = seconds * Mathf.Abs(toDarkness - fromDarkness);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetDarkness(Mathf.Lerp(fromDarkness, toDarkness, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
            yield return null;
        }

        SetDarkness(toDarkness);
    }

    // darkness 0: 다 밝음(클릭 통과), 1: 다 어두움
    private void SetDarkness(float darkness)
    {
        _fadeGroup.alpha = darkness;
        _fadeGroup.blocksRaycasts = darkness > 0f;
    }

    private void SetBusy(bool isBusy)
    {
        if (IsBusy == isBusy) { return; }

        IsBusy = isBusy;
        BusyChanged?.Invoke();
    }
}
