using UnityEngine;

// 자동 진행 화면의 표시 시간 (UI·UX 화면 흐름 세부 기획서 3-2장 읽는 시간). 에셋은 하나만 만든다.
// 실제 값은 S3 플레이 테스트에서 범위 안에서 고정한다. 다른 자동 진행 화면의 시간은 해당 화면을 만드는 이슈에서 추가한다.
[CreateAssetMenu(fileName = "DisplayTimeSettings", menuName = "RSPRullet/Display Time Settings")]
public class DisplayTimeSettings : ScriptableObject
{
    // 판-07 승패 판정 표시 시간(초). 시간이 지나면 다음 판·정산·라운드 종료로 넘어간다
    [SerializeField, Range(1.2f, 2f)] private float _matchResultSeconds = 1.6f;

    // 정산-01 정산 표시 시간(초). 시간이 지나면 한쪽만 체력 0이면 라운드 종료로, 그 외에는 [다음 카드 셔플] 대기로 넘어간다
    [SerializeField, Range(1.5f, 2.5f)] private float _settlementSeconds = 2.0f;

    // 카드 셔플의 카드풀 공개 표시 시간(초). 이번 카드풀 7장을 앞면으로 보여 주고, 시간이 지나면 시작 카드를 나눠 주며 카드 섞기 연출로 넘어간다
    [SerializeField, Range(1.0f, 2.0f)] private float _cardPoolSeconds = 1.5f;

    // 선 정하기 결과 표시 시간(초). 시간이 지나면 첫 번째 판으로 가고, 비겼으면 카드풀 공개부터 다시 한다
    [SerializeField, Range(0.6f, 1.5f)] private float _decideFirstSeconds = 1.0f;

    // 판-03 딜러 고민 중 대기 시간(초). 판마다 최소~최대 사이에서 무작위로 정한다 (연출·아트·사운드 세부 기획서 7-3장).
    // 판-06 딜러 아이템 대기도 같은 범위를 쓴다. 기획서의 0.8~1.5초에서 플레이 테스트(RSP-97)로 1.5~2.0초로 늘렸다
    [SerializeField, Min(0f)] private float _dealerThinkingMinSeconds = 1.5f;
    [SerializeField, Min(0f)] private float _dealerThinkingMaxSeconds = 2.0f;

    // 카드 연출 시간 (연출·아트·사운드 세부 기획서 7-1장 시작값과 범위). 읽는 시간과 따로 두어, 연출을 바꿔도 읽는 시간은 그대로다.
    // 강조 효과는 뒤집기 1박자와 같은 시간을 쓰므로 따로 두지 않는다 (연출·아트·사운드 세부 기획서 7-1장 강조)

    // 카드가 더미에서 카드 슬롯으로 미끄러져 가는 시간(초)
    [SerializeField, Range(0.4f, 0.7f)] private float _cardMoveSeconds = 0.5f;

    // 분배 때 뽑은 카드가 출발한 뒤 더미의 다음 카드가 출발하기까지 기다리는 시간(초). 이동 시간보다 짧아야 두 이동이 겹친다
    [SerializeField, Range(0.1f, 0.35f)] private float _cardMoveDelaySeconds = 0.2f;

    // 카드 한 장을 뒤집거나 강조하는 1박자 시간(초)
    [SerializeField, Range(0.35f, 0.55f)] private float _cardFlipSeconds = 0.45f;

    // 돋보기 교환에서 두 카드가 곡선으로 엇갈려 자리를 바꾸는 시간(초)
    [SerializeField, Range(0.5f, 0.8f)] private float _cardSwapSeconds = 0.6f;

    // 카드 셔플에서 모은 더미를 섞는 짧은 동작 시간(초)
    [SerializeField, Range(0.5f, 1.0f)] private float _deckShuffleSeconds = 0.7f;

    // 뽑은 카드를 더미 위로 들어 올리는 시간(초)
    [SerializeField, Range(0.45f, 0.8f)] private float _cardLiftSeconds = 0.6f;

    // 들어 올린 카드의 딜러 쪽 가장자리를 들어 올리는 각도(테이블 기준, 도). 플레이어·딜러가 뽑은 카드 모두 같은 경사로 기운다.
    // 기획서 시작값 60°(범위 45~75°)는 너무 세워져 보여, Play 검증(RSP-63)에서 낮은 경사로 바꾸고 범위도 넓혔다
    [SerializeField, Range(15f, 75f)] private float _cardLiftTiltDegrees = 30f;

    // 진행 안내 배너 시간 (RSP-86). 배너가 떠 있는 동안 자동 진행 시간과 카드 연출은 멈추고, 꺼진 뒤부터 센다

    // 배너가 나타나는 시간(초). 사라질 때도 같은 시간을 쓴다 (연출·아트·사운드 세부 기획서 7-1장)
    [SerializeField, Range(0.3f, 0.6f)] private float _bannerAppearSeconds = 0.5f;

    // 전환 배너(라운드·셔플·판 시작·라운드 승패)를 다 나타난 뒤 보여 주는 시간(초)
    [SerializeField, Range(0.4f, 1.0f)] private float _bannerTransitionReadSeconds = 0.6f;

    // 결과 배너(선 정하기·판 승패·양쪽 체력 0 복구)를 다 나타난 뒤 보여 주는 시간(초) (연출·아트·사운드 세부 기획서 7-2장 선 정하기)
    [SerializeField, Range(0.6f, 1.5f)] private float _bannerResultReadSeconds = 1.0f;

    // 라운드 전환 페이드 시간 (RSP-91). 기획서에 값이 없어 착수 때 정했다
    // 씬을 다시 불러오기 전에 화면이 검게 어두워지는 시간(초)
    [SerializeField, Range(0.3f, 1.0f)] private float _screenFadeOutSeconds = 0.5f;

    // 새 라운드 씬이 검은 화면에서 밝아지는 시간(초)
    [SerializeField, Range(0.3f, 1.0f)] private float _screenFadeInSeconds = 0.5f;

    // 라운드 승패 배너가 검은 화면 위에서 사라지는 시간(초). 승패 배너가 화면을 덮고 있어, 라운드 승패 때 보이는 암전은 이 시간이다.
    // 다른 배너의 나타남·사라짐(Banner Appear Seconds)과 따로 둔다
    [SerializeField, Range(0.5f, 1.5f)] private float _roundResultBannerHideSeconds = 1.0f;

    public float MatchResultSeconds => _matchResultSeconds;
    public float SettlementSeconds => _settlementSeconds;
    public float CardPoolSeconds => _cardPoolSeconds;
    public float DecideFirstSeconds => _decideFirstSeconds;
    public float DealerThinkingMinSeconds => _dealerThinkingMinSeconds;
    public float DealerThinkingMaxSeconds => _dealerThinkingMaxSeconds;
    public float CardMoveSeconds => _cardMoveSeconds;
    public float CardMoveDelaySeconds => _cardMoveDelaySeconds;
    public float CardFlipSeconds => _cardFlipSeconds;
    public float CardSwapSeconds => _cardSwapSeconds;
    public float DeckShuffleSeconds => _deckShuffleSeconds;
    public float CardLiftSeconds => _cardLiftSeconds;
    public float CardLiftTiltDegrees => _cardLiftTiltDegrees;
    public float BannerAppearSeconds => _bannerAppearSeconds;
    public float BannerTransitionReadSeconds => _bannerTransitionReadSeconds;
    public float BannerResultReadSeconds => _bannerResultReadSeconds;
    public float ScreenFadeOutSeconds => _screenFadeOutSeconds;
    public float ScreenFadeInSeconds => _screenFadeInSeconds;
    public float RoundResultBannerHideSeconds => _roundResultBannerHideSeconds;
}
