using UnityEngine;

// 진행 안내 배너 설정 (RSP-86). 에셋은 하나만 만든다.
// 배너 문구와 역할별 색·스프라이트를 코드에 쓰지 않고 이 에셋에서 인스펙터로 고친다.
// 어떤 배너가 어느 역할인지, 언제 띄우는지는 게임 규칙이라 BannerRules와 GameFlow에 둔다
[CreateAssetMenu(fileName = "BannerSettings", menuName = "RSPRullet/Banner Settings")]
public class BannerSettings : ScriptableObject
{
    // 배너 종류마다 항목을 하나씩 넣는다
    [SerializeField] private BannerEntry[] _entries;

    // 띠 배너의 스프라이트. 진행 전환은 하단 버튼과 같은 패널을 그대로 쓰고, 나머지 역할은 색을 입히는 회색 패널을 쓴다
    [Header("띠 배너 스프라이트")]
    [SerializeField] private Sprite _transitionSprite;
    [SerializeField] private Sprite _tintedPanelSprite;

    // 띠 배너의 역할별 배경색과 글자색 (BannerRules.GetRole).
    // 회색 패널(buttonLong_grey, 평균 밝기 0.88)에 곱해지는 색은 원하는 색을 0.88로 나눈 값을 넣어 화면에서 원하는 색이 나오게 한다
    [Header("진행 전환: 카드 섞기·카드 정산 (UI 버튼 패널 그대로)")]
    [SerializeField] private Color _transitionColor = Color.white;
    [SerializeField] private Color _transitionTextColor = new Color(0.922f, 0.922f, 0.941f);

    [Header("선 정하기 결과")]
    [SerializeField] private Color _firstTurnColor = new Color(0.967f, 0.886f, 0.726f);
    [SerializeField] private Color _firstTurnTextColor = new Color(0.227f, 0.173f, 0.102f);

    [Header("판 결과: 판 승패·무승부·서든 데스")]
    [SerializeField] private Color _matchResultColor = new Color(0.143f, 0.143f, 0.143f);
    [SerializeField] private Color _matchResultTextColor = new Color(0.922f, 0.922f, 0.941f);

    // 라운드 레이어: 화면을 가로로 3등분해 위·아래 막, 가운데 층, 가운데 층의 위·아래 경계선
    [Header("라운드: 라운드 시작·클리어·패배 (3등분 레이어)")]
    [SerializeField] private Color _roundCoverColor = new Color(0f, 0f, 0f, 254f / 255f);
    [SerializeField] private Color _roundMiddleColor = new Color(0.071f, 0.071f, 0.071f);
    [SerializeField] private Color _roundLineColor = new Color(0.514f, 0.529f, 0.588f);
    [SerializeField] private Color _roundTextColor = new Color(0.922f, 0.922f, 0.941f);

    public Sprite TransitionSprite => _transitionSprite;
    public Sprite TintedPanelSprite => _tintedPanelSprite;
    public Color RoundCoverColor => _roundCoverColor;
    public Color RoundMiddleColor => _roundMiddleColor;
    public Color RoundLineColor => _roundLineColor;
    public Color RoundTextColor => _roundTextColor;

    // 종류에 맞는 항목. 목록에 없으면 null이다
    public BannerEntry GetEntry(BannerType type)
    {
        foreach (BannerEntry entry in _entries)
        {
            if (entry.Type == type) { return entry; }
        }

        return null;
    }

    // 띠 배너 역할의 배경색. 라운드 역할은 레이어 색을 따로 쓴다
    public Color GetBackgroundColor(BannerRole role)
    {
        switch (role)
        {
            case BannerRole.FirstTurn:
                return _firstTurnColor;
            case BannerRole.MatchResult:
                return _matchResultColor;
            default:
                return _transitionColor;
        }
    }

    public Color GetTextColor(BannerRole role)
    {
        switch (role)
        {
            case BannerRole.FirstTurn:
                return _firstTurnTextColor;
            case BannerRole.MatchResult:
                return _matchResultTextColor;
            case BannerRole.Round:
                return _roundTextColor;
            default:
                return _transitionTextColor;
        }
    }
}
