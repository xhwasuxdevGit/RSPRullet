using System.Collections.Generic;

// 진행 흐름(GameFlow)의 진행 로그 문구. 흐름은 언제 남길지만 정하고, 여러 값을 엮는 문구는 여기서 만든다.
// 화면에 보이는 판정 기록 문구는 JudgmentRecordRules가 따로 만든다
public static class FlowLogText
{
    public static string GetStartCards(HandShape playerShape, HandShape dealerShape)
    {
        return $"시작 카드: 플레이어 {DisplayNames.GetShapeName(playerShape)}, 딜러 {DisplayNames.GetShapeName(dealerShape)}";
    }

    // 지급 뒤 양쪽 보유 아이템. reason은 "라운드 시작 지급"·"카드 셔플 지급"
    public static string GetHeldItems(string reason, IReadOnlyList<ItemType> playerItems, IReadOnlyList<ItemType> dealerItems)
    {
        return $"{reason}: 플레이어 [{GetItemNames(playerItems)}], 딜러 [{GetItemNames(dealerItems)}]";
    }

    public static string GetMatchResult(int matchIndex, HandShape playerShape, HandShape dealerShape, MatchResult playerResult,
        int playerHealth, int dealerHealth)
    {
        return $"{matchIndex + 1}번째 판: {DisplayNames.GetShapeName(playerShape)} vs {DisplayNames.GetShapeName(dealerShape)} → {GetResultName(playerResult)} " +
            $"(체력 플레이어 {playerHealth} / 딜러 {dealerHealth})";
    }

    public static string GetSettlement(CombinationType playerCombination, CombinationType dealerCombination, int playerHealth, int dealerHealth)
    {
        return $"정산: 플레이어 {playerCombination}, 딜러 {dealerCombination} (체력 플레이어 {playerHealth} / 딜러 {dealerHealth})";
    }

    private static string GetItemNames(IReadOnlyList<ItemType> items)
    {
        var names = new List<string>();
        foreach (ItemType item in items)
        {
            names.Add(DisplayNames.GetItemName(item));
        }

        return string.Join(", ", names);
    }

    private static string GetResultName(MatchResult playerResult)
    {
        switch (playerResult)
        {
            case MatchResult.Win:
                return "플레이어 승리";
            case MatchResult.Lose:
                return "딜러 승리";
            default:
                return "무승부";
        }
    }
}
