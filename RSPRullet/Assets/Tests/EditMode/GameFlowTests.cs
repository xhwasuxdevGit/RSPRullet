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
    private ItemSettings _itemSettings;

    [SetUp]
    public void SetUp()
    {
        _rules = ScriptableObject.CreateInstance<RuleSettings>();
        _round = ScriptableObject.CreateInstance<RoundSettings>();
        _dealerSettings = ScriptableObject.CreateInstance<DealerSettings>();
        _itemSettings = ScriptableObject.CreateInstance<ItemSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_rules);
        Object.DestroyImmediate(_round);
        Object.DestroyImmediate(_dealerSettings);
        Object.DestroyImmediate(_itemSettings);
    }

    private GameFlow StartFlow(int seed)
    {
        var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
        flow.StartRound();
        return flow;
    }

    // 판 결과 확인 지점까지 진행한다. 딜러 차례면 고민 중 대기가 끝난 것으로 보고 딜러가 행동하고, 플레이어 차례면 뽑고 가진다로 분배한다
    private static void PlayTurn(GameFlow flow)
    {
        if (flow.IsWaitingForDealer)
        {
            flow.PlayDealerTurn();
            FinishItemPhases(flow);
            return;
        }

        flow.DrawCard();
        flow.Distribute(keep: true);
        FinishItemPhases(flow);
    }

    // 아이템이 있는 라운드면 플레이어 아이템 단계와 딜러 아이템 대기에서 멈추므로, 판 결과까지 간다.
    // 플레이어는 아이템을 더 쓰지 않고 끝내고, 딜러 아이템 대기는 대기 시간이 지난 것으로 보고 끝낸다
    private static void FinishItemPhases(GameFlow flow)
    {
        while (flow.CurrentPhase == GamePhase.ItemTurnPlayer || flow.CurrentPhase == GamePhase.ItemOpponent)
        {
            bool moved = flow.IsWaitingForDealerItems ? flow.FinishDealerItemPhase() : flow.FinishItemPhase();
            if (!moved) { return; }
        }
    }

    // 두 판을 진행해 정산 확인 지점(또는 판 도중 라운드 종료)까지 간다
    private static void PlayOneShuffle(GameFlow flow)
    {
        PlayTurn(flow);
        flow.Next();
        if (flow.CurrentPhase == GamePhase.RoundEnd)
        {
            return;
        }

        PlayTurn(flow);
        flow.Next();
    }

    [Test]
    public void StartRound_StopsAtPlayerDraw_OrDealerThinking()
    {
        bool sawPlayerTurn = false;
        bool sawDealerTurn = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            // 플레이어 차례면 뽑기 전에, 딜러 차례면 딜러가 뽑은 뒤 분배 전에 멈춘다. 딜러 차례일 때만 딜러를 기다리는 중이다
            GamePhase expectedPhase = flow.Table.TurnPlayer == Side.Player ? GamePhase.Draw : GamePhase.Distribute;
            Assert.AreEqual(expectedPhase, flow.CurrentPhase);
            Assert.AreEqual(1, flow.Table.GetCombinationSlots(Side.Player).Count);
            Assert.AreEqual(1, flow.Table.GetCombinationSlots(Side.Dealer).Count);
            Assert.AreEqual(flow.Table.TurnPlayer == Side.Dealer, flow.IsWaitingForDealer);

            sawPlayerTurn |= flow.Table.TurnPlayer == Side.Player;
            sawDealerTurn |= flow.Table.TurnPlayer == Side.Dealer;
        }

        Assert.IsTrue(sawPlayerTurn && sawDealerTurn, "시드 범위 안에서 두 경우가 모두 나와야 한다");
    }

    [Test]
    public void PlayerDistribute_JudgesMatch_AndStopsAtResult()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Player)
            {
                continue;
            }

            Assert.IsTrue(flow.DrawCard());
            Assert.AreEqual(GamePhase.Distribute, flow.CurrentPhase);
            Card drawn = flow.Table.DrawnCard;

            Assert.IsTrue(flow.Distribute(keep: true));
            Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
            Assert.AreSame(drawn, flow.Table.GetCombinationSlots(Side.Player)[1]);
            Assert.IsNull(flow.Table.DrawnCard);
            return;
        }

        Assert.Fail("플레이어가 선인 시드를 찾지 못했다");
    }

    // 판-03: 딜러가 판 시작 때 먼저 뽑고, 뽑은 카드는 딜러만 안다 (UI·UX 화면 흐름 세부 기획서 3-1장, 4-2장)
    [Test]
    public void DealerTurn_DrawsAtMatchStart_KnownOnlyByDealer()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Dealer)
            {
                continue;
            }

            Assert.IsTrue(flow.IsWaitingForDealer);
            Assert.IsNotNull(flow.Table.DrawnCard);
            Assert.IsTrue(flow.Table.DrawnCard.KnownByDealer);
            Assert.IsFalse(flow.Table.DrawnCard.KnownByPlayer);
            return;
        }

        Assert.Fail("딜러가 선인 시드를 찾지 못했다");
    }

    [Test]
    public void PlayDealerTurn_DistributesDrawnCard_AndStopsAtResult()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Dealer)
            {
                continue;
            }

            Card drawn = flow.Table.DrawnCard;
            Assert.IsTrue(flow.PlayDealerTurn());
            Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
            Assert.IsFalse(flow.IsWaitingForDealer);
            Assert.IsNull(flow.Table.DrawnCard);
            // 가졌으면 딜러 자리, 줬으면 플레이어 자리에 뽑은 카드가 들어간다
            Assert.IsTrue(flow.Table.GetCombinationSlots(Side.Dealer)[1] == drawn || flow.Table.GetCombinationSlots(Side.Player)[1] == drawn);
            Assert.AreEqual(2, flow.Table.GetCombinationSlots(Side.Player).Count);
            Assert.AreEqual(2, flow.Table.GetCombinationSlots(Side.Dealer).Count);
            return;
        }

        Assert.Fail("딜러가 선인 시드를 찾지 못했다");
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
            Assert.AreEqual(3, flow.Table.GetCombinationSlots(Side.Player).Count);
            Assert.AreEqual(3, flow.Table.GetCombinationSlots(Side.Dealer).Count);
            Assert.AreEqual(0, flow.Table.RemainingDeckCount);
        }
    }

    [Test]
    public void SecondMatch_TurnPlayerSwitches()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            Side firstTurnPlayer = flow.Table.TurnPlayer;

            PlayTurn(flow);
            flow.Next();

            Assert.AreEqual(1, flow.MatchIndex);
            Assert.AreNotEqual(firstTurnPlayer, flow.Table.TurnPlayer, $"seed {seed}");
        }
    }

    [Test]
    public void DecideFirstDraw_Reshuffles_AndStartsFirstMatch()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
            var logs = new List<string>();
            flow.Logged += logs.Add;
            flow.StartRound();

            if (!logs.Exists(log => log.Contains("무승부")))
            {
                continue;
            }

            // 다시 섞은 카드풀에서 시작 카드부터 새로 받아 첫 번째 판을 시작한다
            Assert.AreEqual(0, flow.MatchIndex);
            Assert.AreEqual(GamePhase.Draw, flow.CurrentPhase);
            Assert.AreNotEqual(
                flow.Table.GetCombinationSlots(Side.Player)[0].Shape,
                flow.Table.GetCombinationSlots(Side.Dealer)[0].Shape);
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
            PlayTurn(flow);
            bool someoneAtZero = flow.Health.GetHealth(Side.Player) == 0 || flow.Health.GetHealth(Side.Dealer) == 0;
            if (!someoneAtZero)
            {
                continue;
            }

            Assert.IsTrue(flow.Next());

            // 두 번째 판과 정산 없이 끝나므로 더미에 3장이 남는다
            Assert.AreEqual(GamePhase.RoundEnd, flow.CurrentPhase);
            Assert.AreEqual(3, flow.Table.RemainingDeckCount);
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

            // 플레이어가 선이면 뽑기 전, 딜러가 선이면 딜러가 뽑은 뒤 분배 전에 멈춘다
            GamePhase expectedPhase = flow.Table.TurnPlayer == Side.Player ? GamePhase.Draw : GamePhase.Distribute;
            Assert.AreEqual(0, flow.MatchIndex);
            Assert.AreEqual(expectedPhase, flow.CurrentPhase);
            Assert.AreEqual(1, flow.Table.GetCombinationSlots(Side.Player).Count);
            return;
        }

        Assert.Fail("정산 뒤 라운드가 이어지는 시드를 찾지 못했다");
    }

    // 상단 정보 UI (RSP-43). 라운드 번호는 라운드 설정 값을 그대로 쓴다
    [Test]
    public void RoundNumber_ComesFromRoundSettings()
    {
        var round = new SerializedObject(_round);
        round.FindProperty("_roundNumber").intValue = 2;
        round.ApplyModifiedPropertiesWithoutUndo();

        GameFlow flow = StartFlow(0);

        Assert.AreEqual(2, flow.RoundNumber);
    }

    // 상단 정보 UI (RSP-43). 카드 셔플 번호는 카드 셔플마다 늘고, 선 정하기 무승부로 다시 섞을 때는 늘지 않는다
    [Test]
    public void ShuffleCount_IncreasesPerShuffle_ExcludingDecideFirstDraw()
    {
        bool sawDraw = false;
        bool sawNextShuffle = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
            int drawCount = 0;
            flow.Logged += log =>
            {
                // 판 판정의 무승부 로그와 구분하려고 선 정하기 무승부 문구로 센다
                if (log.Contains("선 정하기 무승부"))
                {
                    drawCount++;
                }
            };
            flow.StartRound();

            Assert.AreEqual(1, flow.ShuffleCount);
            sawDraw |= drawCount > 0;

            PlayOneShuffle(flow);
            if (flow.CurrentPhase != GamePhase.Settlement)
            {
                continue;
            }

            // 정산 중에는 아직 같은 카드 셔플이다
            Assert.AreEqual(1, flow.ShuffleCount);
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd)
            {
                continue;
            }

            Assert.AreEqual(2, flow.ShuffleCount);
            sawNextShuffle = true;
        }

        Assert.IsTrue(sawDraw, "선 정하기 무승부가 나는 시드를 찾지 못했다");
        Assert.IsTrue(sawNextShuffle, "정산 뒤 라운드가 이어지는 시드를 찾지 못했다");
    }

    // 카드풀 정보 (RSP-38). UI·UX 화면 흐름 세부 기획서 5-4장 카드풀:
    // 첫 번째 판 "5장 → 3장", 두 번째 판 "3장 → 1장", 정산 중에는 두 번째 판 값 유지
    [Test]
    public void MatchDeckCount_FollowsMatch_AndStaysDuringSettlement()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            Assert.AreEqual(5, flow.Table.MatchStartDeckCount);
            Assert.AreEqual(3, flow.Table.MatchEndDeckCount);

            PlayTurn(flow);
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd)
            {
                continue;
            }

            Assert.AreEqual(3, flow.Table.MatchStartDeckCount);
            Assert.AreEqual(1, flow.Table.MatchEndDeckCount);

            PlayTurn(flow);
            flow.Next();
            if (flow.CurrentPhase != GamePhase.Settlement)
            {
                continue;
            }

            Assert.AreEqual(3, flow.Table.MatchStartDeckCount);
            Assert.AreEqual(1, flow.Table.MatchEndDeckCount);
            return;
        }

        Assert.Fail("정산까지 가는 시드를 찾지 못했다");
    }

    // 카드풀 구성 개수는 3·2·2이고, 카드를 뽑아도 줄지 않는다
    [Test]
    public void PoolCardCount_IsComposition_AndUnchangedByDraw()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            int[] before = GetPoolCounts(flow);
            CollectionAssert.AreEquivalent(
                new[] { GameRules.TripleShapeCardCount, GameRules.OtherShapeCardCount, GameRules.OtherShapeCardCount }, before);

            PlayTurn(flow);
            CollectionAssert.AreEqual(before, GetPoolCounts(flow));
        }
    }

    private static int[] GetPoolCounts(GameFlow flow)
    {
        return new[]
        {
            flow.Table.GetPoolCardCount(HandShape.Scissors),
            flow.Table.GetPoolCardCount(HandShape.Rock),
            flow.Table.GetPoolCardCount(HandShape.Paper)
        };
    }

    [Test]
    public void InputInWrongPhase_IsIgnored()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Player)
            {
                // 딜러를 기다리는 중에는 플레이어의 뽑기·분배·다음 입력을 받지 않는다
                Assert.IsFalse(flow.DrawCard());
                Assert.IsFalse(flow.Distribute(keep: true));
                Assert.IsFalse(flow.Next());
                Assert.IsTrue(flow.IsWaitingForDealer);

                // 판 결과 확인 중에는 딜러 행동을 다시 받지 않는다
                flow.PlayDealerTurn();
                Assert.IsFalse(flow.PlayDealerTurn());
                Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
                continue;
            }

            // 플레이어 차례에는 딜러 행동을 받지 않는다
            Assert.IsFalse(flow.PlayDealerTurn());

            // 뽑기 전에는 분배·다음 입력을 받지 않고, 라운드 시작도 다시 받지 않는다
            Assert.IsFalse(flow.Distribute(keep: true));
            Assert.IsFalse(flow.Next());
            Assert.IsFalse(flow.StartRound());
            Assert.AreEqual(GamePhase.Draw, flow.CurrentPhase);
        }
    }

    // 판정 기록 (UI·UX 화면 흐름 세부 기획서 5-5장): 한 카드 셔플에 선 정하기 1줄, 판정 2줄, 정산 2줄이 순서대로 쌓이고,
    // 다음 카드 셔플이 시작되면 비운다. 선 정하기가 비겨 다시 섞을 때도 비우므로 라운드 시작 직후에는 결정된 선 정하기 1줄만 남는다
    [Test]
    public void JudgmentRecord_KeepsResultLinesPerShuffle_AndClearsOnNextShuffle()
    {
        bool sawNextShuffle = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
            var lines = new List<string>();
            flow.JudgmentRecorded += lines.Add;
            flow.JudgmentRecordCleared += lines.Clear;

            flow.StartRound();
            Assert.AreEqual(1, lines.Count);
            StringAssert.StartsWith("선 정하기:", lines[0]);
            StringAssert.DoesNotContain("비김", lines[0]);

            PlayOneShuffle(flow);
            if (flow.CurrentPhase != GamePhase.Settlement)
            {
                continue;
            }

            Assert.AreEqual(5, lines.Count);
            StringAssert.StartsWith("첫 번째 판:", lines[1]);
            StringAssert.StartsWith("두 번째 판:", lines[2]);
            StringAssert.StartsWith("정산:", lines[3]);
            StringAssert.StartsWith("조합:", lines[4]);

            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd)
            {
                Assert.AreEqual(5, lines.Count);
                continue;
            }

            sawNextShuffle = true;
            Assert.AreEqual(1, lines.Count);
            StringAssert.StartsWith("선 정하기:", lines[0]);
        }

        Assert.IsTrue(sawNextShuffle);
    }

    // 상태 변경 이벤트 (RSP-85). 단계 알림은 진행 단계가 바뀔 때마다 1회 오고, 카드 셔플 진입 알림은 보내지 않는다.
    // 라운드 첫 카드 셔플 진입은 테이블 상태가 없을 때라, 알림을 받은 화면이 턴 플레이어를 읽으면 오류가 난다
    [Test]
    public void PhaseChanged_ComesPerPhaseChange_ExceptEnteringCardShuffle()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
            var phases = new List<GamePhase>();
            flow.PhaseChanged += (previous, next) =>
            {
                phases.Add(next);
                // 알림을 받는 시점에 화면이 읽는 상태를 오류 없이 읽을 수 있어야 한다
                Assert.DoesNotThrow(() => _ = flow.Table.TurnPlayer);
            };

            flow.StartRound();
            CollectionAssert.DoesNotContain(phases, GamePhase.CardShuffle);
            Assert.AreEqual(flow.CurrentPhase, phases[phases.Count - 1]);

            PlayTurn(flow);
            phases.Clear();
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd)
            {
                continue;
            }

            // 첫 번째 판 결과 확인 뒤 두 번째 판 뽑기로 넘어간다. 딜러 차례면 딜러가 바로 뽑아 분배 단계까지 간다
            GamePhase[] expected = flow.Table.TurnPlayer == Side.Player
                ? new[] { GamePhase.Draw }
                : new[] { GamePhase.Draw, GamePhase.Distribute };
            CollectionAssert.AreEqual(expected, phases, $"seed {seed}");
        }
    }

    // 카드가 바뀌는 입력마다 카드 변경 알림이 온다: 플레이어 뽑기 1회, 분배 1회 + 판 카드 공개 1회
    [Test]
    public void CardsChanged_ComesOncePerCardChange()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Player)
            {
                continue;
            }

            int count = 0;
            flow.CardsChanged += () => count++;

            flow.DrawCard();
            Assert.AreEqual(1, count);

            count = 0;
            flow.Distribute(keep: true);
            Assert.AreEqual(2, count);
            return;
        }

        Assert.Fail("플레이어가 선인 시드를 찾지 못했다");
    }

    // 카드풀 정보 알림은 판 시작마다 1회 온다. 정산으로 넘어갈 때는 판이 시작되지 않아 오지 않는다
    [Test]
    public void DeckChanged_ComesOncePerMatchStart()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
            int count = 0;
            flow.DeckChanged += () => count++;

            flow.StartRound();
            Assert.AreEqual(1, count);

            PlayTurn(flow);
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd)
            {
                continue;
            }

            Assert.AreEqual(2, count);

            PlayTurn(flow);
            flow.Next();
            Assert.AreEqual(2, count, $"seed {seed}");
        }
    }

    // 체력 알림은 판정·정산에서 체력이 실제로 바뀌었을 때만 1회 온다
    [Test]
    public void HealthChanged_ComesOnceOnlyWhenHealthChanges()
    {
        bool sawChange = false;
        bool sawNoChange = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            int count = 0;
            flow.HealthChanged += () => count++;

            int playerBefore = flow.Health.GetHealth(Side.Player);
            int dealerBefore = flow.Health.GetHealth(Side.Dealer);
            PlayTurn(flow);

            bool changed = flow.Health.GetHealth(Side.Player) != playerBefore || flow.Health.GetHealth(Side.Dealer) != dealerBefore;
            Assert.AreEqual(changed ? 1 : 0, count, $"seed {seed}");
            sawChange |= changed;
            sawNoChange |= !changed;
        }

        Assert.IsTrue(sawChange && sawNoChange, "시드 범위 안에서 체력이 바뀌는 판과 비기는 판이 모두 나와야 한다");
    }


    // ── 아이템 단계 ──
    // 아이템 지급 전이라 보유 칸이 비어 있다. 아이템을 쓰는 경우는 ItemStateTests에서 확인한다

    // 보유 한도가 있는 라운드로 만든다
    private void SetItemCapacity(int capacity)
    {
        var round = new SerializedObject(_round);
        round.FindProperty("_itemCapacity").intValue = capacity;
        round.ApplyModifiedPropertiesWithoutUndo();
    }

    // 첫 번째 판의 턴 플레이어가 turnPlayer인 진행을 시드 범위에서 찾는다
    private GameFlow StartFlowWithTurnPlayer(Side turnPlayer)
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer == turnPlayer)
            {
                return flow;
            }
        }

        Assert.Fail($"{turnPlayer}가 선인 시드를 찾지 못했다");
        return null;
    }

    // 보유 한도가 0인 라운드는 아이템 단계를 건너뛰고 바로 판 결과로 간다
    [Test]
    public void ZeroItemCapacity_SkipsItemPhase()
    {
        GameFlow flow = StartFlowWithTurnPlayer(Side.Player);

        flow.DrawCard();
        flow.Distribute(keep: true);

        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
    }

    // 플레이어가 턴 플레이어면 플레이어 단계에서 입력을 기다리고, 끝내면 딜러 단계는 바로 지나 판 결과로 간다
    [Test]
    public void PlayerTurn_WaitsForPlayerItemPhase_ThenDealerPhasePasses()
    {
        SetItemCapacity(4);
        GameFlow flow = StartFlowWithTurnPlayer(Side.Player);

        flow.DrawCard();
        flow.Distribute(keep: true);
        Assert.AreEqual(GamePhase.ItemTurnPlayer, flow.CurrentPhase);

        Assert.IsTrue(flow.FinishItemPhase());

        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
    }

    // 딜러가 턴 플레이어면 딜러 단계가 먼저 지나고, 상대인 플레이어 단계에서 입력을 기다린다
    [Test]
    public void DealerTurn_DealerPhasePassesFirst_ThenWaitsForPlayer()
    {
        SetItemCapacity(4);
        GameFlow flow = StartFlowWithTurnPlayer(Side.Dealer);

        flow.PlayDealerTurn();
        Assert.AreEqual(GamePhase.ItemOpponent, flow.CurrentPhase);

        Assert.IsTrue(flow.FinishItemPhase());

        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
    }

    // 가지고 있지 않은 아이템은 쓸 수 없고, 아이템 단계에 그대로 머문다
    [Test]
    public void UseItem_NotHeld_IsIgnored()
    {
        SetItemCapacity(4);
        GameFlow flow = StartFlowWithTurnPlayer(Side.Player);
        flow.DrawCard();
        flow.Distribute(keep: true);

        Assert.IsFalse(flow.HasUsableItem(Side.Player));
        Assert.IsFalse(flow.UseItem(ItemType.Coffee));
        Assert.AreEqual(GamePhase.ItemTurnPlayer, flow.CurrentPhase);
        Assert.AreEqual(0, flow.Items.ItemUses.Count);
    }

    // 플레이어 아이템 단계가 아니면 아이템 입력을 무시한다
    [Test]
    public void ItemInput_OutsidePlayerItemPhase_IsIgnored()
    {
        SetItemCapacity(4);
        GameFlow flow = StartFlowWithTurnPlayer(Side.Player);

        Assert.IsFalse(flow.UseItem(ItemType.Coffee));
        Assert.IsFalse(flow.FinishItemPhase());
        Assert.IsFalse(flow.FinishDealerItemPhase());
        Assert.AreEqual(GamePhase.Draw, flow.CurrentPhase);
    }


    // ── 아이템 지급 (아이템 세부 기획서 4-2장 지급 시점, 코어 규칙 상세 기획서 6장 예외 E1) ──

    // 3라운드 값: 보유 한도 6, 라운드 시작 돋보기 1, 카드 셔플당 2
    private const int Round3Capacity = 6;
    private const int Round3StartMagnifierCount = 1;
    private const int Round3ItemsPerShuffle = 2;

    private void SetRound3ItemValues()
    {
        var round = new SerializedObject(_round);
        round.FindProperty("_itemCapacity").intValue = Round3Capacity;
        round.FindProperty("_startMagnifierCount").intValue = Round3StartMagnifierCount;
        round.FindProperty("_itemsPerShuffle").intValue = Round3ItemsPerShuffle;
        round.ApplyModifiedPropertiesWithoutUndo();
    }

    // 라운드의 첫 카드 셔플까지 라운드 시작 돋보기와 셔플 지급을 모두 받는다 (첫 셔플 후 보유 3 / 6)
    [Test]
    public void StartRound_SuppliesStartMagnifierThenShuffleItems()
    {
        SetRound3ItemValues();
        const int expectedCount = Round3StartMagnifierCount + Round3ItemsPerShuffle;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);

            foreach (Side side in new[] { Side.Player, Side.Dealer })
            {
                Assert.AreEqual(expectedCount, flow.Items.GetItems(side).Count, $"seed {seed} {side}");
                Assert.AreEqual(ItemType.Magnifier, flow.Items.GetItems(side)[0], $"seed {seed} {side}");
            }
        }
    }

    // 선 정하기가 비겨 다시 섞어도 셔플 지급은 한 번만 받는다
    [Test]
    public void DecideFirstDraw_Reshuffle_DoesNotSupplyAgain()
    {
        SetRound3ItemValues();
        const int expectedCount = Round3StartMagnifierCount + Round3ItemsPerShuffle;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
            var logs = new List<string>();
            flow.Logged += logs.Add;
            flow.StartRound();

            if (!logs.Exists(log => log.Contains("무승부")))
            {
                continue;
            }

            Assert.AreEqual(expectedCount, flow.Items.GetItems(Side.Player).Count);
            Assert.AreEqual(expectedCount, flow.Items.GetItems(Side.Dealer).Count);
            return;
        }

        Assert.Fail("선 정하기 무승부가 나는 시드를 찾지 못했다");
    }

    // 정산 뒤 다음 카드 셔플에서 셔플당 개수만큼 더 받는다. 쓰지 않은 아이템은 남는다 (아이템 세부 기획서 4-1장)
    [Test]
    public void NextShuffle_SuppliesShuffleItemsAgain()
    {
        SetRound3ItemValues();
        const int expectedCount = Round3StartMagnifierCount + Round3ItemsPerShuffle * 2;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            var logs = new List<string>();
            flow.Logged += logs.Add;
            PlayOneShuffle(flow);
            if (flow.CurrentPhase != GamePhase.Settlement)
            {
                continue;
            }

            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd)
            {
                continue;
            }

            // 딜러는 아이템 판단으로 아이템을 쓰므로, 쓴 만큼 빼고 남은 수를 확인한다
            int dealerUsedCount = logs.FindAll(log => log.StartsWith("딜러가 ") && log.EndsWith("를 썼다")).Count;
            Assert.AreEqual(2, flow.ShuffleCount);
            Assert.AreEqual(expectedCount, flow.Items.GetItems(Side.Player).Count);
            Assert.AreEqual(expectedCount - dealerUsedCount, flow.Items.GetItems(Side.Dealer).Count);
            return;
        }

        Assert.Fail("두 번째 카드 셔플까지 가는 시드를 찾지 못했다");
    }

    // 같은 난수 시드면 양쪽이 같은 아이템을 받는다 (테스트 재현용)
    [Test]
    public void SameSeed_SuppliesSameItems()
    {
        SetRound3ItemValues();

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow first = StartFlow(seed);
            GameFlow second = StartFlow(seed);

            CollectionAssert.AreEqual(first.Items.GetItems(Side.Player), second.Items.GetItems(Side.Player), $"seed {seed}");
            CollectionAssert.AreEqual(first.Items.GetItems(Side.Dealer), second.Items.GetItems(Side.Dealer), $"seed {seed}");
        }
    }


    // ── 커피 (아이템 세부 기획서 2-1장 커피, 5장 I3·I5) ──

    // 카드 셔플 지급이 모두 커피가 되도록 다른 종류의 지급 가중치를 0으로 둔다. 라운드 시작 돋보기는 그대로 받는다
    private void SetCoffeeOnlySupply()
    {
        var itemSettings = new SerializedObject(_itemSettings);
        itemSettings.FindProperty("_magnifierWeight").intValue = 0;
        itemSettings.FindProperty("_clockWeight").intValue = 0;
        itemSettings.ApplyModifiedPropertiesWithoutUndo();
    }

    // 플레이어가 커피를 쓴 판: 이기면 딜러 체력이 기본 데미지 × 커피 배수만큼 줄고, 지면 기본 데미지만, 비기면 변화 없다.
    // 커피는 보유 칸에서 빠지고, 다음 판에는 사용 기록이 남지 않는다
    [Test]
    public void PlayerUsedCoffee_DoublesDamageOnlyWhenPlayerWins()
    {
        SetRound3ItemValues();
        SetCoffeeOnlySupply();
        int coffeeDamage = _rules.MatchDamage * _rules.CoffeeMultiplier;
        bool sawWin = false;
        bool sawLoseOrDraw = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Player)
            {
                continue;
            }

            flow.DrawCard();
            flow.Distribute(keep: true);
            int coffeeCountBefore = CountItem(flow.Items.GetItems(Side.Player), ItemType.Coffee);
            int playerBefore = flow.Health.GetHealth(Side.Player);
            int dealerBefore = flow.Health.GetHealth(Side.Dealer);

            Assert.IsTrue(flow.UseItem(ItemType.Coffee), $"seed {seed}");
            Assert.AreEqual(coffeeCountBefore - 1, CountItem(flow.Items.GetItems(Side.Player), ItemType.Coffee), $"seed {seed}");
            FinishItemPhases(flow);
            Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase, $"seed {seed}");

            // 판 결과를 보여 주는 동안 각자 조합 칸의 마지막 카드가 이번 판 카드다
            HandShape playerShape = GetLastSlotShape(flow, Side.Player);
            HandShape dealerShape = GetLastSlotShape(flow, Side.Dealer);
            MatchResult playerResult = HandShapeRules.Compare(playerShape, dealerShape);
            int playerLoss = playerBefore - flow.Health.GetHealth(Side.Player);
            int dealerLoss = dealerBefore - flow.Health.GetHealth(Side.Dealer);

            switch (playerResult)
            {
                case MatchResult.Win:
                    sawWin = true;
                    Assert.AreEqual(0, playerLoss, $"seed {seed}");
                    Assert.AreEqual(Mathf.Min(coffeeDamage, dealerBefore), dealerLoss, $"seed {seed}");
                    break;
                case MatchResult.Lose:
                    sawLoseOrDraw = true;
                    Assert.AreEqual(Mathf.Min(_rules.MatchDamage, playerBefore), playerLoss, $"seed {seed}");
                    Assert.AreEqual(0, dealerLoss, $"seed {seed}");
                    break;
                default:
                    sawLoseOrDraw = true;
                    Assert.AreEqual(0, playerLoss, $"seed {seed}");
                    Assert.AreEqual(0, dealerLoss, $"seed {seed}");
                    break;
            }

            // 판이 바뀌면 이번 판 사용 기록이 비워져 커피 효과가 다음 판으로 이어지지 않는다
            flow.Next();
            if (flow.CurrentPhase != GamePhase.RoundEnd && flow.CurrentPhase != GamePhase.Settlement)
            {
                Assert.AreEqual(0, flow.Items.ItemUses.Count, $"seed {seed}");
            }
        }

        Assert.IsTrue(sawWin && sawLoseOrDraw, "시드 범위 안에서 플레이어가 이기는 판과 지거나 비기는 판이 모두 나와야 한다");
    }


    // ── 돋보기 (아이템 세부 기획서 2-2장 돋보기, 5장 I1·I2, 코어 규칙 상세 기획서 5-3장) ──
    // 3라운드 값이면 라운드 시작 지급으로 양쪽 모두 돋보기를 1개 가지고 시작한다

    // 플레이어가 턴 플레이어로 뽑은 카드를 가진 뒤, 플레이어 아이템 단계까지 진행한다
    private GameFlow StartPlayerItemPhaseWithMagnifier()
    {
        SetRound3ItemValues();
        GameFlow flow = StartFlowWithTurnPlayer(Side.Player);
        flow.DrawCard();
        flow.Distribute(keep: true);
        Assert.AreEqual(GamePhase.ItemTurnPlayer, flow.CurrentPhase);
        return flow;
    }

    // 돋보기를 쓰면 딜러 자리 카드를 플레이어만 알게 되고, 진행 단계는 그대로인 채 선택을 기다린다.
    // 고르는 동안에는 다른 아이템 입력을 받지 않는다
    [Test]
    public void UseMagnifier_OnlyPlayerLearnsDealerCard_AndWaitsForChoice()
    {
        GameFlow flow = StartPlayerItemPhaseWithMagnifier();
        Card dealerCard = GetLastSlotCard(flow, Side.Dealer);

        Assert.IsTrue(flow.UseItem(ItemType.Magnifier));

        Assert.IsTrue(dealerCard.KnownByPlayer);
        Assert.IsFalse(dealerCard.KnownByDealer);
        Assert.IsTrue(flow.IsWaitingForMagnifierChoice);
        Assert.AreEqual(GamePhase.ItemTurnPlayer, flow.CurrentPhase);
        Assert.IsFalse(flow.FinishItemPhase());
        Assert.IsFalse(flow.UseItem(ItemType.Coffee));
        Assert.AreEqual(GamePhase.ItemTurnPlayer, flow.CurrentPhase);
    }

    // 바꾼다: 카드가 아는 사람 정보와 함께 자리를 바꾸고, 사용 기록에 교환이 남는다.
    // 판정에는 교환된 카드가 쓰이고, 사용 알림은 돋보기 사용과 선택 때 한 번씩 온다
    [Test]
    public void ChooseExchange_SwapsCards_RecordsExchange_AndJudgesSwappedCards()
    {
        GameFlow flow = StartPlayerItemPhaseWithMagnifier();
        Card playerCardBefore = GetLastSlotCard(flow, Side.Player);
        Card dealerCardBefore = GetLastSlotCard(flow, Side.Dealer);
        int itemUsedCount = 0;
        flow.ItemUsed += () => itemUsedCount++;

        flow.UseItem(ItemType.Magnifier);
        Assert.IsTrue(flow.ChooseMagnifierExchange(exchange: true));

        Assert.AreEqual(2, itemUsedCount);
        Assert.IsFalse(flow.IsWaitingForMagnifierChoice);
        Assert.IsTrue(flow.Table.IsMatchCardSwapped);
        Assert.AreSame(dealerCardBefore, GetLastSlotCard(flow, Side.Player));
        Assert.AreSame(playerCardBefore, GetLastSlotCard(flow, Side.Dealer));
        // 교환 뒤 플레이어 자리 카드(원래 딜러 자리 카드)는 플레이어만 안다 (코어 규칙 상세 기획서 5-3장 경우 A)
        Assert.IsTrue(dealerCardBefore.KnownByPlayer);
        Assert.IsFalse(dealerCardBefore.KnownByDealer);
        Assert.AreEqual((Side.Player, ItemType.Magnifier, true), flow.Items.ItemUses[0]);

        int playerBefore = flow.Health.GetHealth(Side.Player);
        int dealerBefore = flow.Health.GetHealth(Side.Dealer);
        Assert.IsTrue(flow.FinishItemPhase());
        FinishItemPhases(flow);
        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);

        MatchResult playerResult = HandShapeRules.Compare(dealerCardBefore.Shape, playerCardBefore.Shape);
        int expectedPlayerLoss = playerResult == MatchResult.Lose ? Mathf.Min(_rules.MatchDamage, playerBefore) : 0;
        int expectedDealerLoss = playerResult == MatchResult.Win ? Mathf.Min(_rules.MatchDamage, dealerBefore) : 0;
        Assert.AreEqual(expectedPlayerLoss, playerBefore - flow.Health.GetHealth(Side.Player));
        Assert.AreEqual(expectedDealerLoss, dealerBefore - flow.Health.GetHealth(Side.Dealer));
    }

    // 바꿔서 판정하면 판정 기록의 결과 줄에 [돋보기]가 붙는다. 교환했어도 손 모양이 같아 비기면 붙지 않는다 (RSP-100)
    [Test]
    public void ChooseExchange_JudgmentRecordMarksMagnifier_UnlessDraw()
    {
        GameFlow flow = StartPlayerItemPhaseWithMagnifier();
        Card playerCardBefore = GetLastSlotCard(flow, Side.Player);
        Card dealerCardBefore = GetLastSlotCard(flow, Side.Dealer);
        var lines = new List<string>();
        flow.JudgmentRecorded += lines.Add;

        flow.UseItem(ItemType.Magnifier);
        flow.ChooseMagnifierExchange(exchange: true);
        flow.FinishItemPhase();
        FinishItemPhases(flow);

        string matchLine = lines[lines.Count - 1];
        StringAssert.StartsWith("첫 번째 판:", matchLine);
        bool isDraw = HandShapeRules.Compare(dealerCardBefore.Shape, playerCardBefore.Shape) == MatchResult.Draw;
        Assert.AreEqual(!isDraw, matchLine.Contains("[돋보기]"));
    }

    // 그대로 두고 판정하면 결과 줄에 [돋보기]가 붙지 않는다 (RSP-100)
    [Test]
    public void ChooseKeep_JudgmentRecordHasNoMagnifierMark()
    {
        GameFlow flow = StartPlayerItemPhaseWithMagnifier();
        var lines = new List<string>();
        flow.JudgmentRecorded += lines.Add;

        flow.UseItem(ItemType.Magnifier);
        flow.ChooseMagnifierExchange(exchange: false);
        flow.FinishItemPhase();
        FinishItemPhases(flow);

        string matchLine = lines[lines.Count - 1];
        StringAssert.StartsWith("첫 번째 판:", matchLine);
        StringAssert.DoesNotContain("[돋보기]", matchLine);
    }

    // 그대로 둔다: 카드는 그대로이고 "돋보기 사용 / 교환 안 함"이 기록된다. 다시 아이템 단계 입력을 받는다
    [Test]
    public void ChooseKeep_LeavesCards_AndRecordsNoExchange()
    {
        GameFlow flow = StartPlayerItemPhaseWithMagnifier();
        Card playerCardBefore = GetLastSlotCard(flow, Side.Player);

        flow.UseItem(ItemType.Magnifier);
        Assert.IsTrue(flow.ChooseMagnifierExchange(exchange: false));

        Assert.IsFalse(flow.IsWaitingForMagnifierChoice);
        Assert.IsFalse(flow.Table.IsMatchCardSwapped);
        Assert.AreSame(playerCardBefore, GetLastSlotCard(flow, Side.Player));
        Assert.AreEqual((Side.Player, ItemType.Magnifier, false), flow.Items.ItemUses[0]);
        Assert.IsTrue(flow.FinishItemPhase());
    }

    // 돋보기를 쓰지 않았으면 선택 입력을 무시한다
    [Test]
    public void ChooseMagnifierExchange_WithoutMagnifier_IsIgnored()
    {
        GameFlow flow = StartPlayerItemPhaseWithMagnifier();

        Assert.IsFalse(flow.ChooseMagnifierExchange(exchange: true));
        Assert.IsFalse(flow.Table.IsMatchCardSwapped);
        Assert.AreEqual(0, flow.Items.ItemUses.Count);
    }


    // ── 시계 (아이템 세부 기획서 2-3장 시계, 5장 I6) ──
    // 3라운드 값에 카드 셔플 지급이 모두 시계가 되도록 해, 플레이어가 첫 카드 셔플부터 시계 2개를 가지고 시작한다

    private void SetRound3ClockOnlySupply()
    {
        SetRound3ItemValues();
        var itemSettings = new SerializedObject(_itemSettings);
        itemSettings.FindProperty("_coffeeWeight").intValue = 0;
        itemSettings.FindProperty("_magnifierWeight").intValue = 0;
        itemSettings.ApplyModifiedPropertiesWithoutUndo();
    }

    // 첫 번째 판에 시계를 쓰면 두 번째 판도 같은 턴 플레이어이고, 이월이 없어 다음 카드 셔플은 선 정하기를 한다
    [Test]
    public void ClockOnFirstMatch_KeepsTurn_AndNextShuffleDecidesFirst()
    {
        SetRound3ClockOnlySupply();
        bool sawNextShuffle = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Player) { continue; }

            var logs = new List<string>();
            flow.Logged += logs.Add;
            flow.DrawCard();
            flow.Distribute(keep: true);
            Assert.IsTrue(flow.UseItem(ItemType.Clock), $"seed {seed}");
            Assert.IsTrue(flow.Items.HasUsedItem(Side.Player, ItemType.Clock), $"seed {seed}");
            FinishItemPhases(flow);
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            Assert.AreEqual(1, flow.MatchIndex, $"seed {seed}");
            Assert.AreEqual(Side.Player, flow.Table.TurnPlayer, $"seed {seed}");

            PlayTurn(flow);
            flow.Next();
            if (flow.CurrentPhase != GamePhase.Settlement) { continue; }

            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            sawNextShuffle = true;
            Assert.IsFalse(logs.Exists(log => log.StartsWith("시계 이월")), $"seed {seed}");
            Assert.IsTrue(logs.Exists(log => log.StartsWith("선 정하기")), $"seed {seed}");
        }

        Assert.IsTrue(sawNextShuffle, "시드 범위 안에서 다음 카드 셔플까지 이어지는 진행이 나와야 한다");
    }

    // 두 번째 판에 시계를 쓰면 다음 카드 셔플은 선 정하기 없이 시계를 쓴 쪽이 첫 번째 판 턴 플레이어다.
    // 이월은 한 번 쓰면 사라져, 그다음 카드 셔플은 다시 선 정하기를 한다
    [Test]
    public void ClockOnSecondMatch_SkipsDecideFirstOnNextShuffleOnly()
    {
        SetRound3ClockOnlySupply();
        bool sawCarryOver = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Dealer) { continue; }

            PlayTurn(flow);
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            // 딜러가 첫 번째 판에 시계를 써서 턴을 유지한 시드는 건너뛴다
            if (flow.Table.TurnPlayer != Side.Player) { continue; }

            // 두 번째 판은 턴이 넘어와 플레이어 차례다
            flow.DrawCard();
            flow.Distribute(keep: true);
            Assert.IsTrue(flow.UseItem(ItemType.Clock), $"seed {seed}");
            FinishItemPhases(flow);
            flow.Next();
            if (flow.CurrentPhase != GamePhase.Settlement) { continue; }

            var logs = new List<string>();
            var records = new List<string>();
            flow.Logged += logs.Add;
            flow.JudgmentRecorded += records.Add;
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            sawCarryOver = true;
            Assert.AreEqual(2, flow.ShuffleCount, $"seed {seed}");
            Assert.AreEqual(0, flow.MatchIndex, $"seed {seed}");
            Assert.AreEqual(Side.Player, flow.Table.TurnPlayer, $"seed {seed}");
            Assert.IsTrue(logs.Exists(log => log.StartsWith("시계 이월로 선 정하기 생략")), $"seed {seed}");
            Assert.AreEqual(0, records.Count, $"seed {seed}");

            // 이번 카드 셔플은 시계를 쓰지 않고 끝내, 다음 카드 셔플에서 선 정하기가 다시 나오는지 본다.
            // 딜러가 시계를 쓴 시드는 이월이 다시 생길 수 있어 건너뛴다
            logs.Clear();
            PlayOneShuffle(flow);
            if (flow.CurrentPhase != GamePhase.Settlement) { continue; }
            if (logs.Exists(log => log.StartsWith("딜러가 시계"))) { continue; }

            logs.Clear();
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            Assert.IsFalse(logs.Exists(log => log.StartsWith("시계 이월")), $"seed {seed}");
            Assert.IsTrue(logs.Exists(log => log.StartsWith("선 정하기")), $"seed {seed}");
        }

        Assert.IsTrue(sawCarryOver, "시드 범위 안에서 시계 이월로 다음 카드 셔플이 시작되는 진행이 나와야 한다");
    }

    private static Card GetLastSlotCard(GameFlow flow, Side side)
    {
        IReadOnlyList<Card> slots = flow.Table.GetCombinationSlots(side);
        return slots[slots.Count - 1];
    }

    private static int CountItem(IReadOnlyList<ItemType> items, ItemType item)
    {
        int count = 0;
        foreach (ItemType heldItem in items)
        {
            if (heldItem == item)
            {
                count++;
            }
        }

        return count;
    }

    private static HandShape GetLastSlotShape(GameFlow flow, Side side)
    {
        IReadOnlyList<Card> slots = flow.Table.GetCombinationSlots(side);
        return slots[slots.Count - 1].Shape;
    }

    // ── 딜러 아이템 단계 (RSP-58, 딜러 AI 세부 기획서 4장) ──
    // 판단 규칙 자체는 DealerAITests에서 확인하고, 여기서는 진행 중에 딜러가 고른 아이템이 실제로 적용되는지 확인한다

    // 2라운드 값: 보유 한도 4, 라운드 시작 돋보기 1, 카드 셔플당 1
    private void SetRound2ItemValues()
    {
        var round = new SerializedObject(_round);
        round.FindProperty("_itemCapacity").intValue = 4;
        round.FindProperty("_startMagnifierCount").intValue = 1;
        round.FindProperty("_itemsPerShuffle").intValue = 1;
        round.ApplyModifiedPropertiesWithoutUndo();
    }

    // 딜러가 돋보기를 쓰면 플레이어 자리 카드를 알게 되고, 교환 판단 결과가 사용 기록·카드 자리·로그에 똑같이 반영된다.
    // 플레이어는 아이템을 쓰지 않으므로 교환 표시는 딜러가 교환했을 때만 켜진다
    [Test]
    public void DealerMagnifier_InFlow_AppliesExchangeChoice()
    {
        SetRound2ItemValues();
        bool sawExchange = false;
        bool sawKeep = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            var logs = new List<string>();
            flow.Logged += logs.Add;
            PlayTurn(flow);
            if (!flow.Items.HasUsedItem(Side.Dealer, ItemType.Magnifier)) { continue; }

            bool exchanged = false;
            foreach ((Side User, ItemType Item, bool Exchanged) use in flow.Items.ItemUses)
            {
                if (use.User == Side.Dealer && use.Item == ItemType.Magnifier) { exchanged = use.Exchanged; }
            }

            Assert.IsTrue(logs.Exists(log => log.StartsWith("딜러가 돋보기로 플레이어 카드를 확인했다")), $"seed {seed}");
            Assert.AreEqual(exchanged, logs.Contains("딜러가 돋보기로 카드를 교환했다"), $"seed {seed}");
            Assert.AreEqual(!exchanged, logs.Contains("딜러가 카드를 그대로 뒀다"), $"seed {seed}");
            Assert.AreEqual(exchanged, flow.Table.IsMatchCardSwapped, $"seed {seed}");
            sawExchange |= exchanged;
            sawKeep |= !exchanged;
        }

        Assert.IsTrue(sawExchange && sawKeep, "시드 범위 안에서 교환과 그대로가 모두 나와야 한다");
    }

    // 딜러가 커피를 쓰고 판에서 이기면 플레이어 체력이 커피 배수만큼 줄어든다 (아이템 세부 기획서 2-1장 커피)
    [Test]
    public void DealerCoffee_InFlow_DoublesDamageWhenDealerWins()
    {
        SetRound2ItemValues();
        var itemSettings = new SerializedObject(_itemSettings);
        itemSettings.FindProperty("_magnifierWeight").intValue = 0;
        itemSettings.FindProperty("_clockWeight").intValue = 0;
        itemSettings.ApplyModifiedPropertiesWithoutUndo();
        bool sawCoffeeWin = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            int healthBefore = flow.Health.GetHealth(Side.Player);
            PlayTurn(flow);
            if (!flow.Items.HasUsedItem(Side.Dealer, ItemType.Coffee)) { continue; }

            MatchResult dealerResult = HandShapeRules.Compare(GetLastSlotShape(flow, Side.Dealer), GetLastSlotShape(flow, Side.Player));
            if (dealerResult != MatchResult.Win) { continue; }

            sawCoffeeWin = true;
            Assert.AreEqual(healthBefore - _rules.MatchDamage * _rules.CoffeeMultiplier, flow.Health.GetHealth(Side.Player), $"seed {seed}");
        }

        Assert.IsTrue(sawCoffeeWin, "시드 범위 안에서 딜러가 커피를 쓰고 이기는 판이 나와야 한다");
    }

    // 아이템이 있는 라운드라도 딜러에게 쓸 수 있는 아이템이 없으면 판단 없이 넘긴다 (아이템 세부 기획서 3-3장)
    [Test]
    public void DealerWithoutItems_InFlow_SkipsItemPhase()
    {
        SetItemCapacity(4);
        GameFlow flow = StartFlow(0);
        var logs = new List<string>();
        flow.Logged += logs.Add;

        PlayTurn(flow);

        Assert.IsTrue(logs.Contains("딜러가 쓸 수 있는 아이템이 없어 아이템 단계를 넘겼다"));
        Assert.AreEqual(0, flow.Items.ItemUses.Count);
        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
    }

    // ── 딜러 아이템 대기 (RSP-97, UI·UX 화면 흐름 세부 기획서 3-1장 판-06) ──
    // 2라운드 값이면 라운드 시작 지급으로 딜러가 돋보기를 가지고 시작해, 딜러에게 쓸 수 있는 아이템이 있다

    // 플레이어가 턴 플레이어면 플레이어 단계를 끝낸 뒤 딜러 단계에서 대기하고, 판정으로 가지 않는다.
    // 대기 중에는 플레이어 아이템 입력을 받지 않고, 대기를 끝내면 판 결과로 간다
    [Test]
    public void PlayerTurn_DealerWithItems_WaitsBeforeResolve()
    {
        SetRound2ItemValues();
        GameFlow flow = StartFlowWithTurnPlayer(Side.Player);
        flow.DrawCard();
        flow.Distribute(keep: true);

        Assert.IsTrue(flow.FinishItemPhase());

        Assert.AreEqual(GamePhase.ItemOpponent, flow.CurrentPhase);
        Assert.IsTrue(flow.IsWaitingForDealerItems);
        Assert.IsFalse(flow.FinishItemPhase());
        Assert.IsFalse(flow.UseItem(ItemType.Magnifier));

        Assert.IsTrue(flow.FinishDealerItemPhase());

        Assert.IsFalse(flow.IsWaitingForDealerItems);
        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
    }

    // 딜러가 턴 플레이어면 딜러 분배 뒤 딜러 단계에서 먼저 대기하고, 대기를 끝내면 플레이어 단계에서 입력을 기다린다
    [Test]
    public void DealerTurn_DealerWithItems_WaitsThenPlayerItemPhase()
    {
        SetRound2ItemValues();
        GameFlow flow = StartFlowWithTurnPlayer(Side.Dealer);

        Assert.IsTrue(flow.PlayDealerTurn());

        Assert.AreEqual(GamePhase.ItemTurnPlayer, flow.CurrentPhase);
        Assert.IsTrue(flow.IsWaitingForDealerItems);

        Assert.IsTrue(flow.FinishDealerItemPhase());

        Assert.AreEqual(GamePhase.ItemOpponent, flow.CurrentPhase);
        Assert.IsFalse(flow.IsWaitingForDealerItems);
        Assert.IsTrue(flow.FinishItemPhase());
        Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
    }

    // 화면은 단계 변경 알림 안에서 딜러 아이템 대기 여부를 읽는다. 아이템 진행이 이번 판 테이블을 받기 전인 첫 번째 판과
    // 테이블이 새로 만들어진 다음 카드 셔플에서도 오류 없이, 지금 아이템을 쓰는 쪽이 딜러인지로 맞게 나와야 한다 (RSP-97 Play 검증에서 발견)
    [Test]
    public void IsWaitingForDealerItems_InPhaseChanged_MatchesActingSide()
    {
        SetRound2ItemValues();
        int itemPhaseEventCount = 0;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, _itemSettings, new System.Random(seed));
            flow.PhaseChanged += (previous, next) =>
            {
                bool expected = false;
                if (next == GamePhase.ItemTurnPlayer || next == GamePhase.ItemOpponent)
                {
                    itemPhaseEventCount++;
                    Side actingSide = next == GamePhase.ItemTurnPlayer ? flow.Table.TurnPlayer : TableState.Opponent(flow.Table.TurnPlayer);
                    expected = actingSide == Side.Dealer;
                }

                Assert.AreEqual(expected, flow.IsWaitingForDealerItems, $"seed {seed}, {previous} → {next}");
            };

            flow.StartRound();
            PlayOneShuffle(flow);
            if (flow.CurrentPhase != GamePhase.Settlement) { continue; }

            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            PlayOneShuffle(flow);
        }

        Assert.Greater(itemPhaseEventCount, 0);
    }

    // 딜러가 턴 플레이어로 시계를 쓰면 다음 판도 딜러 차례이고,
    // 두 번째 판에 쓰면 다음 카드 셔플은 선 정하기 없이 딜러가 첫 번째 판 턴 플레이어다 (아이템 세부 기획서 5장 I6·I7)
    [Test]
    public void DealerClock_InFlow_KeepsTurnAndCarriesOver()
    {
        SetRound3ClockOnlySupply();
        bool sawKeepTurn = false;
        bool sawCarryOver = false;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.Table.TurnPlayer != Side.Dealer) { continue; }

            PlayTurn(flow);
            if (!flow.Items.HasUsedItem(Side.Dealer, ItemType.Clock)) { continue; }

            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            sawKeepTurn = true;
            Assert.AreEqual(1, flow.MatchIndex, $"seed {seed}");
            Assert.AreEqual(Side.Dealer, flow.Table.TurnPlayer, $"seed {seed}");

            PlayTurn(flow);
            if (!flow.Items.HasUsedItem(Side.Dealer, ItemType.Clock)) { continue; }

            flow.Next();
            if (flow.CurrentPhase != GamePhase.Settlement) { continue; }

            var logs = new List<string>();
            flow.Logged += logs.Add;
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd) { continue; }

            sawCarryOver = true;
            Assert.AreEqual(0, flow.MatchIndex, $"seed {seed}");
            Assert.AreEqual(Side.Dealer, flow.Table.TurnPlayer, $"seed {seed}");
            Assert.IsTrue(logs.Exists(log => log.StartsWith("시계 이월로 선 정하기 생략")), $"seed {seed}");
        }

        Assert.IsTrue(sawKeepTurn && sawCarryOver, "시드 범위 안에서 딜러의 시계 턴 유지와 이월이 모두 나와야 한다");
    }
}
