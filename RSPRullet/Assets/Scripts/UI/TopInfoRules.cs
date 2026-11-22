// 상단 정보 UI 문구 규칙: "1라운드    |    카드 섞기 2번째    |    두 번째 판    |    턴 플레이어: 딜러"
// (UI·UX 화면 흐름 세부 기획서 5-1장 상단 정보 UI).
// 기획서의 "카드 셔플 n번째"는 플레이어가 읽기 쉽도록 "카드 섞기 n번째"로 쓴다 (RSP-43에서 확정).
// 카드 섞기 번호와 턴 플레이어 이름은 한눈에 구분되도록 색을 입힌다 (RSP-57에서 추가).
// 글자 색은 TextMeshPro의 color 태그로 바꾸며, 색 값은 "#" 없는 RGB 16진수로 받는다.
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class TopInfoRules
{
    // 항목이 한눈에 나뉘어 보이도록 구분선 양옆을 4칸씩 띄운다
    private const string Separator = "    |    ";
    private const string SettlementName = "정산";

    // 정산 단계(결과 표시와 다음 카드 섞기 대기)에서는 판 대신 "정산"을 보여 주고 턴 플레이어는 빼고, 그 밖의 단계는 지금 판(matchIndex 0, 1)과 턴 플레이어를 보여 준다.
    // 두 번째 판이 끝나면 진행 흐름이 턴을 상대에게 넘기지만(GameFlow.FinishMatch) 다음 카드 섞기의 선은 선 정하기로 다시 정하므로,
    // 정산 중의 턴 플레이어는 의미가 없다
    public static string GetText(int roundNumber, int shuffleCount, GamePhase phase, int matchIndex, Side turnPlayer,
        string shuffleColorHex, string playerColorHex, string dealerColorHex)
    {
        string header = $"{roundNumber}라운드{Separator}{Colored($"카드 섞기 {shuffleCount}번째", shuffleColorHex)}{Separator}";
        if (phase == GamePhase.Settlement || phase == GamePhase.SettlementWait)
        {
            return header + SettlementName;
        }

        string turnColorHex = turnPlayer == Side.Player ? playerColorHex : dealerColorHex;
        return $"{header}{DisplayNames.GetMatchName(matchIndex)}{Separator}턴 플레이어: {Colored(DisplayNames.GetSideName(turnPlayer), turnColorHex)}";
    }

    // 시계를 쓴 쪽을 상단 정보 뒤에 강조색으로 붙인다 (UI·UX 화면 흐름 세부 기획서 5-2장 시계 사용 표시)
    public static string AppendClockText(string infoText, Side clockUser, string colorHex)
    {
        return $"{infoText}{Separator}{Colored($"{DisplayNames.GetSideName(clockUser)}가 시계를 사용함", colorHex)}";
    }

    private static string Colored(string text, string colorHex)
    {
        return $"<color=#{colorHex}>{text}</color>";
    }
}
