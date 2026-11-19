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

    // 판 결과 확인 지점까지 진행한다. 딜러 차례면 고민 중 대기가 끝난 것으로 보고 딜러가 행동하고, 플레이어 차례면 뽑고 가진다로 분배한다
    private static void PlayTurn(GameFlow flow)
    {
        if (flow.IsWaitingForDealer)
        {
            flow.PlayDealerTurn();
            return;
        }

        flow.DrawCard();
        flow.Distribute(keep: true);
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
            GamePhase expectedPhase = flow.TurnPlayer == Side.Player ? GamePhase.Draw : GamePhase.Distribute;
            Assert.AreEqual(expectedPhase, flow.CurrentPhase);
            Assert.AreEqual(1, flow.GetCombinationSlots(Side.Player).Count);
            Assert.AreEqual(1, flow.GetCombinationSlots(Side.Dealer).Count);
            Assert.AreEqual(flow.TurnPlayer == Side.Dealer, flow.IsWaitingForDealer);

            sawPlayerTurn |= flow.TurnPlayer == Side.Player;
            sawDealerTurn |= flow.TurnPlayer == Side.Dealer;
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

    // 판-03: 딜러가 판 시작 때 먼저 뽑고, 뽑은 카드는 딜러만 안다 (UI·UX 화면 흐름 세부 기획서 3-1장, 4-2장)
    [Test]
    public void DealerTurn_DrawsAtMatchStart_KnownOnlyByDealer()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.TurnPlayer != Side.Dealer)
            {
                continue;
            }

            Assert.IsTrue(flow.IsWaitingForDealer);
            Assert.IsNotNull(flow.DrawnCard);
            Assert.IsTrue(flow.DrawnCard.KnownByDealer);
            Assert.IsFalse(flow.DrawnCard.KnownByPlayer);
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
            if (flow.TurnPlayer != Side.Dealer)
            {
                continue;
            }

            Card drawn = flow.DrawnCard;
            Assert.IsTrue(flow.PlayDealerTurn());
            Assert.AreEqual(GamePhase.ShowResult, flow.CurrentPhase);
            Assert.IsFalse(flow.IsWaitingForDealer);
            Assert.IsNull(flow.DrawnCard);
            // 가졌으면 딜러 자리, 줬으면 플레이어 자리에 뽑은 카드가 들어간다
            Assert.IsTrue(flow.GetCombinationSlots(Side.Dealer)[1] == drawn || flow.GetCombinationSlots(Side.Player)[1] == drawn);
            Assert.AreEqual(2, flow.GetCombinationSlots(Side.Player).Count);
            Assert.AreEqual(2, flow.GetCombinationSlots(Side.Dealer).Count);
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

            PlayTurn(flow);
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
            Assert.AreEqual(GamePhase.Draw, flow.CurrentPhase);
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
            PlayTurn(flow);
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

            // 플레이어가 선이면 뽑기 전, 딜러가 선이면 딜러가 뽑은 뒤 분배 전에 멈춘다
            GamePhase expectedPhase = flow.TurnPlayer == Side.Player ? GamePhase.Draw : GamePhase.Distribute;
            Assert.AreEqual(0, flow.MatchIndex);
            Assert.AreEqual(expectedPhase, flow.CurrentPhase);
            Assert.AreEqual(1, flow.GetCombinationSlots(Side.Player).Count);
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
            var flow = new GameFlow(_rules, _round, _dealerSettings, new System.Random(seed));
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
            Assert.AreEqual(5, flow.MatchStartDeckCount);
            Assert.AreEqual(3, flow.MatchEndDeckCount);

            PlayTurn(flow);
            flow.Next();
            if (flow.CurrentPhase == GamePhase.RoundEnd)
            {
                continue;
            }

            Assert.AreEqual(3, flow.MatchStartDeckCount);
            Assert.AreEqual(1, flow.MatchEndDeckCount);

            PlayTurn(flow);
            flow.Next();
            if (flow.CurrentPhase != GamePhase.Settlement)
            {
                continue;
            }

            Assert.AreEqual(3, flow.MatchStartDeckCount);
            Assert.AreEqual(1, flow.MatchEndDeckCount);
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
            flow.GetPoolCardCount(HandShape.Scissors),
            flow.GetPoolCardCount(HandShape.Rock),
            flow.GetPoolCardCount(HandShape.Paper)
        };
    }

    [Test]
    public void InputInWrongPhase_IsIgnored()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            GameFlow flow = StartFlow(seed);
            if (flow.TurnPlayer != Side.Player)
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
            var flow = new GameFlow(_rules, _round, _dealerSettings, new System.Random(seed));
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
    // 라운드 첫 카드 셔플 진입은 셔플 테이블이 없을 때라, 알림을 받은 화면이 턴 플레이어를 읽으면 오류가 난다
    [Test]
    public void PhaseChanged_ComesPerPhaseChange_ExceptEnteringCardShuffle()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var flow = new GameFlow(_rules, _round, _dealerSettings, new System.Random(seed));
            var phases = new List<GamePhase>();
            flow.PhaseChanged += (previous, next) =>
            {
                phases.Add(next);
                // 알림을 받는 시점에 화면이 읽는 상태를 오류 없이 읽을 수 있어야 한다
                Assert.DoesNotThrow(() => _ = flow.TurnPlayer);
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
            GamePhase[] expected = flow.TurnPlayer == Side.Player
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
            if (flow.TurnPlayer != Side.Player)
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
            var flow = new GameFlow(_rules, _round, _dealerSettings, new System.Random(seed));
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

            int playerBefore = flow.GetHealth(Side.Player);
            int dealerBefore = flow.GetHealth(Side.Dealer);
            PlayTurn(flow);

            bool changed = flow.GetHealth(Side.Player) != playerBefore || flow.GetHealth(Side.Dealer) != dealerBefore;
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
            if (flow.TurnPlayer == turnPlayer)
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
        Assert.AreEqual(0, flow.ItemUses.Count);
    }

    // 플레이어 아이템 단계가 아니면 아이템 입력을 무시한다
    [Test]
    public void ItemInput_OutsidePlayerItemPhase_IsIgnored()
    {
        SetItemCapacity(4);
        GameFlow flow = StartFlowWithTurnPlayer(Side.Player);

        Assert.IsFalse(flow.UseItem(ItemType.Coffee));
        Assert.IsFalse(flow.FinishItemPhase());
        Assert.AreEqual(GamePhase.Draw, flow.CurrentPhase);
    }
}
