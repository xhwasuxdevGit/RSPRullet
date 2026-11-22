using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// 1라운드 튜토리얼 안내 (튜토리얼(1라운드) 세부 기획서 3장 가르칠 순서, 5장 예외, 6-3장 TU-F1~F4, RSP-62).
// 판단 규칙은 값으로, 기록과 순서는 시드를 지정한 진행 흐름(GameFlow)으로 확인한다
public class TutorialGuideRulesTests
{
    private const int SeedCount = 50;

    // 진행 반복문 한 번에 허용하는 최대 횟수. 진행 흐름의 입력 규칙이 바뀌어 진행이 멈추면
    // 에디터가 테스트를 끝내지 못하고 멈추는 대신 이 횟수에서 실패로 끝낸다. 한 라운드는 이보다 훨씬 적게 반복한다
    private const int MaxFlowSteps = 1000;

    private RuleSettings _rules;
    private RoundSettings _round;
    private DealerSettings _dealerSettings;
    private ItemSettings _itemSettings;
    private TutorialSettings _tutorialSettings;

    [SetUp]
    public void SetUp()
    {
        _rules = ScriptableObject.CreateInstance<RuleSettings>();
        _round = ScriptableObject.CreateInstance<RoundSettings>();
        _dealerSettings = ScriptableObject.CreateInstance<DealerSettings>();
        _itemSettings = ScriptableObject.CreateInstance<ItemSettings>();
        _tutorialSettings = CreateTutorialSettings();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_rules);
        Object.DestroyImmediate(_round);
        Object.DestroyImmediate(_dealerSettings);
        Object.DestroyImmediate(_itemSettings);
        Object.DestroyImmediate(_tutorialSettings);
    }

    // 단계마다 항목을 하나씩 넣고, 클릭 대기 대상은 기획서 3장 표대로 둔다
    private static TutorialSettings CreateTutorialSettings()
    {
        var settings = ScriptableObject.CreateInstance<TutorialSettings>();
        var serialized = new SerializedObject(settings);
        SerializedProperty entries = serialized.FindProperty("_entries");
        var steps = (TutorialStep[])System.Enum.GetValues(typeof(TutorialStep));
        entries.arraySize = steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("_step").enumValueIndex = (int)steps[i];
            entry.FindPropertyRelative("_text").stringValue = steps[i].ToString();
            entry.FindPropertyRelative("_waitsForClick").boolValue = IsClickWaitStep(steps[i]);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return settings;
    }

    private static bool IsClickWaitStep(TutorialStep step)
    {
        return step != TutorialStep.PlayerDistribute && step != TutorialStep.Settlement && step != TutorialStep.RoundWin;
    }

    private void SetRoundNumber(int roundNumber)
    {
        var round = new SerializedObject(_round);
        round.FindProperty("_roundNumber").intValue = roundNumber;
        round.ApplyModifiedPropertiesWithoutUndo();
    }

    // 안내가 바뀔 때마다 띄운 단계를 기록한다. 라운드 시작 전에 만들어야 첫 카드풀 안내부터 받는다.
    // autoClick이면 클릭 대기 안내가 뜨는 즉시 클릭해, 대기열의 안내가 모두 차례로 뜨게 한다
    private (GameFlow flow, TutorialGuide guide, List<TutorialStep> shown) StartRound(int seed, bool autoClick = false)
    {
        var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
        var guide = new TutorialGuide(flow, _tutorialSettings);
        List<TutorialStep> shown = RecordShownSteps(guide, autoClick);
        flow.StartRound();
        return (flow, guide, shown);
    }

    // 변경 알림은 클릭 대기 여부만 바뀌어도 오므로, 안내 단계가 새로 바뀐 때만 기록한다
    private static List<TutorialStep> RecordShownSteps(TutorialGuide guide, bool autoClick)
    {
        var shown = new List<TutorialStep>();
        guide.Changed += () =>
        {
            if (guide.CurrentEntry == null) { return; }
            if (shown.Count == 0 || shown[shown.Count - 1] != guide.CurrentEntry.Step) { shown.Add(guide.CurrentEntry.Step); }
            if (autoClick && guide.IsWaitingForClick) { guide.Confirm(); }
        };
        return shown;
    }

    // 플레이어가 클릭 대기 안내를 모두 클릭한다. 실제 게임에서는 클릭을 기다리는 동안 투명 버튼이 화면을 덮어
    // 단계별 버튼을 누를 수 없고 딜러 대기도 넘어가지 않으므로, 진행 입력 전에 부른다
    private static void ClickAll(TutorialGuide guide)
    {
        while (guide.IsWaitingForClick) { guide.Confirm(); }
    }

    // 판 결과 확인 지점까지 진행한다. 1라운드는 아이템이 없어 아이템 단계에서 멈추지 않는다.
    // 분배 뒤 카드 공개 단계에서 멈추므로, 화면의 카드 공개 연출이 끝난 것으로 보고 판정까지 진행한다
    private static void PlayTurn(GameFlow flow)
    {
        if (flow.IsWaitingForDealer)
        {
            flow.PlayDealerTurn();
        }
        else
        {
            flow.DrawCard();
            flow.Distribute(keep: true);
        }

        if (flow.CurrentPhase == GamePhase.Reveal) { flow.Next(); }
    }

    // 라운드가 끝날 때까지 진행한다. 판 결과와 정산은 확인 입력(Next)으로 넘긴다
    private static void PlayUntilRoundEnd(GameFlow flow)
    {
        int steps = 0;
        while (flow.CurrentPhase != GamePhase.RoundEnd)
        {
            Assert.Less(steps++, MaxFlowSteps, $"진행이 멈춤: {flow.CurrentPhase}");
            if (flow.CurrentPhase == GamePhase.ShowResult || flow.CurrentPhase == GamePhase.Settlement ||
                flow.CurrentPhase == GamePhase.SettlementWait)
            {
                flow.Next();
                continue;
            }

            PlayTurn(flow);
        }
    }

    // ── 판단 규칙 ──

    // 화면마다 띄울 안내 단계 (튜토리얼(1라운드) 세부 기획서 3장 표시 화면 상태)
    [TestCase(GamePhase.StartCardDeal, Side.Player, TutorialStep.CardPool)]
    [TestCase(GamePhase.DecideFirst, Side.Player, TutorialStep.DecideFirst)]
    [TestCase(GamePhase.Distribute, Side.Player, TutorialStep.PlayerDistribute)]
    [TestCase(GamePhase.Distribute, Side.Dealer, TutorialStep.DealerDistribute)]
    [TestCase(GamePhase.ShowResult, Side.Player, TutorialStep.MatchResult)]
    [TestCase(GamePhase.Settlement, Side.Player, TutorialStep.Settlement)]
    [TestCase(GamePhase.RoundEnd, Side.Player, TutorialStep.RoundWin)]
    public void GetStep_ReturnsStepForScreen(GamePhase phase, Side turnPlayer, TutorialStep expected)
    {
        Assert.AreEqual(expected, TutorialGuideRules.GetStep(phase, turnPlayer, 1, true));
    }

    // 카드풀 공개는 두 번째 카드 셔플이면 튜토리얼 종료 안내, 그 뒤로는 안내가 없다
    [TestCase(2, TutorialStep.TutorialEnd)]
    [TestCase(3, null)]
    public void GetStep_LaterShuffleCardPool_ReturnsTutorialEndOnlyOnSecond(int shuffleCount, TutorialStep? expected)
    {
        Assert.AreEqual(expected, TutorialGuideRules.GetStep(GamePhase.StartCardDeal, Side.Player, shuffleCount, true));
    }

    // 라운드를 지면 아이템 예고 안내가 없다 (튜토리얼(1라운드) 세부 기획서 5장 예외 TU3)
    [Test]
    public void GetStep_RoundLost_ReturnsNull()
    {
        Assert.IsNull(TutorialGuideRules.GetStep(GamePhase.RoundEnd, Side.Player, 1, false));
    }

    // 뽑기와 아이템 단계, 카드 공개·판정은 안내가 없다
    [TestCase(GamePhase.Draw)]
    [TestCase(GamePhase.ItemTurnPlayer)]
    [TestCase(GamePhase.ItemOpponent)]
    [TestCase(GamePhase.Reveal)]
    [TestCase(GamePhase.Judge)]
    public void GetStep_NoGuideScreen_ReturnsNull(GamePhase phase)
    {
        Assert.IsNull(TutorialGuideRules.GetStep(phase, Side.Player, 1, true));
    }

    // [다음 카드 셔플]로 넘어갈 때와 라운드 종료에 들어갈 때만 지운다 (튜토리얼(1라운드) 세부 기획서 2-2장 표시 방식).
    // 정산 결과 표시에서 다음 카드 셔플 대기로 갈 때는 정산 안내를 남긴다
    [TestCase(GamePhase.SettlementWait, GamePhase.StartCardDeal, true)]
    [TestCase(GamePhase.ShowResult, GamePhase.RoundEnd, true)]
    [TestCase(GamePhase.Settlement, GamePhase.RoundEnd, true)]
    [TestCase(GamePhase.Settlement, GamePhase.SettlementWait, false)]
    [TestCase(GamePhase.ShowResult, GamePhase.Draw, false)]
    [TestCase(GamePhase.DecideFirst, GamePhase.Draw, false)]
    [TestCase(GamePhase.Distribute, GamePhase.ShowResult, false)]
    public void ShouldClearGuide_ClearsOnlyAfterSettlementOrAtRoundEnd(GamePhase lastPhase, GamePhase next, bool expected)
    {
        Assert.AreEqual(expected, TutorialGuideRules.ShouldClearGuide(TutorialStep.PlayerDistribute, lastPhase, next));
    }

    // 튜토리얼 종료 안내는 판 결과를 보여 줄 때 지우고, 그 전에는 남긴다
    [TestCase(GamePhase.ShowResult, true)]
    [TestCase(GamePhase.Distribute, false)]
    [TestCase(GamePhase.Draw, false)]
    public void ShouldClearGuide_TutorialEnd_ClearsAtMatchResult(GamePhase next, bool expected)
    {
        Assert.AreEqual(expected, TutorialGuideRules.ShouldClearGuide(TutorialStep.TutorialEnd, GamePhase.Judge, next));
    }

    // 자동 진행 화면은 원래 표시 시간이 지나고 클릭 대기도 끝나야 넘어간다 (튜토리얼(1라운드) 세부 기획서 2-3장)
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    public void CanAdvance_NeedsTimeOverAndNoClickWait(bool isTimeOver, bool isWaitingForClick, bool expected)
    {
        Assert.AreEqual(expected, TutorialGuideRules.CanAdvance(isTimeOver, isWaitingForClick));
    }

    // ── 진행 흐름과 함께 ──

    // 각 안내는 1라운드에서 한 번만 띄운다 (튜토리얼(1라운드) 세부 기획서 2-4장 반복 규칙)
    [Test]
    public void FirstRound_EachStepShownAtMostOnce()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var (flow, _, shown) = StartRound(seed, autoClick: true);
            PlayUntilRoundEnd(flow);

            CollectionAssert.AllItemsAreUnique(shown, $"seed {seed}");
            Assert.AreEqual(TutorialStep.CardPool, shown[0], $"seed {seed}");
        }
    }

    // 플레이어가 선이면 03 → 04, 딜러가 선이면 04 → 03 순서다 (튜토리얼(1라운드) 세부 기획서 3장, 5장 예외 TU1)
    [Test]
    public void FirstRound_DistributeGuidesFollowFirstTurnPlayer()
    {
        bool checkedPlayerFirst = false;
        bool checkedDealerFirst = false;
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var (flow, _, shown) = StartRound(seed, autoClick: true);
            Side firstTurnPlayer = flow.Table.TurnPlayer;
            PlayUntilRoundEnd(flow);

            int playerIndex = shown.IndexOf(TutorialStep.PlayerDistribute);
            int dealerIndex = shown.IndexOf(TutorialStep.DealerDistribute);
            if (playerIndex < 0 || dealerIndex < 0) { continue; }

            if (firstTurnPlayer == Side.Player)
            {
                Assert.Less(playerIndex, dealerIndex, $"seed {seed}");
                checkedPlayerFirst = true;
            }
            else
            {
                Assert.Less(dealerIndex, playerIndex, $"seed {seed}");
                checkedDealerFirst = true;
            }
        }

        Assert.IsTrue(checkedPlayerFirst && checkedDealerFirst, "플레이어 선과 딜러 선이 모두 나오는 시드가 필요하다");
    }

    // 선 정하기가 비겨도 처음 선 정하기에서 선 정하기 안내를 한 번 띄우고, 다시 섞을 때 카드풀 안내는 반복하지 않는다 (튜토리얼(1라운드) 세부 기획서 5장 예외 TU2)
    [Test]
    public void FirstRound_DecideFirstDraw_ShowsDecideFirstOnce()
    {
        bool checkedDraw = false;
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
            var guide = new TutorialGuide(flow, _tutorialSettings);
            List<TutorialStep> shown = RecordShownSteps(guide, autoClick: true);
            bool isDraw = false;
            flow.Logged += message => isDraw |= message.Contains("선 정하기 무승부");
            flow.StartRound();
            if (!isDraw) { continue; }

            CollectionAssert.AreEqual(new[] { TutorialStep.CardPool, TutorialStep.DecideFirst }, shown.GetRange(0, 2), $"seed {seed}");
            Assert.AreEqual(1, shown.FindAll(step => step == TutorialStep.DecideFirst).Count, $"seed {seed}");
            checkedDraw = true;
        }

        Assert.IsTrue(checkedDraw, "선 정하기가 비기는 시드가 필요하다");
    }

    // 1라운드가 아니면 안내를 띄우지 않고 클릭도 기다리지 않는다 (튜토리얼(1라운드) 세부 기획서 2-1장 범위)
    [TestCase(2)]
    [TestCase(3)]
    public void LaterRounds_NoGuide(int roundNumber)
    {
        SetRoundNumber(roundNumber);
        var (flow, guide, shown) = StartRound(0);
        PlayTurn(flow);

        Assert.IsEmpty(shown);
        Assert.IsNull(guide.CurrentEntry);
        Assert.IsFalse(guide.IsWaitingForClick);
    }

    // 판 결과 안내는 처음 판 결과에서만 클릭을 기다리고, 두 번째 판 결과는 기다리지 않는다 (튜토리얼(1라운드) 세부 기획서 2-4장 반복 규칙).
    // 플레이어가 클릭 대기 안내마다 클릭한다고 보고 진행한다
    [Test]
    public void FirstRound_MatchResult_WaitsForClickOnlyFirstTime()
    {
        var (flow, guide, _) = StartRound(0);
        ClickAll(guide);
        PlayTurn(flow);
        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
        Assert.AreEqual(TutorialStep.MatchResult, guide.CurrentEntry.Step);
        Assert.IsTrue(guide.IsWaitingForClick);
        ClickAll(guide);

        flow.Next();
        if (flow.CurrentPhase == GamePhase.RoundEnd) { return; }

        ClickAll(guide);
        PlayTurn(flow);
        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
        Assert.IsFalse(guide.IsWaitingForClick);
    }

    // 클릭하면 말풍선과 클릭 대기가 함께 사라진다. 기다리는 중이 아니면 클릭해도 바뀌는 것이 없다
    [Test]
    public void Confirm_ClearsGuideAndWaitTogether()
    {
        var (flow, guide, _) = StartRound(0);
        ClickAll(guide);
        PlayTurn(flow);
        Assert.IsTrue(guide.IsWaitingForClick);
        int changedCount = 0;
        guide.Changed += () => changedCount++;

        guide.Confirm();
        Assert.IsFalse(guide.IsWaitingForClick);
        Assert.IsNull(guide.CurrentEntry);
        Assert.AreEqual(1, changedCount);

        guide.Confirm();
        Assert.AreEqual(1, changedCount);
    }

    // 클릭 대기 안내는 클릭하기 전에 단계가 바뀌어도 그 안내가 남아 있는 동안 계속 기다린다.
    // 판 결과 안내를 클릭하지 않고 두 번째 판으로 넘어가도, 다음 안내가 나오기 전까지는 안내와 대기가 함께 남는다
    [Test]
    public void ClickWaitGuide_KeepsWaitingUntilClickedOrReplaced()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var (flow, guide, _) = StartRound(seed);
            ClickAll(guide);
            PlayTurn(flow);
            flow.Next();
            if (flow.CurrentPhase != GamePhase.Draw || flow.IsWaitingForDealer) { continue; }

            Assert.AreEqual(TutorialStep.MatchResult, guide.CurrentEntry.Step, $"seed {seed}");
            Assert.IsTrue(guide.IsWaitingForClick, $"seed {seed}");
            return;
        }

        Assert.Fail("두 번째 판이 플레이어 차례인 시드가 필요하다");
    }

    // 버튼으로 넘어가는 플레이어 분배 화면은 안내를 띄워도 클릭을 기다리지 않는다 (튜토리얼(1라운드) 세부 기획서 2-3장)
    [Test]
    public void FirstRound_PlayerDistribute_DoesNotWaitForClick()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var (flow, guide, _) = StartRound(seed);
            if (flow.IsWaitingForDealer) { continue; }

            ClickAll(guide);
            flow.DrawCard();
            Assert.AreEqual(TutorialStep.PlayerDistribute, guide.CurrentEntry.Step, $"seed {seed}");
            Assert.IsFalse(guide.IsWaitingForClick, $"seed {seed}");
            return;
        }

        Assert.Fail("플레이어가 선인 시드가 필요하다");
    }

    // 정산 안내는 [다음 카드 셔플]로 넘어가면 지우고, 두 번째 카드 셔플에서 튜토리얼 종료 안내를 띄운다.
    // 종료 안내는 그 카드 셔플의 첫 판 결과를 보여 줄 때 지우고, 그 뒤로는 안내가 없다
    [Test]
    public void FirstRound_AfterSettlement_ShowsTutorialEndThenClears()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var (flow, guide, _) = StartRound(seed);
            int steps = 0;
            while (flow.CurrentPhase != GamePhase.Settlement && flow.CurrentPhase != GamePhase.RoundEnd)
            {
                Assert.Less(steps++, MaxFlowSteps, $"seed {seed} 진행이 멈춤: {flow.CurrentPhase}");
                ClickAll(guide);
                if (flow.CurrentPhase == GamePhase.ShowResult) { flow.Next(); }
                else { PlayTurn(flow); }
            }

            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            Assert.AreEqual(TutorialStep.Settlement, guide.CurrentEntry.Step, $"seed {seed}");
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            // 다음 카드 셔플 대기 동안에는 정산 안내가 남아 있다
            Assert.AreEqual(GamePhase.SettlementWait, flow.CurrentPhase, $"seed {seed}");
            Assert.AreEqual(TutorialStep.Settlement, guide.CurrentEntry.Step, $"seed {seed}");
            flow.Next();

            Assert.AreEqual(TutorialStep.TutorialEnd, guide.CurrentEntry.Step, $"seed {seed}");

            // 첫 판 동안은 종료 안내가 남아 있다
            steps = 0;
            while (flow.CurrentPhase != GamePhase.ShowResult)
            {
                Assert.Less(steps++, MaxFlowSteps, $"seed {seed} 진행이 멈춤: {flow.CurrentPhase}");
                Assert.AreEqual(TutorialStep.TutorialEnd, guide.CurrentEntry.Step, $"seed {seed}");
                PlayTurn(flow);
            }

            Assert.IsNull(guide.CurrentEntry, $"seed {seed}");
            Assert.IsFalse(guide.IsWaitingForClick, $"seed {seed}");
            return;
        }

        Assert.Fail("정산 뒤 다음 카드 셔플로 넘어가는 시드가 필요하다");
    }

    // 1라운드를 이기면 아이템 예고 안내를, 지면 안내 없이 끝낸다 (튜토리얼(1라운드) 세부 기획서 5장 예외 TU3)
    [Test]
    public void FirstRound_RoundEnd_ShowsRoundWinOnlyWhenPlayerWins()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var (flow, guide, _) = StartRound(seed, autoClick: true);
            PlayUntilRoundEnd(flow);

            bool playerWon = flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
            if (playerWon) { Assert.AreEqual(TutorialStep.RoundWin, guide.CurrentEntry.Step, $"seed {seed}"); }
            else { Assert.IsNull(guide.CurrentEntry, $"seed {seed}"); }
        }
    }

    // 응급약 복용으로 같은 라운드를 다시 하면 새 진행 흐름과 함께 기록이 비어 안내가 처음부터 나온다 (튜토리얼(1라운드) 세부 기획서 6-3장 TU-F1)
    [Test]
    public void RestartedRound_ShowsGuidesFromStart()
    {
        var (firstFlow, _, _) = StartRound(0);
        PlayUntilRoundEnd(firstFlow);

        var (_, guide, shown) = StartRound(0);

        Assert.AreEqual(TutorialStep.CardPool, shown[0]);
        Assert.IsNotNull(guide.CurrentEntry);
    }

    // 카드 셔플이 한 번에 진행되어 안내가 한꺼번에 나와도, 클릭할 때마다 하나씩 차례로 띄운다.
    // 딜러가 선이면 카드풀 → 선 정하기 → 딜러 분배 순서이고, 마지막 클릭 뒤에는 말풍선이 없다
    [Test]
    public void FirstRound_DealerFirst_ShowsQueuedGuidesOneByOne()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var (flow, guide, _) = StartRound(seed);
            if (!flow.IsWaitingForDealer) { continue; }

            var steps = new List<TutorialStep>();
            while (guide.IsWaitingForClick)
            {
                steps.Add(guide.CurrentEntry.Step);
                guide.Confirm();
            }

            CollectionAssert.AreEqual(
                new[] { TutorialStep.CardPool, TutorialStep.DecideFirst, TutorialStep.DealerDistribute }, steps, $"seed {seed}");
            Assert.IsNull(guide.CurrentEntry, $"seed {seed}");
            return;
        }

        Assert.Fail("딜러가 선인 시드가 필요하다");
    }

    // 라운드가 끝나 안내를 지울 때는 아직 띄우지 않은 대기열의 안내도 함께 지운다
    [Test]
    public void RoundEnd_ClearsQueuedGuides()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var (flow, guide, _) = StartRound(seed);
            PlayUntilRoundEnd(flow);

            bool playerWon = flow.Health.GetHealth(Side.Player) > GameRules.MinHealth;
            if (playerWon) { Assert.AreEqual(TutorialStep.RoundWin, guide.CurrentEntry.Step, $"seed {seed}"); }
            else { Assert.IsNull(guide.CurrentEntry, $"seed {seed}"); }
            Assert.IsFalse(guide.IsWaitingForClick, $"seed {seed}");
        }
    }
}
