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

    // 뽑은 카드를 더미 위로 들어 올리는 시간(초)
    [SerializeField, Range(0.45f, 0.8f)] private float _cardLiftSeconds = 0.6f;

    // 들어 올린 카드의 딜러 쪽 가장자리를 들어 올리는 각도(테이블 기준, 도). 플레이어·딜러가 뽑은 카드 모두 같은 경사로 기운다.
    // 기획서 시작값 60°(범위 45~75°)는 너무 세워져 보여, Play 검증(RSP-63)에서 낮은 경사로 바꾸고 범위도 넓혔다
    [SerializeField, Range(15f, 75f)] private float _cardLiftTiltDegrees = 30f;

    public float MatchResultSeconds => _matchResultSeconds;
    public float SettlementSeconds => _settlementSeconds;
    public float DealerThinkingMinSeconds => _dealerThinkingMinSeconds;
    public float DealerThinkingMaxSeconds => _dealerThinkingMaxSeconds;
    public float CardMoveSeconds => _cardMoveSeconds;
    public float CardMoveDelaySeconds => _cardMoveDelaySeconds;
    public float CardFlipSeconds => _cardFlipSeconds;
    public float CardSwapSeconds => _cardSwapSeconds;
    public float CardLiftSeconds => _cardLiftSeconds;
    public float CardLiftTiltDegrees => _cardLiftTiltDegrees;
}
