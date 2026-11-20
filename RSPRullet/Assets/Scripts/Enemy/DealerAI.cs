using UnityEngine;

// 딜러 AI (딜러 AI 세부 기획서). 딜러의 판단을 이 클래스 한 곳에 모은다 (딜러 AI 세부 기획서 2장 공통 원칙).
// 카드 분배 판단(딜러 AI 세부 기획서 7-2장 D1)은 이 클래스가, 아이템·교환 판단(D2·D3)은 아이템 판단(DealerItemJudge)이 맡는다.
// 바깥에서는 딜러 판단을 모두 이 클래스로 부른다. 카드를 세는 방법과 HP 변화 계산은 DealerEstimateRules를 함께 쓴다.
// 딜러는 선택만 하고, 카드를 어느 자리에 놓는지는 테이블 상태가, 아이템을 쓴 결과의 적용은 아이템 진행이 처리한다.
public class DealerAI
{
    private const int FirstMatchIndex = DealerEstimateRules.FirstMatchIndex;

    // 반대로 갈 근거가 없는 상황에서 규칙상 결론을 따를 확률 (딜러 AI 세부 기획서 3-5장 항상 따름)
    private const float AlwaysFollowRate = 1f;

    // 2번째 판에서 선택 간 HP 차이가 이 값 이상이면 강함 보정률을 쓴다 (딜러 AI 세부 기획서 3-5장)
    private const int StrengthCorrectionHealthGap = 2;

    private readonly RuleSettings _rules;
    private readonly DealerSettings _dealerSettings;
    private readonly System.Random _random;
    private readonly DealerItemJudge _itemJudge;

    // 판정·정산 대미지와 회복량은 실제 진행과 같은 값(RuleSettings)으로 계산한다.
    // 무작위 단계의 확률(규칙 준수율·강함 보정률)은 딜러 설정(DealerSettings)에서 읽는다.
    // 같은 시드의 난수를 넘기면 같은 선택이 나오므로 테스트에서 상황을 재현할 수 있다
    public DealerAI(RuleSettings rules, DealerSettings dealerSettings, System.Random random)
    {
        _rules = rules;
        _dealerSettings = dealerSettings;
        _random = random;
        _itemJudge = new DealerItemJudge(rules, dealerSettings, random);
    }

    // 딜러가 턴 플레이어이고 뽑은 카드가 있을 때만 분배한다. 그 외에는 아무것도 하지 않고 false를 돌려준다.
    // 판단에는 테이블 대신 딜러가 아는 정보(DealerKnownInfo)만 넘긴다
    public bool TryDistribute(TableState table, int matchIndex)
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

    // 아이템 판단 (딜러 AI 세부 기획서 7-2장 D2). 다음에 쓸 아이템, 더 쓰지 않으면 null(아이템 끝내기)
    public ItemType? ChooseItem(DealerKnownInfo knownInfo, ItemState items, Side turnPlayer)
    {
        return _itemJudge.ChooseItem(knownInfo, items, turnPlayer);
    }

    // 교환 판단 (딜러 AI 세부 기획서 7-2장 D3). 돋보기로 상대 카드를 안 뒤에 부르며, 교환하면 true
    public bool ChooseExchange(DealerKnownInfo knownInfo, ItemState items)
    {
        return _itemJudge.ChooseExchange(knownInfo, items);
    }

    // ② 이번 판 승패 판단 규칙 (딜러 AI 세부 기획서 3-2장).
    // 뽑은 카드가 이기는 손모양의 수와 지는 손모양의 수를 비교해 이번 판을 이길 가능성이 높은 쪽을 돌려준다.
    // 비기는 손모양은 세지 않고, 두 수가 같으면 상관없음이다
    public DealerChoice JudgeThisMatch(DealerKnownInfo knownInfo)
    {
        int winCount = 0;
        int loseCount = 0;

        foreach (HandShape shape in System.Enum.GetValues(typeof(HandShape)))
        {
            if (HandShapeRules.Beats(knownInfo.DrawnShape.Value, shape))
            {
                winCount += DealerEstimateRules.GetThisMatchCardCount(knownInfo, shape);
            }
            else if (HandShapeRules.Beats(shape, knownInfo.DrawnShape.Value))
            {
                loseCount += DealerEstimateRules.GetThisMatchCardCount(knownInfo, shape);
            }
        }

        if (winCount == loseCount)
        {
            return DealerChoice.Either;
        }

        return winCount > loseCount ? DealerChoice.Keep : DealerChoice.Give;
    }

    // ① 조합 판단 규칙 (딜러 AI 세부 기획서 3-3장). 뽑은 카드를 가질 때와 줄 때 중 카드 조합에 더 유리한 쪽을 돌려준다
    public DealerChoice JudgeCombination(DealerKnownInfo knownInfo)
    {
        return knownInfo.MatchIndex == FirstMatchIndex
            ? JudgeFirstMatchCombination(knownInfo)
            : JudgeSecondMatchCombination(knownInfo);
    }

    // 카드 분배 판단 (딜러 AI 세부 기획서 3장). 딜러가 아는 정보만 받아 가진다(true) / 준다(false)를 돌려준다.
    // ③ 충돌 해결 규칙으로 규칙상 결론을 정하고, 무작위 단계를 거쳐 실제 행동을 고른다 (딜러 AI 세부 기획서 1장 판단 흐름)
    private bool ChooseKeep(DealerKnownInfo knownInfo)
    {
        return knownInfo.MatchIndex == FirstMatchIndex
            ? ChooseFirstMatchKeep(knownInfo)
            : ChooseSecondMatchKeep(knownInfo);
    }

    // 1번째 판: "②-승패 판정"을 우선하고, "②-승패 판정"가 상관없음이면, "①-카드조합"을 따른다 (딜러 AI 세부 기획서 3-4장 1번째 판 표).
    // "①-카드조합"이 같은 방향이거나 상관없음이면 항상 따르고, "①-카드조합"이 반대이거나 "①-카드조합"만 결론이면 규칙 준수율을 쓴다 (딜러 AI 세부 기획서 3-5장).
    // "②-승패 판정"가 상관없음이면 뽑은 카드가 3장인 손모양이다.
    // 시작 카드가 같은 3장인 손모양이면 A-1, 2장인 손모양이면 뽑은 카드와 다르므로 B-1이 되어 "①-카드조합"은 항상 가진다
    private bool ChooseFirstMatchKeep(DealerKnownInfo knownInfo)
    {
        DealerChoice consideredThisMatch = JudgeThisMatch(knownInfo);
        DealerChoice consideredCombination = JudgeCombination(knownInfo);

        if (consideredThisMatch == DealerChoice.Either)
        {
            return ApplyRandomRatio(consideredCombination, _dealerSettings.RuleComplianceRate);
        }

        bool hasNoOpposingReason = consideredCombination == consideredThisMatch || consideredCombination == DealerChoice.Either;
        float followRate = hasNoOpposingReason ? AlwaysFollowRate : _dealerSettings.RuleComplianceRate;
        return ApplyRandomRatio(consideredThisMatch, followRate);
    }

    // 2번째 판: 예상 HP 변화가 큰 쪽이 규칙상 결론이고, 같으면 결론 없음이다 (딜러 AI 세부 기획서 3-4장 2번째 판).
    // 선택 간 HP 차이가 1이면 규칙 준수율, 2 이상이면 강함 보정률을 쓴다 (딜러 AI 세부 기획서 3-5장)
    private bool ChooseSecondMatchKeep(DealerKnownInfo knownInfo)
    {
        int changeByKeep = SumExpectedChange(knownInfo, keep: true);
        int changeByGive = SumExpectedChange(knownInfo, keep: false);

        if (changeByKeep == changeByGive)
        {
            return ApplyRandomRatio(DealerChoice.Either, AlwaysFollowRate);
        }

        DealerChoice conclusion = changeByKeep > changeByGive ? DealerChoice.Keep : DealerChoice.Give;
        int healthGap = System.Math.Abs(changeByKeep - changeByGive);
        float followRate = healthGap >= StrengthCorrectionHealthGap
            ? _dealerSettings.StrengthCorrectionRate
            : _dealerSettings.RuleComplianceRate;
        return ApplyRandomRatio(conclusion, followRate);
    }

    // 무작위 단계 (딜러 AI 세부 기획서 3-5장). 규칙상 결론을 followRate 확률로 따르고, 나머지 확률로는 반대 선택을 한다.
    // 결론 없음(상관없음)이면 followRate와 상관없이 50:50으로 정한다 (딜러 AI 세부 기획서 7-1-2장)
    private bool ApplyRandomRatio(DealerChoice conclusion, float followRate)
    {
        if (conclusion == DealerChoice.Either)
        {
            return _random.Next(2) == 0;
        }

        bool ruleKeep = conclusion == DealerChoice.Keep;
        bool follows = _random.NextDouble() < followRate;
        Debug.Log($"[DealerAI] 규칙상 결론: {(ruleKeep ? "가진다" : "준다")}, 따를 확률 {followRate}, {(follows ? "따름" : "반대 선택")}");
        return follows ? ruleKeep : !ruleKeep;
    }

    // 예상 HP 변화 = 판정 시 HP 변화 + 정산 시 HP 변화 (딜러 AI 세부 기획서 3-4장 2번째 판 표).
    // 분배는 그 판의 아이템 단계보다 먼저라 이번 판에 쓴 커피가 없으므로, 기본 대미지로 계산한다.
    private int SumExpectedChange(DealerKnownInfo knownInfo, bool keep)
    {
        return SumJudgmentChange(knownInfo, keep) + SumSettlementChange(knownInfo, keep);
    }

    // 딜러가 모르는 카드를 한 장씩 상대 자리에 넣어 본 판정 시 HP 변화(딜러 기준)의 합.
    // 가지면 딜러는 뽑은 카드, 플레이어는 모르는 카드로 겨루고, 주면 그 반대다
    private int SumJudgmentChange(DealerKnownInfo knownInfo, bool keep)
    {
        int sum = 0;

        foreach (HandShape unknownShape in System.Enum.GetValues(typeof(HandShape)))
        {
            HandShape dealerShape = keep ? knownInfo.DrawnShape.Value : unknownShape;
            HandShape playerShape = keep ? unknownShape : knownInfo.DrawnShape.Value;
            sum += DealerEstimateRules.GetJudgmentValue(_rules, HandShapeRules.Compare(dealerShape, playerShape)) * knownInfo.GetUnknownCardCount(unknownShape);
        }

        return sum;
    }

    // 1번째 판: 시작 카드가 3장인 손모양인지와 뽑은 카드가 시작 카드와 같은지로 정한다.
    // 상대 조합은 보지 않는다 (딜러 AI 세부 기획서 3-3장 1번째 판 표 A-1·A-2·B-1·B-2)
    private static DealerChoice JudgeFirstMatchCombination(DealerKnownInfo knownInfo)
    {
        HandShape startShape = knownInfo.GetSlotShapes(Side.Dealer)[0].Value;
        bool isStartTripleShape = knownInfo.GetPoolCardCount(startShape) == GameRules.TripleShapeCardCount;
        bool isSameAsStart = knownInfo.DrawnShape.Value == startShape;

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
        int changeByKeep = SumSettlementChange(knownInfo, keep: true);
        int changeByGive = SumSettlementChange(knownInfo, keep: false);

        if (changeByKeep == changeByGive)
        {
            return DealerChoice.Either;
        }

        return changeByKeep > changeByGive ? DealerChoice.Keep : DealerChoice.Give;
    }

    // 딜러가 모르는 카드를 한 장씩 남은 빈자리에 넣어 본 정산 시 HP 변화(딜러 기준)의 합.
    // 가지면 뽑은 카드가 딜러 자리, 모르는 카드가 플레이어 자리에 들어가고, 주면 그 반대다.
    // ③ 충돌 해결의 예상 HP 변화(SumExpectedChange)도 이 값을 쓴다 (딜러 AI 세부 기획서 3-4장)
    private int SumSettlementChange(DealerKnownInfo knownInfo, bool keep)
    {
        int sum = 0;

        foreach (HandShape unknownShape in System.Enum.GetValues(typeof(HandShape)))
        {
            HandShape dealerThird = keep ? knownInfo.DrawnShape.Value : unknownShape;
            HandShape playerThird = keep ? unknownShape : knownInfo.DrawnShape.Value;
            sum += DealerEstimateRules.GetSettlementChange(_rules, knownInfo, dealerThird, playerThird) * knownInfo.GetUnknownCardCount(unknownShape);
        }

        return sum;
    }
}
