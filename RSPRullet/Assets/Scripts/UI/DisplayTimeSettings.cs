using UnityEngine;

// 자동 진행 화면의 표시 시간 (UI·UX 화면 흐름 세부 기획서 3-2장 읽는 시간). 에셋은 하나만 만든다.
// 실제 값은 S3 플레이 테스트에서 범위 안에서 고정한다. 다른 자동 진행 화면의 시간은 해당 화면을 만드는 이슈에서 추가한다.
[CreateAssetMenu(fileName = "DisplayTimeSettings", menuName = "RSPRullet/Display Time Settings")]
public class DisplayTimeSettings : ScriptableObject
{
    // 판-07 승패 판정 표시 시간(초). 시간이 지나면 다음 판·정산·라운드 종료로 넘어간다
    [SerializeField, Range(1.2f, 2f)] private float _matchResultSeconds = 1.6f;

    // 판-03 딜러 고민 중 대기 시간(초). 판마다 최소~최대 사이에서 무작위로 정한다 (연출·아트·사운드 세부 기획서 7-3장).
    // 판-06 딜러 아이템 대기도 같은 범위를 쓴다. 기획서의 0.8~1.5초에서 플레이 테스트(RSP-97)로 1.5~2.0초로 늘렸다
    [SerializeField, Min(0f)] private float _dealerThinkingMinSeconds = 1.5f;
    [SerializeField, Min(0f)] private float _dealerThinkingMaxSeconds = 2.0f;

    public float MatchResultSeconds => _matchResultSeconds;
    public float DealerThinkingMinSeconds => _dealerThinkingMinSeconds;
    public float DealerThinkingMaxSeconds => _dealerThinkingMaxSeconds;
}
