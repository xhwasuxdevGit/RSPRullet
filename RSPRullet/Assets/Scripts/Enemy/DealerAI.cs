using UnityEngine;

// 딜러 AI (딜러 AI 세부 기획서). 딜러의 판단을 이 클래스 한 곳에 모은다 (딜러 AI 세부 기획서 2장 공통 원칙).
// 지금은 카드 분배 판단(딜러 AI 세부 기획서 7-2장 D1)만 있다. 아이템·교환 판단은 이후 이슈에서 이 클래스에 더한다.
// 딜러는 선택만 하고, 카드를 어느 자리에 놓는지는 셔플 테이블이 처리한다.
public class DealerAI
{
    private const int FirstMatchIndex = 0;

    private readonly RuleSettings _rules;
    private readonly System.Random _random;

    // 정산 대미지·회복량은 실제 정산과 같은 값(RuleSettings)으로 계산한다.
    // 같은 시드의 난수를 넘기면 같은 선택이 나오므로 테스트에서 상황을 재현할 수 있다
    public DealerAI(RuleSettings rules, System.Random random)
    {
        _rules = rules;
        _random = random;
    }

    // 딜러가 턴 플레이어이고 뽑은 카드가 있을 때만 분배한다. 그 외에는 아무것도 하지 않고 false를 돌려준다.
    // 판단에는 테이블 대신 딜러가 아는 정보(DealerKnownInfo)만 넘긴다
    public bool TryDistribute(ShuffleTable table, int matchIndex)
    {
        if (table.TurnPlayer != Side.Dealer || table.DrawnCard == null)
        {
            Debug.Log("[DealerAI] 딜러가 분배할 차례가 아니라 무시");
            return false;
        }

        bool keep = ChooseKeep(new DealerKnownInfo(table, matchIndex));
        Debug.Log($"[DealerAI] 분배: {(keep ? "가진다" : "준다")}");
        table.Distribute(keep);
        return true;
    }

    // ① 조합 판단 규칙 (딜러 AI 세부 기획서 3-3장). 뽑은 카드를 가질 때와 줄 때 중 카드 조합에 더 유리한 쪽을 돌려준다
    public DealerChoice JudgeCombination(DealerKnownInfo knownInfo)
    {
        return knownInfo.MatchIndex == FirstMatchIndex
            ? JudgeFirstMatchCombination(knownInfo)
            : JudgeSecondMatchCombination(knownInfo);
    }

    // 카드 분배 판단 (딜러 AI 세부 기획서 3장). 딜러가 아는 정보만 받아 가진다(true) / 준다(false)를 돌려준다.
    // ② 이번 판 판단(RSP-46), ③ 충돌 해결과 무작위 단계(RSP-47)가 들어오기 전까지는 50:50으로 고른다.
    // ① 조합 판단(JudgeCombination)은 ③ 충돌 해결(RSP-47)에서 이 메서드에 연결한다
    private bool ChooseKeep(DealerKnownInfo knownInfo)
    {
        return _random.Next(2) == 0;
    }

    // 1번째 판: 시작 카드가 3장인 손모양인지와 뽑은 카드가 시작 카드와 같은지로 정한다.
    // 상대 조합은 보지 않는다 (딜러 AI 세부 기획서 3-3장 1번째 판 표 A-1·A-2·B-1·B-2)
    private static DealerChoice JudgeFirstMatchCombination(DealerKnownInfo knownInfo)
    {
        HandShape startShape = knownInfo.GetSlotShapes(Side.Dealer)[0].Value;
        bool isStartTripleShape = knownInfo.GetPoolCardCount(startShape) == GameRules.TripleShapeCardCount;
        bool isSameAsStart = knownInfo.DrawnShape == startShape;

        if (isStartTripleShape)
        {
            // A-1: 트리플 가능성 유지 / A-2: 어느 카드를 받아도 조합 가능성이 남음
            return isSameAsStart ? DealerChoice.Keep : DealerChoice.Either;
        }

        // B-1: 풀세트 가능성 유지 / B-2: 가지면 2장짜리 손모양이 2장 쌓여 조합이 사라짐
        return isSameAsStart ? DealerChoice.Give : DealerChoice.Keep;
    }

    // 2번째 판: 정산 시 HP 변화 합이 큰 쪽을 고르고, 같으면 상관없음 (딜러 AI 세부 기획서 3-3장 2번째 판 표)
    private DealerChoice JudgeSecondMatchCombination(DealerKnownInfo knownInfo)
    {
        int keepChange = SumSettlementChange(knownInfo, keep: true);
        int giveChange = SumSettlementChange(knownInfo, keep: false);

        if (keepChange == giveChange)
        {
            return DealerChoice.Either;
        }

        return keepChange > giveChange ? DealerChoice.Keep : DealerChoice.Give;
    }

    // 딜러가 모르는 카드를 한 장씩 남은 빈자리에 넣어 본 정산 시 HP 변화(딜러 기준)의 합.
    // 가지면 뽑은 카드가 딜러 자리, 모르는 카드가 플레이어 자리에 들어가고, 주면 그 반대다.
    // ③ 충돌 해결(RSP-47)의 예상 HP 변화도 이 값에 판정 시 HP 변화를 더해 구한다 (딜러 AI 세부 기획서 3-4장)
    private int SumSettlementChange(DealerKnownInfo knownInfo, bool keep)
    {
        var dealerSlots = knownInfo.GetSlotShapes(Side.Dealer);
        var playerSlots = knownInfo.GetSlotShapes(Side.Player);
        int sum = 0;

        foreach (HandShape unknownShape in System.Enum.GetValues(typeof(HandShape)))
        {
            HandShape dealerThird = keep ? knownInfo.DrawnShape : unknownShape;
            HandShape playerThird = keep ? unknownShape : knownInfo.DrawnShape;
            CombinationType dealerCombination = CombinationRules.Judge(dealerSlots[0].Value, dealerSlots[1].Value, dealerThird);
            CombinationType playerCombination = CombinationRules.Judge(playerSlots[0].Value, playerSlots[1].Value, playerThird);

            int changePerCard = GetSettlementValue(dealerCombination) - GetSettlementValue(playerCombination);
            sum += changePerCard * knownInfo.GetUnknownCardCount(unknownShape);
        }

        return sum;
    }

    // 조합 하나가 정산에서 상대와의 HP 차이를 얼마나 벌리는지 (트리플·공격형은 상대 대미지, 회복형은 자신 회복)
    private int GetSettlementValue(CombinationType combination)
    {
        switch (combination)
        {
            case CombinationType.Triple:
                return _rules.TripleDamage;
            case CombinationType.AttackFullSet:
                return _rules.AttackFullSetDamage;
            case CombinationType.HealFullSet:
                return _rules.HealFullSetAmount;
            default:
                return 0;
        }
    }
}
