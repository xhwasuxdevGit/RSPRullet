using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 가운데 진행 안내 배너 (RSP-86). 진행 흐름(GameFlow)의 배너 요청을 받으면 나타남 → 읽는 시간 → 사라짐 순서로 보여 준다.
// 요청을 받은 순간 IsShowing을 켜므로, 같은 순간에 오는 단계·카드 알림보다 먼저 잠긴다.
// 배너가 떠 있는 동안 단계별 버튼, 자동 진행 시간, 카드 연출, 새 딜러 말풍선은 IsShowing·ShowingChanged를 보고 기다린다.
// 딜러 대기 동작·조명·상단 정보·판정 기록·체력 숫자는 멈추지 않는다. Time.timeScale은 쓰지 않는다.
// 배너가 떠 있을 때 새 요청이 오면 지금 배너를 바로 바꿔 띄운다. 대기열은 두지 않는다.
// 마지막 라운드가 아닌 라운드 승패 배너는 읽는 시간이 지나도 남겨 두고, 라운드 전환 화면(ScreenFadeUI)이 화면을 어둡게 한 뒤 지운다 (RSP-91)
public class BannerUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private DisplayTimeSettings _displayTimeSettings;

    // 배너가 뜰 때 효과음 (RSP-74)
    [SerializeField] private SoundPlayer _sound;

    // 투명도로 나타남·사라짐을 보여 준다. 클릭은 막지 않는다(blocksRaycasts 끔)
    [SerializeField] private CanvasGroup _group;

    // 나타날 때 커지고 사라질 때 작아지는 배너 띠
    [SerializeField] private RectTransform _panel;
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _text;

    // 라운드 레이어(라운드 시작·클리어·패배): 화면을 가로로 3등분해 위·아래 막, 가운데 층, 가운데 층의 위·아래 경계선으로 덮는다.
    // 화면 전체를 덮으므로 띠처럼 크기를 바꾸지 않고 투명도로만 나타난다
    [SerializeField] private GameObject _roundLayer;
    [SerializeField] private TMP_Text _roundText;
    [SerializeField] private Image[] _roundCovers;
    [SerializeField] private Image _roundMiddle;
    [SerializeField] private Image[] _roundLines;

    // 배너 문구와 역할별 색·스프라이트 (RSP-86 시안에서 확정한 값이 들어 있다)
    [SerializeField] private BannerSettings _settings;

    // 다 사라졌을 때의 크기 배율. 나타나는 동안 이 크기에서 원래 크기(1)로 커진다
    [SerializeField, Range(0.5f, 1f)] private float _hiddenScale = 0.9f;

    private GameFlow _flow;

    // 배너를 보여 주는 중인지. 나타나기 시작한 순간부터 다 사라질 때까지 참이다
    public bool IsShowing { get; private set; }

    // 라운드 승패 배너를 다 보여 준 뒤 사라지지 않고 남겨 둔 중인지 (RSP-91). 라운드 전환 화면이 ReleaseHeld로 지운다
    private bool _isHeld;

    public event Action ShowingChanged;

    // 남겨 두는 배너가 다 나타나고 읽는 시간까지 지났을 때 알린다. 라운드 전환 화면은 이때부터 화면을 어둡게 한다
    public event Action Held;

    private void Awake()
    {
        _flow = _runner.Flow;
        _flow.BannerRequested += OnBannerRequested;
        _group.blocksRaycasts = false;
        SetVisibility(0f);
    }

    private void OnDestroy()
    {
        _flow.BannerRequested -= OnBannerRequested;
    }

    private void OnBannerRequested(BannerType type)
    {
        if (!BannerRules.IsShown(type, _runner.IsFinalRound)) { return; }

        // 설정 에셋에 항목이 없으면 빈 배너를 띄우지 않는다. 항목이 모두 있는지는 EditMode 테스트(SettingsAssetTests)로 확인한다
        BannerEntry entry = _settings.GetEntry(type);
        if (entry == null)
        {
            Debug.LogWarning($"[BannerUI] 배너 설정에 {type} 항목이 없어 배너를 띄우지 않음");
            return;
        }

        // 나타남·사라짐 코루틴까지 함께 멈춘다. 이 컴포넌트는 배너 재생 코루틴만 쓴다
        StopAllCoroutines();

        BannerRole role = BannerRules.GetRole(type);
        string text = BannerRules.FormatText(entry.Text, type, _flow.RoundNumber, _flow.ShuffleCount);
        Color textColor = entry.UsesCustomTextColor ? entry.TextColor : _settings.GetTextColor(role);
        ApplyRole(role, text, textColor);
        float readSeconds = BannerRules.IsResult(type)
            ? _displayTimeSettings.BannerResultReadSeconds
            : _displayTimeSettings.BannerTransitionReadSeconds;

        _isHeld = false;
        SetShowing(true);
        StartCoroutine(Play(type, readSeconds,BannerRules.IsHeldForTransition(type, _runner.IsFinalRound)));
    }

    // 판 승패·무승부 판정은 계산대 소리, 라운드 승패는 소리를 내지 않고(어두워진 뒤 ScreenFadeUI가 낸다), 나머지는 안내 소리를 낸다.
    // 카드 정산의 계산대 소리는 배너가 아니라 정산 결과를 반영할 때 GameFlowRunner가 낸다
    private void PlayBannerSound(BannerType type)
    {
        switch (type)
        {
            case BannerType.MatchWin:
            case BannerType.MatchLose:
            case BannerType.MatchDraw:
                _sound.PlayCashRegister();
                break;
            case BannerType.RoundWin:
            case BannerType.RoundLose:
                break;
            default:
                _sound.PlayBanner();
                break;
        }
    }

    // 남겨 둔 라운드 승패 배너를 사라지게 한다. 화면이 다 어두워진 뒤 라운드 전환 화면이 부른다.
    // 검은 화면으로 넘어가는 모습이 이때 보이므로, 다른 배너와 따로 정한 시간으로 사라진다
    public void ReleaseHeld()
    {
        if (!_isHeld) { return; }

        _isHeld = false;
        StartCoroutine(Hide(_displayTimeSettings.RoundResultBannerHideSeconds));
    }

    // 라운드 배너는 3등분 레이어로, 나머지 역할은 가운데 띠로 보여 준다
    private void ApplyRole(BannerRole role, string text, Color textColor)
    {
        bool isRound = role == BannerRole.Round;
        _roundLayer.SetActive(isRound);
        _panel.gameObject.SetActive(!isRound);
        if (isRound)
        {
            _roundText.text = text;
            _roundText.color = textColor;
            foreach (Image cover in _roundCovers) { cover.color = _settings.RoundCoverColor; }
            foreach (Image line in _roundLines) { line.color = _settings.RoundLineColor; }
            _roundMiddle.color = _settings.RoundMiddleColor;
            return;
        }

        _text.text = text;
        _text.color = textColor;
        _background.sprite = role == BannerRole.Transition ? _settings.TransitionSprite : _settings.TintedPanelSprite;
        _background.color = _settings.GetBackgroundColor(role);
    }

    // 바꿔 띄울 때는 지금 보이는 정도에서 이어서 나타나게 해, 배너가 깜박이지 않게 한다
    // 남겨 두는 배너는 읽는 시간이 지나면 사라지지 않고 Held를 알린 채 기다린다
    // 효과음은 배너가 다 나타난 순간에 낸다
    private IEnumerator Play(BannerType type, float readSeconds, bool isHeld)
    {
        yield return StartCoroutine(Fade(_group.alpha, 1f, _displayTimeSettings.BannerAppearSeconds));
        PlayBannerSound(type);
        yield return new WaitForSeconds(readSeconds);
        if (isHeld)
        {
            _isHeld = true;
            Held?.Invoke();
            yield break;
        }

        yield return StartCoroutine(Hide(_displayTimeSettings.BannerAppearSeconds));
    }

    private IEnumerator Hide(float seconds)
    {
        yield return StartCoroutine(Fade(_group.alpha, 0f, seconds));
        SetShowing(false);
    }

    private IEnumerator Fade(float fromVisibility, float toVisibility, float seconds)
    {
        // 남은 변화량에 비례해 시간을 줄여, 바꿔 띄울 때도 같은 빠르기로 나타난다
        float duration = seconds * Mathf.Abs(toVisibility - fromVisibility);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetVisibility(Mathf.Lerp(fromVisibility, toVisibility, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
            yield return null;
        }

        SetVisibility(toVisibility);
    }

    // visibility 0: 다 사라짐(투명, 작은 크기), 1: 다 나타남(불투명, 원래 크기)
    private void SetVisibility(float visibility)
    {
        _group.alpha = visibility;
        _panel.localScale = Vector3.one * Mathf.Lerp(_hiddenScale, 1f, visibility);
    }

    private void SetShowing(bool isShowing)
    {
        if (IsShowing == isShowing) { return; }

        IsShowing = isShowing;
        ShowingChanged?.Invoke();
    }
}
