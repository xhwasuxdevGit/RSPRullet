// 딜러 판단이 함께 쓰는 계산 규칙. 분배 판단(DealerAI)과 아이템·교환 판단(DealerItemJudge)이
// 같은 방법으로 카드를 세고 HP 변화를 계산하도록 한곳에 둔다.
// 대미지·회복량은 실제 진행과 같은 규칙 설정(RuleSettings)을 받아 계산한다
public static class DealerEstimateRules
{
    public const int FirstMatchIndex = 0;

    // 판단 근거로 셀 손 모양별 카드 수 (딜러 AI 세부 기획서 3-1장).
    // 1번째 판: 딜러는 셔플 직후처럼 3장인 손모양 3장·나머지 손모양 2장씩이 그대로 남아 있다고 여긴다.
    // - > "뽑은 카드가 3장인 손모양을 이기면 가진다, 지면 준다"와 같다
    // 2번째 판: 공개된 카드와 딜러가 직접 본 카드(뽑은 카드 등)를 빼고, 정체를 모르는 카드만 남았다고 보고 비교한다
    public static int GetThisMatchCardCount(DealerKnownInfo knownInfo, HandShape shape)
    {
        return knownInfo.MatchIndex == FirstMatchIndex
            ? knownInfo.GetPoolCardCount(shape)
            : knownInfo.GetUnknownCardCount(shape);
    }

    // 판정 한 번이 딜러 기준 HP 차이를 얼마나 벌리는지 (딜러 승리 +, 비김 0, 딜러 패배 −)
    public static int GetJudgmentValue(RuleSettings rules, MatchResult dealerResult)
    {
        switch (dealerResult)
        {
            case MatchResult.Win:
                return rules.MatchDamage;
            case MatchResult.Lose:
                return -rules.MatchDamage;
            default:
                return 0;
        }
    }

    // 이번 판에 쓴 커피를 반영한 판정 시 HP 변화 (딜러 AI 세부 기획서 3-4장 판정 시 HP 변화).
    // 커피는 쓴 사람이 이겼을 때만 대미지를 키운다 (아이템 세부 기획서 2-1장 커피)
    public static int GetJudgmentValue(RuleSettings rules, MatchResult dealerResult, ItemState items)
    {
        int value = GetJudgmentValue(rules, dealerResult);
        bool winnerUsedCoffee = (dealerResult == MatchResult.Win && items.HasUsedItem(Side.Dealer, ItemType.Coffee))
            || (dealerResult == MatchResult.Lose && items.HasUsedItem(Side.Player, ItemType.Coffee));
        return winnerUsedCoffee ? value * rules.CoffeeMultiplier : value;
    }

    // 조합 칸 1·2번째 칸에 두 번째 판 카드를 더했을 때의 정산 시 HP 변화(딜러 기준)
    public static int GetSettlementChange(RuleSettings rules, DealerKnownInfo knownInfo, HandShape dealerThird, HandShape playerThird)
    {
        var dealerSlots = knownInfo.GetSlotShapes(Side.Dealer);
        var playerSlots = knownInfo.GetSlotShapes(Side.Player);
        CombinationType dealerCombination = CombinationRules.Judge(dealerSlots[0].Value, dealerSlots[1].Value, dealerThird);
        CombinationType playerCombination = CombinationRules.Judge(playerSlots[0].Value, playerSlots[1].Value, playerThird);
        return GetSettlementValue(rules, dealerCombination) - GetSettlementValue(rules, playerCombination);
    }

    // 조합 하나가 정산에서 상대와의 HP 차이를 얼마나 벌리는지 (트리플·공격형은 상대 대미지, 회복형은 자신 회복)
    private static int GetSettlementValue(RuleSettings rules, CombinationType combination)
    {
        switch (combination)
        {
            case CombinationType.Triple:
                return rules.TripleDamage;
            case CombinationType.AttackFullSet:
                return rules.AttackFullSetDamage;
            case CombinationType.HealFullSet:
                return rules.HealFullSetAmount;
            default:
                return 0;
        }
    }
}
