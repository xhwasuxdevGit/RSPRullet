using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class DealerAITests
{
    // 카드 순서를 한 줄로 읽기 쉽게 줄인 이름
    private const HandShape S = HandShape.Scissors;
    private const HandShape R = HandShape.Rock;
    private const HandShape P = HandShape.Paper;

    private RuleSettings _rules;
    private DealerSettings _dealerSettings;

    // 피셔-예이츠 셔플이 매번 자기 자리와 바꾸게 해, 넣은 순서 그대로 더미를 만든다
    private class KeepOrderRandom : System.Random
    {
        public override int Next(int maxValue)
        {
            return maxValue - 1;
        }
    }

    // 무작위 단계에서 규칙상 결론을 따를지 정하는 값을 고정한다 (이 값이 따를 확률보다 작으면 따른다)
    private class FixedDoubleRandom : System.Random
    {
        private readonly double _value;

        public FixedDoubleRandom(double value)
        {
            _value = value;
        }

        public override double NextDouble()
        {
            return _value;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _rules = ScriptableObject.CreateInstance<RuleSettings>();
        _dealerSettings = ScriptableObject.CreateInstance<DealerSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_rules);
        Object.DestroyImmediate(_dealerSettings);
    }

    private DealerAI MakeDealer(int seed)
    {
        return MakeDealer(new System.Random(seed));
    }

    private DealerAI MakeDealer(System.Random random)
    {
        return new DealerAI(_rules, _dealerSettings, random);
    }

    // 딜러 설정의 확률 값은 인스펙터 전용 필드라 테스트에서는 직렬화 필드를 직접 바꾼다
    private void SetFollowRates(float ruleComplianceRate, float strengthCorrectionRate)
    {
        var serialized = new SerializedObject(_dealerSettings);
        serialized.FindProperty("_ruleComplianceRate").floatValue = ruleComplianceRate;
        serialized.FindProperty("_strengthCorrectionRate").floatValue = strengthCorrectionRate;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    // 시작 카드를 나눈 뒤 firstTurnPlayer를 첫 번째 판 턴 플레이어로 둔 테이블
    private static ShuffleTable MakeTable(Side firstTurnPlayer)
    {
        var table = new ShuffleTable(new CardDeck(CardPool.Create(new System.Random(1)), new System.Random(1)));
        table.DealStartCards();
        table.SetFirstTurnPlayerByClock(firstTurnPlayer);
        return table;
    }

    [Test]
    public void TryDistribute_PlayerTurn_DoesNothing()
    {
        var table = MakeTable(Side.Player);
        Card drawn = table.DrawForTurnPlayer();

        bool distributed = MakeDealer(1).TryDistribute(table, 0);

        Assert.IsFalse(distributed);
        Assert.AreSame(drawn, table.DrawnCard);
        Assert.AreEqual(1, table.GetCombinationSlots(Side.Player).Count);
        Assert.AreEqual(1, table.GetCombinationSlots(Side.Dealer).Count);
    }

    [Test]
    public void TryDistribute_BeforeDraw_DoesNothing()
    {
        var table = MakeTable(Side.Dealer);

        bool distributed = MakeDealer(1).TryDistribute(table, 0);

        Assert.IsFalse(distributed);
        Assert.AreEqual(5, table.RemainingDeckCount);
    }

    [Test]
    public void TryDistribute_DealerTurn_DistributesDrawnCard()
    {
        var table = MakeTable(Side.Dealer);
        Card drawn = table.DrawForTurnPlayer();

        bool distributed = MakeDealer(1).TryDistribute(table, 0);

        Assert.IsTrue(distributed);
        Assert.IsNull(table.DrawnCard);
        Assert.AreEqual(2, table.GetCombinationSlots(Side.Player).Count);
        Assert.AreEqual(2, table.GetCombinationSlots(Side.Dealer).Count);
        bool kept = table.GetCombinationSlots(Side.Dealer)[1] == drawn;
        bool given = table.GetCombinationSlots(Side.Player)[1] == drawn;
        Assert.IsTrue(kept ^ given);
    }

    // 1번째 판: 플레이어가 뽑아 가진다 → 2번째 판: 딜러가 뽑은 상태.
    // 플레이어가 가진 카드(플레이어만 앎)와 딜러 자리에 엎어진 카드(아무도 모름)는 딜러가 모르는 카드다
    private static ShuffleTable MakeSecondMatchDealerDrawn()
    {
        var table = MakeTable(Side.Player);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.PassTurn(clockUsed: false);
        table.DrawForTurnPlayer();
        return table;
    }

    [Test]
    public void DealerKnownInfo_UnknownSlotCards_AreHidden()
    {
        var table = MakeSecondMatchDealerDrawn();

        var knownInfo = new DealerKnownInfo(table, matchIndex: 1);

        // 시작 카드는 앞면이라 딜러도 알고, 1번째 판에 들어간 카드는 아직 공개 전이라 모른다
        Assert.AreEqual(table.GetCombinationSlots(Side.Player)[0].Shape, knownInfo.GetSlotShapes(Side.Player)[0]);
        Assert.AreEqual(table.GetCombinationSlots(Side.Dealer)[0].Shape, knownInfo.GetSlotShapes(Side.Dealer)[0]);
        Assert.IsNull(knownInfo.GetSlotShapes(Side.Player)[1]);
        Assert.IsNull(knownInfo.GetSlotShapes(Side.Dealer)[1]);
    }

    [Test]
    public void DealerKnownInfo_HasDrawnShapeMatchIndexAndPoolComposition()
    {
        var table = MakeSecondMatchDealerDrawn();

        var knownInfo = new DealerKnownInfo(table, matchIndex: 1);

        Assert.AreEqual(table.DrawnCard.Shape, knownInfo.DrawnShape);
        Assert.AreEqual(1, knownInfo.MatchIndex);
        var poolCounts = new[] { HandShape.Scissors, HandShape.Rock, HandShape.Paper }.Select(knownInfo.GetPoolCardCount).ToList();
        Assert.AreEqual(GameRules.CardPoolSize, poolCounts.Sum());
        Assert.AreEqual(1, poolCounts.Count(count => count == GameRules.TripleShapeCardCount));
    }

    // 더미 순서: 플레이어 시작 카드 → 딜러 시작 카드 → 이후 뽑는 순서. 넣은 7장이 그대로 카드풀이 된다
    private static ShuffleTable MakeOrderedTable(params HandShape[] order)
    {
        var table = new ShuffleTable(new CardDeck(order.Select(shape => new Card(shape)), new KeepOrderRandom()));
        table.DealStartCards();
        return table;
    }

    // 1번째 판: 딜러가 턴 플레이어로 뽑은 상태
    private static ShuffleTable MakeOrderedFirstMatchDealerDrawnTable(params HandShape[] order)
    {
        var table = MakeOrderedTable(order);
        table.SetFirstTurnPlayerByClock(Side.Dealer);
        table.DrawForTurnPlayer();
        return table;
    }

    private static DealerKnownInfo MakeOrderedFirstMatchDealerDrawn(params HandShape[] order)
    {
        return new DealerKnownInfo(MakeOrderedFirstMatchDealerDrawnTable(order), matchIndex: 0);
    }

    // 1번째 판을 firstTurnPlayer가 뽑아 가지고 카드를 공개한 뒤, 2번째 판에 딜러가 뽑은 상태.
    // firstTurnPlayer가 딜러면 시계를 쓴 것으로 보고 2번째 판도 딜러 차례로 둔다
    private static ShuffleTable MakeOrderedSecondMatchDealerDrawnTable(Side firstTurnPlayer, params HandShape[] order)
    {
        var table = MakeOrderedTable(order);
        table.SetFirstTurnPlayerByClock(firstTurnPlayer);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.RevealMatchCards();
        table.PassTurn(clockUsed: firstTurnPlayer == Side.Dealer);
        table.DrawForTurnPlayer();
        return table;
    }

    private static DealerKnownInfo MakeOrderedSecondMatchDealerDrawn(Side firstTurnPlayer, params HandShape[] order)
    {
        return new DealerKnownInfo(MakeOrderedSecondMatchDealerDrawnTable(firstTurnPlayer, order), matchIndex: 1);
    }

    // 딜러가 분배한 뒤 뽑은 카드가 딜러의 이번 판 조합 칸에 들어갔는지 (가진다면 true)
    private static bool DistributeAndCheckKept(DealerAI dealer, ShuffleTable table, int matchIndex)
    {
        Card drawn = table.DrawnCard;
        dealer.TryDistribute(table, matchIndex);
        return table.GetCombinationSlots(Side.Dealer)[matchIndex + 1] == drawn;
    }

    // 딜러 AI 세부 기획서 6장 예시 5 (딜러 AI 세부 기획서 3-3장 A-1): 시작 카드 바위(3장), 뽑은 카드 바위 → 트리플 가능성 유지
    [Test]
    public void JudgeCombination_FirstMatch_TripleShapeStartAndSameDrawn_Keeps()
    {
        var knownInfo = MakeOrderedFirstMatchDealerDrawn(S, R, R, R, S, P, P);

        Assert.AreEqual(DealerChoice.Keep, MakeDealer(1).JudgeCombination(knownInfo));
    }

    // 딜러 AI 세부 기획서 3-3장 A-2: 시작 카드 바위(3장), 뽑은 카드 보 → 상관없음
    [Test]
    public void JudgeCombination_FirstMatch_TripleShapeStartAndDifferentDrawn_ReturnsEither()
    {
        var knownInfo = MakeOrderedFirstMatchDealerDrawn(S, R, P, R, R, S, P);

        Assert.AreEqual(DealerChoice.Either, MakeDealer(1).JudgeCombination(knownInfo));
    }

    // 딜러 AI 세부 기획서 3-3장 B-1: 시작 카드 보(2장), 뽑은 카드 가위 → 풀세트 가능성 유지
    [Test]
    public void JudgeCombination_FirstMatch_TwoCardShapeStartAndDifferentDrawn_Keeps()
    {
        var knownInfo = MakeOrderedFirstMatchDealerDrawn(R, P, S, R, R, S, P);

        Assert.AreEqual(DealerChoice.Keep, MakeDealer(1).JudgeCombination(knownInfo));
    }

    // 딜러 AI 세부 기획서 6장 예시 1 (딜러 AI 세부 기획서 3-3장 B-2): 시작 카드 보(2장), 뽑은 카드 보 → 가지면 조합이 사라짐
    [Test]
    public void JudgeCombination_FirstMatch_TwoCardShapeStartAndSameDrawn_Gives()
    {
        var knownInfo = MakeOrderedFirstMatchDealerDrawn(R, P, P, R, R, S, S);

        Assert.AreEqual(DealerChoice.Give, MakeDealer(1).JudgeCombination(knownInfo));
    }

    // 딜러 AI 세부 기획서 6장 예시 2: 딜러 가위→바위, 플레이어 바위→보, 뽑은 카드 바위, 모르는 카드 가위·보 1장씩.
    // 가지면 정산 합 −1 (플레이어 공격형), 주면 +1 (딜러 공격형) → 카드를 준다
    [Test]
    public void JudgeCombination_SecondMatch_Example2_Gives()
    {
        var knownInfo = MakeOrderedSecondMatchDealerDrawn(Side.Player, R, S, P, R, R, S, P);

        Assert.AreEqual(1, knownInfo.GetUnknownCardCount(S));
        Assert.AreEqual(0, knownInfo.GetUnknownCardCount(R));
        Assert.AreEqual(1, knownInfo.GetUnknownCardCount(P));
        Assert.AreEqual(DealerChoice.Give, MakeDealer(1).JudgeCombination(knownInfo));
    }

    // 딜러 AI 세부 기획서 6장 예시 3: 딜러 보→보, 플레이어 바위→바위, 뽑은 카드 보, 모르는 카드 가위 2장.
    // 가지면 딜러 트리플(+3 × 2장), 주면 양쪽 조합 없음(0) → 카드를 가진다
    [Test]
    public void JudgeCombination_SecondMatch_Example3_Keeps()
    {
        var knownInfo = MakeOrderedSecondMatchDealerDrawn(Side.Dealer, R, P, P, R, P, S, S);

        Assert.AreEqual(2, knownInfo.GetUnknownCardCount(S));
        Assert.AreEqual(DealerChoice.Keep, MakeDealer(1).JudgeCombination(knownInfo));
    }

    // 딜러 가위→가위, 플레이어 보→보, 뽑은 카드 바위, 모르는 카드 바위 2장: 어느 쪽이든 양쪽 모두 조합 없음 → 상관없음
    [Test]
    public void JudgeCombination_SecondMatch_SameSettlementChange_ReturnsEither()
    {
        var knownInfo = MakeOrderedSecondMatchDealerDrawn(Side.Dealer, P, S, S, P, R, R, R);

        Assert.AreEqual(DealerChoice.Either, MakeDealer(1).JudgeCombination(knownInfo));
    }

    // 딜러 AI 세부 기획서 6장 예시 1: 카드풀 바위 3, 뽑은 카드 보 → 보는 3장인 바위를 이김 → 카드를 가진다.
    // 공개된 카드와 뽑은 카드를 빼고 따졌다면 남은 카드는 바위 2·가위 2라 상관없음이 되므로,
    // 1번째 판에는 나온 카드를 따지지 않고 셔플 직후 구성 그대로 본다는 것도 함께 확인한다 (딜러 AI 세부 기획서 3-1장)
    [Test]
    public void JudgeThisMatch_FirstMatch_DrawnBeatsTripleShape_KeepsWithoutCounting()
    {
        var knownInfo = MakeOrderedFirstMatchDealerDrawn(R, P, P, R, R, S, S);

        Assert.AreEqual(2, knownInfo.GetUnknownCardCount(R));
        Assert.AreEqual(2, knownInfo.GetUnknownCardCount(S));
        Assert.AreEqual(DealerChoice.Keep, MakeDealer(1).JudgeThisMatch(knownInfo));
    }

    // 딜러 AI 세부 기획서 3-2장: 카드풀 바위 3, 뽑은 카드 가위 → 가위는 3장인 바위에게 짐 → 카드를 준다
    [Test]
    public void JudgeThisMatch_FirstMatch_DrawnLosesToTripleShape_Gives()
    {
        var knownInfo = MakeOrderedFirstMatchDealerDrawn(R, P, S, R, R, S, P);

        Assert.AreEqual(DealerChoice.Give, MakeDealer(1).JudgeThisMatch(knownInfo));
    }

    // 딜러 AI 세부 기획서 6장 예시 5: 카드풀 바위 3, 뽑은 카드 바위 → 3장인 손모양 자체라 이기는 수와 지는 수가 2장씩 같음 → 상관없음
    [Test]
    public void JudgeThisMatch_FirstMatch_DrawnIsTripleShape_ReturnsEither()
    {
        var knownInfo = MakeOrderedFirstMatchDealerDrawn(S, R, R, R, S, P, P);

        Assert.AreEqual(DealerChoice.Either, MakeDealer(1).JudgeThisMatch(knownInfo));
    }

    // 딜러 AI 세부 기획서 6장 예시 2: 뽑은 카드 바위, 모르는 카드 가위·보 1장씩 → 이기는 수와 지는 수가 같음 → 상관없음
    [Test]
    public void JudgeThisMatch_SecondMatch_Example2_ReturnsEither()
    {
        var knownInfo = MakeOrderedSecondMatchDealerDrawn(Side.Player, R, S, P, R, R, S, P);

        Assert.AreEqual(DealerChoice.Either, MakeDealer(1).JudgeThisMatch(knownInfo));
    }

    // 딜러 AI 세부 기획서 6장 예시 3: 뽑은 카드 보, 모르는 카드 가위 2장 → 지는 손모양만 남음 → 카드를 준다.
    // 1번째 판처럼 셔플 직후 구성(보 3 / 가위 2 / 바위 2)으로 봤다면 상관없음이 되므로,
    // 2번째 판에는 공개된 카드와 뽑은 카드를 빼고 따지는 것도 함께 확인한다
    [Test]
    public void JudgeThisMatch_SecondMatch_Example3_GivesByUnknownCards()
    {
        var knownInfo = MakeOrderedSecondMatchDealerDrawn(Side.Dealer, R, P, P, R, P, S, S);

        Assert.AreEqual(DealerChoice.Give, MakeDealer(1).JudgeThisMatch(knownInfo));
    }

    // 딜러 AI 세부 기획서 3-2장: 뽑은 카드 바위, 모르는 카드 바위 2장 → 비기는 손모양은 세지 않아 0 대 0 → 상관없음
    [Test]
    public void JudgeThisMatch_SecondMatch_OnlyDrawShapesUnknown_ReturnsEither()
    {
        var knownInfo = MakeOrderedSecondMatchDealerDrawn(Side.Dealer, P, S, S, P, R, R, R);

        Assert.AreEqual(2, knownInfo.GetUnknownCardCount(R));
        Assert.AreEqual(DealerChoice.Either, MakeDealer(1).JudgeThisMatch(knownInfo));
    }

    // 딜러 AI 세부 기획서 6장 예시 1: ② 가진다, ① 준다로 반대 → 1번째 판은 ② 우선 → 카드를 가진다
    [Test]
    public void TryDistribute_FirstMatch_Example1_FollowsThisMatchOverCombination()
    {
        var table = MakeOrderedFirstMatchDealerDrawnTable(R, P, P, R, R, S, S);

        Assert.IsTrue(DistributeAndCheckKept(MakeDealer(1), table, 0));
    }

    // 딜러 AI 세부 기획서 6장 예시 5: ② 상관없음, ① 가진다 → ①만 결론 → 카드를 가진다
    [Test]
    public void TryDistribute_FirstMatch_Example5_FollowsCombinationOnly()
    {
        var table = MakeOrderedFirstMatchDealerDrawnTable(S, R, R, R, S, P, P);

        Assert.IsTrue(DistributeAndCheckKept(MakeDealer(1), table, 0));
    }

    // 딜러 AI 세부 기획서 6장 예시 2: 예상 HP 변화 가지면 −1, 주면 +1 → 카드를 준다
    [Test]
    public void TryDistribute_SecondMatch_Example2_Gives()
    {
        var table = MakeOrderedSecondMatchDealerDrawnTable(Side.Player, R, S, P, R, R, S, P);

        Assert.IsFalse(DistributeAndCheckKept(MakeDealer(1), table, 1));
    }

    // 딜러 AI 세부 기획서 6장 예시 3: 예상 HP 변화 가지면 +4, 주면 +2 → 이번 판은 지지만 트리플로 카드를 가진다
    [Test]
    public void TryDistribute_SecondMatch_Example3_Keeps()
    {
        var table = MakeOrderedSecondMatchDealerDrawnTable(Side.Dealer, R, P, P, R, P, S, S);

        Assert.IsTrue(DistributeAndCheckKept(MakeDealer(1), table, 1));
    }

    // 딜러 AI 세부 기획서 3-5장 1번째 판 같은 방향: 카드풀 바위 3, 딜러 시작 카드 가위(2장), 뽑은 카드 보.
    // ② 보가 3장인 바위를 이겨 가진다, ① B-1 가진다 → 규칙 준수율이 낮아도 항상 따른다
    [Test]
    public void TryDistribute_FirstMatch_SameDirection_AlwaysFollows()
    {
        SetFollowRates(ruleComplianceRate: 0.5f, strengthCorrectionRate: 0.5f);
        var table = MakeOrderedFirstMatchDealerDrawnTable(S, S, P, R, R, R, P);

        Assert.IsTrue(DistributeAndCheckKept(MakeDealer(new FixedDoubleRandom(0.99)), table, 0));
    }

    // 딜러 AI 세부 기획서 3-5장 1번째 판 반대 (예시 1): 규칙 준수율 0.5 → 0.5보다 작게 뽑히면 따르고, 아니면 반대 선택
    [TestCase(0.4, true)]
    [TestCase(0.6, false)]
    public void TryDistribute_FirstMatch_Opposite_UsesRuleComplianceRate(double randomValue, bool expectedKept)
    {
        SetFollowRates(ruleComplianceRate: 0.5f, strengthCorrectionRate: 1f);
        var table = MakeOrderedFirstMatchDealerDrawnTable(R, P, P, R, R, S, S);

        Assert.AreEqual(expectedKept, DistributeAndCheckKept(MakeDealer(new FixedDoubleRandom(randomValue)), table, 0));
    }

    // 딜러 AI 세부 기획서 3-5장 1번째 판 ①만 결론 (예시 5): 규칙 준수율 0.5
    [TestCase(0.4, true)]
    [TestCase(0.6, false)]
    public void TryDistribute_FirstMatch_CombinationOnly_UsesRuleComplianceRate(double randomValue, bool expectedKept)
    {
        SetFollowRates(ruleComplianceRate: 0.5f, strengthCorrectionRate: 1f);
        var table = MakeOrderedFirstMatchDealerDrawnTable(S, R, R, R, S, P, P);

        Assert.AreEqual(expectedKept, DistributeAndCheckKept(MakeDealer(new FixedDoubleRandom(randomValue)), table, 0));
    }

    // 딜러 AI 세부 기획서 3-5장 2번째 판 선택 간 HP 차이 2 이상 (예시 2, 규칙상 결론 준다):
    // 강함 보정률만 쓰고 규칙 준수율은 쓰지 않는다
    [TestCase(1f, 0.5f, true)]
    [TestCase(0.5f, 1f, false)]
    public void TryDistribute_SecondMatch_HealthGapTwo_UsesStrengthCorrectionRate(
        float ruleComplianceRate, float strengthCorrectionRate, bool expectedKept)
    {
        SetFollowRates(ruleComplianceRate, strengthCorrectionRate);
        var table = MakeOrderedSecondMatchDealerDrawnTable(Side.Player, R, S, P, R, R, S, P);

        Assert.AreEqual(expectedKept, DistributeAndCheckKept(MakeDealer(new FixedDoubleRandom(0.6)), table, 1));
    }

    // 딜러 AI 세부 기획서 3-5장 결론 없음: 딜러 가위→가위, 플레이어 보→보, 뽑은 카드 바위, 모르는 카드 바위 2장.
    // 예상 HP 변화가 같아 50:50이므로 시드 20개 중 가진다와 준다가 모두 나온다
    [Test]
    public void TryDistribute_SecondMatch_SameExpectedChange_ChoosesBothAcrossSeeds()
    {
        var keptResults = Enumerable.Range(0, 20).Select(seed =>
        {
            var table = MakeOrderedSecondMatchDealerDrawnTable(Side.Dealer, P, S, S, P, R, R, R);
            return DistributeAndCheckKept(MakeDealer(seed), table, 1);
        }).ToList();

        Assert.Contains(true, keptResults);
        Assert.Contains(false, keptResults);
    }
}
