using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 가운데 진행 안내 배너 (RSP-86). 진행 흐름(GameFlow)의 배너 요청을 받으면 나타남 → 읽는 시간 → 사라짐 순서로 보여 준다.
// 요청을 받은 순간 IsShowing을 켜므로, 같은 순간에 오는 단계·카드 알림보다 먼저 잠긴다.
// 배너가 떠 있는 동안 단계별 버튼, 자동 진행 시간, 카드 연출, 새 딜러 말풍선은 IsShowing·ShowingChanged를 보고 기다린다.
// 딜러 대기 동작·조명·상단 정보·판정 기록·체력 숫자는 멈추지 않는다. Time.timeScale은 쓰지 않는다.
// 배너가 떠 있을 때 새 요청이 오면 지금 배너를 바로 바꿔 띄운다. 대기열은 두지 않는다
public class BannerUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private DisplayTimeSettings _displayTimeSettings;

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

    public event Action ShowingChanged;

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

        SetShowing(true);
        StartCoroutine(Play(readSeconds));
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
    private IEnumerator Play(float readSeconds)
    {
        float appearSeconds = _displayTimeSettings.BannerAppearSeconds;
        yield return StartCoroutine(Fade(_group.alpha, 1f, appearSeconds));
        yield return new WaitForSeconds(readSeconds);
        yield return StartCoroutine(Fade(1f, 0f, appearSeconds));

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
