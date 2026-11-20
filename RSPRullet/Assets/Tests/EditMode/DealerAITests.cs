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
    private static TableState MakeTable(Side firstTurnPlayer)
    {
        var table = new TableState(new CardDeck(CardPool.Create(new System.Random(1)), new System.Random(1)));
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
    private static TableState MakeSecondMatchDealerDrawn()
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

    // 1번째 판: 플레이어가 턴 플레이어로 보를 뽑아 가진 아이템 단계 (플레이어 [가위, 보], 딜러 [바위, 가위]).
    // 플레이어 자리의 보는 플레이어만, 딜러 자리의 가위는 아무도 모른다
    private static TableState MakeOrderedFirstMatchPlayerKeptTable()
    {
        var table = MakeOrderedTable(S, R, P, S, R, P, R);
        table.SetFirstTurnPlayerByClock(Side.Player);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        return table;
    }

    [Test]
    public void DealerKnownInfo_AfterDistribute_HasNoDrawnShapeAndHidesMatchCards()
    {
        var table = MakeOrderedFirstMatchPlayerKeptTable();

        var knownInfo = new DealerKnownInfo(table, matchIndex: 0);

        Assert.IsNull(knownInfo.DrawnShape);
        Assert.IsNull(knownInfo.GetSlotShapes(Side.Player)[1]);
        Assert.IsNull(knownInfo.GetSlotShapes(Side.Dealer)[1]);
    }

    [Test]
    public void DealerKnownInfo_AfterDealerPeek_KnowsPlayerMatchCard()
    {
        var table = MakeOrderedFirstMatchPlayerKeptTable();
        table.PeekOpponentMatchCard(Side.Dealer);

        var knownInfo = new DealerKnownInfo(table, matchIndex: 0);

        Assert.AreEqual(P, knownInfo.GetSlotShapes(Side.Player)[1]);
        Assert.IsNull(knownInfo.GetSlotShapes(Side.Dealer)[1]);
    }

    // 교환하면 아는 사람 정보가 카드를 따라가므로, 딜러가 본 보는 딜러 자리로, 모르는 가위는 플레이어 자리로 간다
    [Test]
    public void DealerKnownInfo_AfterDealerPeekAndSwap_KnownCardMovesToDealerSlot()
    {
        var table = MakeOrderedFirstMatchPlayerKeptTable();
        table.PeekOpponentMatchCard(Side.Dealer);
        table.SwapMatchCards();

        var knownInfo = new DealerKnownInfo(table, matchIndex: 0);

        Assert.AreEqual(P, knownInfo.GetSlotShapes(Side.Dealer)[1]);
        Assert.IsNull(knownInfo.GetSlotShapes(Side.Player)[1]);
    }

    // 더미 순서: 플레이어 시작 카드 → 딜러 시작 카드 → 이후 뽑는 순서. 넣은 7장이 그대로 카드풀이 된다
    private static TableState MakeOrderedTable(params HandShape[] order)
    {
        var table = new TableState(new CardDeck(order.Select(shape => new Card(shape)), new KeepOrderRandom()));
        table.DealStartCards();
        return table;
    }

    // 1번째 판: 딜러가 턴 플레이어로 뽑은 상태
    private static TableState MakeOrderedFirstMatchDealerDrawnTable(params HandShape[] order)
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
    private static TableState MakeOrderedSecondMatchDealerDrawnTable(Side firstTurnPlayer, params HandShape[] order)
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
    private static bool DistributeAndCheckKept(DealerAI dealer, TableState table, int matchIndex)
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

    // 딜러 보유 칸에 items를 넣은 아이템 상태 (보유 한도는 2라운드 값)
    private static ItemState MakeDealerItems(params ItemType[] items)
    {
        var itemState = new ItemState(4);
        foreach (ItemType item in items)
        {
            itemState.TryAddItem(Side.Dealer, item);
        }
        return itemState;
    }

    // 딜러 AI 세부 기획서 6장 예시 3을 가진다로 분배한 아이템 단계: 딜러 [보, 보, 보], 플레이어 [바위, 바위, 가위(딜러 모름)]
    private static TableState MakeExample4Table()
    {
        var table = MakeOrderedSecondMatchDealerDrawnTable(Side.Dealer, R, P, P, R, P, S, S);
        table.Distribute(keep: true);
        return table;
    }

    // 딜러 AI 세부 기획서 6장 예시 4: 상대 카드를 모르므로 돋보기부터 쓴다
    [Test]
    public void ChooseItem_Example4_UsesMagnifierFirst()
    {
        var table = MakeExample4Table();
        var items = MakeDealerItems(ItemType.Magnifier, ItemType.Coffee);

        Assert.AreEqual(ItemType.Magnifier, MakeDealer(1).ChooseItem(new DealerKnownInfo(table, 1), items, Side.Dealer));
    }

    // 딜러 AI 세부 기획서 6장 예시 4: 돋보기로 가위 확인. 교환 전 +2(패배 −1, 트리플 +3), 교환 후 +1(승리) → 교환 안 함.
    // 커피는 이길 경우가 0이라 쓰지 않고 아이템 단계를 끝낸다
    [Test]
    public void ChooseExchange_Example4_KeepsTripleAndSkipsCoffee()
    {
        var table = MakeExample4Table();
        var items = MakeDealerItems(ItemType.Magnifier, ItemType.Coffee);
        var dealer = MakeDealer(1);
        items.TryUseItem(Side.Dealer, ItemType.Magnifier, Side.Dealer);
        table.PeekOpponentMatchCard(Side.Dealer);
        var knownInfo = new DealerKnownInfo(table, 1);

        Assert.IsFalse(dealer.ChooseExchange(knownInfo, items));
        Assert.IsNull(dealer.ChooseItem(knownInfo, items, Side.Dealer));
    }

    // 1번째 판, 딜러가 뽑은 보를 플레이어에게 줘서 자기 카드(가위)를 모른다. 카드풀 구성(바위 3 / 가위 2 / 보 2)으로 세면
    // 교환 전(딜러 ? vs 보) 승 2·패 3 → −1, 교환 후(딜러 보 vs ?) 승 3·패 2 → +1 이라 교환한다 (딜러 AI 세부 기획서 3-1장)
    [Test]
    public void ChooseExchange_FirstMatch_OwnCardUnknown_CountsByPoolComposition()
    {
        var table = MakeOrderedTable(R, P, P, S, R, R, S);
        table.SetFirstTurnPlayerByClock(Side.Dealer);
        table.DrawForTurnPlayer();
        table.Distribute(keep: false);
        var knownInfo = new DealerKnownInfo(table, 0);
        var dealer = MakeDealer(1);

        Assert.IsTrue(dealer.ChooseExchange(knownInfo, MakeDealerItems()));
        Assert.AreEqual(ItemType.Magnifier, dealer.ChooseItem(knownInfo, MakeDealerItems(ItemType.Magnifier), Side.Dealer));
    }

    // 2번째 판, 플레이어 턴. 딜러 [보, 가위, ?] / 플레이어 [바위, 보, 바위(돋보기로 확인)], 딜러가 모르는 카드 보 1 · 가위 1.
    // 교환 전 합 0, 교환 후 합 +1(딜러 공격형 풀세트 확정) → 교환한다. 판 승패만 보면 둘 다 0이라 교환하지 않는 상황이다
    [Test]
    public void ChooseExchange_SecondMatch_ComparesExpectedHealthChange()
    {
        var table = MakeOrderedTable(R, P, S, P, R, P, S);
        table.SetFirstTurnPlayerByClock(Side.Dealer);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.RevealMatchCards();
        table.PassTurn(clockUsed: false);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.PeekOpponentMatchCard(Side.Dealer);
        var knownInfo = new DealerKnownInfo(table, 1);

        Assert.AreEqual(1, knownInfo.GetUnknownCardCount(P));
        Assert.AreEqual(1, knownInfo.GetUnknownCardCount(S));
        Assert.IsTrue(MakeDealer(1).ChooseExchange(knownInfo, MakeDealerItems()));
    }

    // 2번째 판, 플레이어 턴, 양쪽 카드를 모두 모름. 모르는 카드가 바위 · 가위 · 보 1장씩이라 비길 수 없다.
    // 딜러 자리 후보를 정한 뒤 남은 2장으로 상대 자리를 채우면 3/6 = 50% → 커피 사용 기준(50% 이상)을 만족한다
    [Test]
    public void ChooseItem_SecondMatch_BothUnknownAllDifferent_UsesCoffeeAtHalf()
    {
        var table = MakeOrderedTable(R, P, R, S, P, S, R);
        table.SetFirstTurnPlayerByClock(Side.Dealer);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        table.RevealMatchCards();
        table.PassTurn(clockUsed: false);
        table.DrawForTurnPlayer();
        table.Distribute(keep: true);
        var knownInfo = new DealerKnownInfo(table, 1);

        Assert.AreEqual(ItemType.Coffee, MakeDealer(1).ChooseItem(knownInfo, MakeDealerItems(ItemType.Coffee), Side.Player));
    }

    // 1번째 판, 양쪽 카드를 모두 모름. 카드풀 구성 바위 3 / 가위 2 / 보 2로 세면 42가지 중 이기는 경우 16가지(약 38%) → 쓰지 않는다
    [Test]
    public void ChooseItem_FirstMatch_BothUnknown_DoesNotUseCoffee()
    {
        var table = MakeOrderedFirstMatchPlayerKeptTable();

        Assert.IsNull(MakeDealer(1).ChooseItem(new DealerKnownInfo(table, 0), MakeDealerItems(ItemType.Coffee), Side.Player));
    }

    // 시계 사용 확률(기본 0.85) 경계: 난수가 확률보다 작으면 쓴다
    [TestCase(0.8, true)]
    [TestCase(0.9, false)]
    public void ChooseItem_DealerTurn_UsesClockByChance(double randomValue, bool expectedUse)
    {
        var table = MakeOrderedFirstMatchDealerDrawnTable(R, P, P, S, R, R, S);
        table.Distribute(keep: true);
        var knownInfo = new DealerKnownInfo(table, 0);

        ItemType? item = MakeDealer(new FixedDoubleRandom(randomValue)).ChooseItem(knownInfo, MakeDealerItems(ItemType.Clock), Side.Dealer);

        Assert.AreEqual(expectedUse, item == ItemType.Clock);
    }

    // 검토 순서(돋보기 → 시계 → 커피)는 한 번만 지난다: 커피를 쓴 뒤에는 앞 순서인 시계를 다시 보지 않는다
    [Test]
    public void ChooseItem_AfterCoffee_DoesNotReviewClockAgain()
    {
        var table = MakeOrderedFirstMatchDealerDrawnTable(R, P, P, S, R, R, S);
        table.Distribute(keep: true);
        var items = MakeDealerItems(ItemType.Clock, ItemType.Coffee);
        items.TryUseItem(Side.Dealer, ItemType.Coffee, Side.Dealer);

        Assert.IsNull(MakeDealer(new FixedDoubleRandom(0.0)).ChooseItem(new DealerKnownInfo(table, 0), items, Side.Dealer));
    }

    // 쓸 수 있는 아이템이 없으면 끝낸다: 보유 칸이 비었거나, 시계뿐인데 턴 플레이어가 아니다
    [Test]
    public void ChooseItem_NoUsableItem_ReturnsNull()
    {
        var table = MakeOrderedFirstMatchPlayerKeptTable();
        var knownInfo = new DealerKnownInfo(table, 0);
        var dealer = MakeDealer(new FixedDoubleRandom(0.0));

        Assert.IsNull(dealer.ChooseItem(knownInfo, MakeDealerItems(), Side.Player));
        Assert.IsNull(dealer.ChooseItem(knownInfo, MakeDealerItems(ItemType.Clock), Side.Player));
    }
}
