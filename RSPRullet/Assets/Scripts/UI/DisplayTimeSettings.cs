using UnityEngine;

// 자동 진행 화면의 표시 시간 (UI·UX 화면 흐름 세부 기획서 3-2장 읽는 시간). 에셋은 하나만 만든다.
// 실제 값은 S3 플레이 테스트에서 범위 안에서 고정한다. 다른 자동 진행 화면의 시간은 해당 화면을 만드는 이슈에서 추가한다.
[CreateAssetMenu(fileName = "DisplayTimeSettings", menuName = "RSPRullet/Display Time Settings")]
public class DisplayTimeSettings : ScriptableObject
{
    // 판-07 승패 판정 표시 시간(초). 시간이 지나면 다음 판·정산·라운드 종료로 넘어간다
    [SerializeField, Range(1.2f, 2f)] private float _matchResultSeconds = 1.6f;

    public float MatchResultSeconds => _matchResultSeconds;
}
