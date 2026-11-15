// 고정 규칙 값 (코어 규칙 상세 기획서 7-1-2장).
// 규칙과 진행 단계의 구조를 정하는 값이라 인스펙터에 노출하지 않고 이곳에 모은다.
public static class GameRules
{
    // "체력 0 = 패배"라는 규칙 정의
    public const int MinHealth = 0;

    // 정산에서 양쪽 모두 체력 0이 되었을 때 되돌릴 체력
    public const int HealthWhenBothZero = 1;

    // 카드풀 구성 3 · 2 · 2: 한 손 모양만 3장, 나머지 두 손 모양은 2장씩
    public const int TripleShapeCardCount = 3;
    public const int OtherShapeCardCount = 2;
    public const int CardPoolSize = TripleShapeCardCount + OtherShapeCardCount * 2;

    // 카드 셔플당 판 수
    public const int MatchesPerShuffle = 2;
}
