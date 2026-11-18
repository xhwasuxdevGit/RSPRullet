// 상단 정보 UI 문구 규칙: "1라운드    |    카드 섞기 2번째    |    두 번째 판    |    턴 플레이어: 딜러"
// (UI·UX 화면 흐름 세부 기획서 5-1장 상단 정보 UI).
// 기획서의 "카드 셔플 n번째"는 플레이어가 읽기 쉽도록 "카드 섞기 n번째"로 쓴다 (RSP-43에서 확정).
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class TopInfoRules
{
    // 항목이 한눈에 나뉘어 보이도록 구분선 양옆을 4칸씩 띄운다
    private const string Separator = "    |    ";
    private const string SettlementName = "정산";

    // 정산 단계에서는 판 대신 "정산"을 보여 주고 턴 플레이어는 빼고, 그 밖의 단계는 지금 판(matchIndex 0, 1)과 턴 플레이어를 보여 준다.
    // 두 번째 판이 끝나면 진행 흐름이 턴을 상대에게 넘기지만(GameFlow.FinishMatch) 다음 카드 섞기의 선은 선 정하기로 다시 정하므로,
    // 정산 중의 턴 플레이어는 의미가 없다
    public static string GetText(int roundNumber, int shuffleCount, GamePhase phase, int matchIndex, Side turnPlayer)
    {
        string header = $"{roundNumber}라운드{Separator}카드 섞기 {shuffleCount}번째{Separator}";
        if (phase == GamePhase.Settlement)
        {
            return header + SettlementName;
        }

        return $"{header}{DisplayNames.GetMatchName(matchIndex)}{Separator}턴 플레이어: {DisplayNames.GetSideName(turnPlayer)}";
    }
}
