using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// 진행 흐름 (RSP-81). 코어 규칙 상세 기획서 2장 진행 단계, 6장 예외 E1·E5 기준.
// 카드 순서를 고정하는 난수는 선 정하기가 매번 비겨 끝나지 않으므로, 시드를 지정한 System.Random으로 진행을 재현한다
public class GameFlowTests
{
    private const int SeedCount = 50;

    private RuleSettings _rules;
    private RoundSettings _round;
    private DealerSettings _dealerSettings;

    [SetUp]
    public void SetUp()
    {
        _rules = ScriptableObject.CreateInstance<RuleSettings>();
        _round = ScriptableObject.CreateInstance<RoundSettings>();
        _dealerSettings = ScriptableObject.CreateInstance<DealerSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_rules);
        Object.DestroyImmediate(_round);
        Object.DestroyImmediate(_dealerSettings);
    }

    private GameFlow StartFlow(int seed)
    {
        var flow = new GameFlow(_rules, _round, _dealerSettings, new System.Random(seed));
        flow.StartRound();
        return flow;
    }

    // 플레이어 차례면 뽑고 가진다로 분배해 판 결과 확인 지점까지 진행한다. 딜러 차례는 이미 진행되어 있다
    private static void PlayPlayerTurnIfNeeded(GameFlow flow)
    {
        if (flow.CurrentPhase != GamePhase.Draw)
        {
            return;
        }

        flow.DrawCard();
        flow.Distribute(keep: true);
    }

    // 두 판을 진행해 정산 확인 지점(또는 판 도중 라운드 종료)까지 간다
    private static void PlayOneShuffle(GameFlow flow)
    {
        PlayPlayerTurnIfNeeded(flow);
        flow.Next();
        if (flow.CurrentPhase == GamePhase.RoundEnd)
        {
            return;
        }

        PlayPlayerTurnIfNeeded(flow);
        flow.Next();
    }

    [Test]
    public void StartRound_StopsAtPlayerDraw_OrDealerMatchResult()
    {
        bool sawPlayerTurn = false;
        bool sawDealerTurn = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.TurnPlayer == Side.Player)
            {
                sawPlayerTurn = true;
                Assert.AreEqual(GamePhase.Draw, flow.CurrentPhase);
                Assert.AreEqual(1, flow.GetCombinationSlots(Side.Player).Count);
                Assert.AreEqual(1, flow.GetCombinationSlots(Side.Dealer).Count);
                continue;
            }

            // 딜러 차례면 딜러가 뽑고 분배까지 마쳐 판정 결과에서 멈춘다
            sawDealerTurn = true;
            Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
            Assert.AreEqual(2, flow.GetCombinationSlots(Side.Player).Count);
            Assert.AreEqual(2, flow.GetCombinationSlots(Side.Dealer).Count);
        }

        Assert.IsTrue(sawPlayerTurn && sawDealerTurn, "시드 범위 안에서 두 경우가 모두 나와야 한다");
    }

    [Test]
    public void PlayerDistribute_JudgesMatch_AndStopsAtResult()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.TurnPlayer != Side.Player)
            {
                continue;
            }

            Assert.IsTrue(flow.DrawCard());
            Assert.AreEqual(GamePhase.Distribute, flow.CurrentPhase);
            Card drawn = flow.DrawnCard;

            Assert.IsTrue(flow.Distribute(keep: true));
            Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
            Assert.AreSame(drawn, flow.GetCombinationSlots(Side.Player)[1]);
            Assert.IsNull(flow.DrawnCard);
            return;
        }

        Assert.Fail("플레이어가 선인 시드를 찾지 못했다");
    }

    [Test]
    public void OneShuffle_ReachesSettlement_WithThreeCardsEach_AndEmptyDeck()
    {
        // 시작 체력 3에서 두 판의 데미지는 최대 2이므로 판 도중 라운드가 끝나지 않는다
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            PlayOneShuffle(flow);

            Assert.AreEqual(GamePhase.Settlement, flow.CurrentPhase, $"seed {seed}");
            Assert.AreEqual(3, flow.GetCombinationSlots(Side.Player).Count);
            Assert.AreEqual(3, flow.GetCombinationSlots(Side.Dealer).Count);
            Assert.AreEqual(0, flow.RemainingDeckCount);
        }
    }

    [Test]
    public void SecondMatch_TurnPlayerSwitches()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            Side firstTurnPlayer = flow.TurnPlayer;

            PlayPlayerTurnIfNeeded(flow);
            flow.Next();

            Assert.AreEqual(1, flow.MatchIndex);
            Assert.AreNotEqual(firstTurnPlayer, flow.TurnPlayer, $"seed {seed}");
        }
    }

    [Test]
    public void DecideFirstDraw_Reshuffles_AndStartsFirstMatch()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, new System.Random(seed));
            var logs = new List<string>();
            flow.Logged += logs.Add;
            flow.StartRound();

            if (!logs.Exists(log => log.Contains("무승부")))
            {
                continue;
            }

            // 다시 섞은 카드풀에서 시작 카드부터 새로 받아 첫 번째 판을 시작한다
            Assert.AreEqual(0, flow.MatchIndex);
            Assert.That(flow.CurrentPhase, Is.EqualTo(GamePhase.Draw).Or.EqualTo(GamePhase.ShowResult));
            Assert.AreNotEqual(
                flow.GetCombinationSlots(Side.Player)[0].Shape,
                flow.GetCombinationSlots(Side.Dealer)[0].Shape);
            return;
        }

        Assert.Fail("선 정하기 무승부가 나는 시드를 찾지 못했다");
    }

    [Test]
    public void ZeroHealthInMatch_EndsRoundWithoutSettlement()
    {
        var round = new SerializedObject(_round);
        round.FindProperty("_startHealth").intValue = 1;
        round.ApplyModifiedPropertiesWithoutUndo();

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            PlayPlayerTurnIfNeeded(flow);
            bool someoneAtZero = flow.GetHealth(Side.Player) == 0 || flow.GetHealth(Side.Dealer) == 0;
            if (!someoneAtZero)
            {
                continue;
            }

            Assert.IsTrue(flow.Next());

            // 두 번째 판과 정산 없이 끝나므로 더미에 3장이 남는다
            Assert.AreEqual(GamePhase.RoundEnd, flow.CurrentPhase);
            Assert.AreEqual(3, flow.RemainingDeckCount);
            return;
        }

        Assert.Fail("첫 번째 판에서 승패가 나는 시드를 찾지 못했다");
    }

    [Test]
    public void NextAfterSettlement_StartsNewShuffleFromFirstMatch()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            PlayOneShuffle(flow);
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd)
            {
                continue;
            }

            Assert.AreEqual(0, flow.MatchIndex);
            Assert.That(flow.CurrentPhase, Is.EqualTo(GamePhase.Draw).Or.EqualTo(GamePhase.ShowResult));
            Assert.LessOrEqual(flow.GetCombinationSlots(Side.Player).Count, 2);
            return;
        }

        Assert.Fail("정산 뒤 라운드가 이어지는 시드를 찾지 못했다");
    }

    [Test]
    public void InputInWrongPhase_IsIgnored()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.TurnPlayer != Side.Player)
            {
                // 판 결과 확인 중에는 뽑기·분배를 받지 않는다
                Assert.IsFalse(flow.DrawCard());
                Assert.IsFalse(flow.Distribute(keep: true));
                Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
                continue;
            }

            // 뽑기 전에는 분배·다음 입력을 받지 않고, 라운드 시작도 다시 받지 않는다
            Assert.IsFalse(flow.Distribute(keep: true));
            Assert.IsFalse(flow.Next());
            Assert.IsFalse(flow.StartRound());
            Assert.AreEqual(GamePhase.Draw, flow.CurrentPhase);
        }
    }
}
