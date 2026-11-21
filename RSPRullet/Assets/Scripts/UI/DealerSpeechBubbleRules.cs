// 딜러 상태 말풍선에 "고민 중…"을 보여 줄지 정한다 (UI·UX 화면 흐름 세부 기획서 3-1장 판-03).
// 1라운드는 튜토리얼 라운드라 같은 자리에 튜토리얼 안내를 띄우므로 고민 중 말풍선을 보여 주지 않는다
// (튜토리얼(1라운드) 세부 기획서). 딜러 고민 시간 대기와 입력 잠금은 라운드와 상관없이 그대로다.
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class DealerSpeechBubbleRules
{
    // 튜토리얼은 1라운드에서만 한다 (튜토리얼(1라운드) 세부 기획서)
    private const int TutorialRoundNumber = 1;

    public static bool IsThinkingShown(bool isWaitingForDealer, int roundNumber)
    {
        return isWaitingForDealer && roundNumber != TutorialRoundNumber;
    }
}
