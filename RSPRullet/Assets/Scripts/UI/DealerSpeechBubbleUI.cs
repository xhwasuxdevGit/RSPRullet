using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 딜러 상태 말풍선 (UI·UX 화면 흐름 세부 기획서 2-2장 딜러 영역, 3-1장 판-03·판-06).
// 딜러가 고민 중 대기일 때는 "고민 중…" 말풍선을 보여주고, 1라운드에서는 보여주지 않는다 (DealerSpeechBubbleRules).
// 딜러가 아이템을 쓴 뒤 대기할 때는 문구 길이에 맞춘 별도의 "아이템을 쓰는 중…" 말풍선을 보여준다.
// 1라운드는 아이템이 없어 아이템 대기가 생기지 않는다.
// 1라운드 튜토리얼 안내는 같은 자리에 최대 4줄 문구가 들어가는 별도의 안내 말풍선으로 보여준다 (튜토리얼(1라운드) 세부 기획서 2-2장).
// 1라운드에는 다른 두 말풍선이 뜨지 않아 안내 말풍선과 겹치지 않는다.
// 안내 말풍선의 크기는 문구에 맞춰 씬의 레이아웃 컴포넌트(ContentSizeFitter, HorizontalLayoutGroup)가 정하므로 코드에서 정하지 않는다.
// 두 대기는 진행 단계와 턴 플레이어로 정해지므로 진행 흐름(GameFlow)의 단계 변경 알림을 받을 때만 다시 정한다.
// 안내는 다음 안내가 나오거나 지울 때까지 단계가 바뀌어도 남으므로 튜토리얼 안내(TutorialGuide)의 변경 알림을 받을 때만 다시 정한다.
// 안내가 클릭을 기다리는 동안에는 화면 전체를 덮는 투명 버튼과 말풍선 오른쪽 아래의 화살표 아이콘을 켠다.
// 이때는 자동 진행 화면이라 단계별 버튼이 잠겨 있어(PhaseButtonRules) 투명 버튼이 다른 버튼의 클릭을 가로채지 않는다.
// 진행 안내 배너가 떠 있는 동안에는 말풍선과 투명 버튼을 모두 숨겨 두고, 배너가 꺼지면 다시 정한다 (RSP-86)
public class DealerSpeechBubbleUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private BannerUI _banner;
    [SerializeField] private GameObject _bubble;
    [SerializeField] private GameObject _itemBubble;
    [SerializeField] private GameObject _guideBubble;
    [SerializeField] private TMP_Text _guideText;
    [SerializeField] private Button _guideClickCatcher;
    [SerializeField] private GameObject _guideClickIcon;

    private GameFlow _flow;
    private TutorialGuide _guide;

    private void Awake()
    {
        _flow = _runner.Flow;
        _guide = _runner.Guide;
        _flow.PhaseChanged += OnPhaseChanged;
        _guide.Changed += OnGuideChanged;
        _banner.ShowingChanged += OnBannerShowingChanged;
        _guideClickCatcher.onClick.AddListener(OnGuideClicked);
        _bubble.SetActive(false);
        _itemBubble.SetActive(false);
        _guideBubble.SetActive(false);
        _guideClickCatcher.gameObject.SetActive(false);
        _guideClickIcon.SetActive(false);
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
        _guide.Changed -= OnGuideChanged;
        _banner.ShowingChanged -= OnBannerShowingChanged;
        _guideClickCatcher.onClick.RemoveListener(OnGuideClicked);
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        RefreshWaitingBubbles();
    }

    private void OnGuideChanged()
    {
        RefreshGuideBubble();
    }

    private void OnBannerShowingChanged()
    {
        RefreshWaitingBubbles();
        RefreshGuideBubble();
    }

    // 고민 중 대기는 분배 단계, 아이템 대기는 아이템 단계라 두 말풍선이 함께 켜지지 않는다
    private void RefreshWaitingBubbles()
    {
        bool isHidden = _banner.IsShowing;
        _bubble.SetActive(!isHidden && DealerSpeechBubbleRules.IsThinkingShown(_flow.IsWaitingForDealer, _flow.RoundNumber));
        _itemBubble.SetActive(!isHidden && _flow.IsWaitingForDealerItems);
    }

    private void RefreshGuideBubble()
    {
        TutorialGuideEntry entry = _guide.CurrentEntry;
        bool isShown = !_banner.IsShowing && entry != null;
        _guideBubble.SetActive(isShown);
        if (isShown) { _guideText.text = entry.Text; }

        bool isWaitingForClick = !_banner.IsShowing && _guide.IsWaitingForClick;
        _guideClickCatcher.gameObject.SetActive(isWaitingForClick);
        _guideClickIcon.SetActive(isWaitingForClick);
    }

    // 화면 어디를 눌러도 안내를 읽었다고 보고 다음으로 넘어가게 한다 (튜토리얼(1라운드) 세부 기획서 2-3장)
    private void OnGuideClicked()
    {
        _guide.Confirm();
    }
}
