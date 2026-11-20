using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// 아이템 진행 (RSP-94). 아이템 세부 기획서 3장 아이템 단계 순서, 2-2장 돋보기 기준.
// 게임 진행을 거친 아이템 사용·돋보기·커피 결과는 GameFlowTests에서 확인하고, 여기서는 차례 진행과 입력 차단만 확인한다
public class ItemPhaseFlowTests
{
    private const int Capacity = 4;

    private RoundSettings _round;
    private ItemSettings _itemSettings;
    private RuleSettings _rules;
    private DealerSettings _dealerSettings;
    private PhaseMachine _phaseMachine;
    private ItemState _items;
    private ItemPhaseFlow _itemPhase;
    private readonly List<string> _logs = new List<string>();

    // 셔플이 순서를 바꾸지 않도록 항상 마지막 칸을 고르는 난수.
    // 피셔-예이츠에서 j = i가 되어 넣은 순서 그대로 더미가 만들어진다
    private class KeepOrderRandom : System.Random
    {
        public override int Next(int maxValue)
        {
            return maxValue - 1;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _round = ScriptableObject.CreateInstance<RoundSettings>();
        _itemSettings = ScriptableObject.CreateInstance<ItemSettings>();
        _rules = ScriptableObject.CreateInstance<RuleSettings>();
        _dealerSettings = ScriptableObject.CreateInstance<DealerSettings>();
        _logs.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_round);
        Object.DestroyImmediate(_itemSettings);
        Object.DestroyImmediate(_rules);
        Object.DestroyImmediate(_dealerSettings);
    }

    // 보유 한도 capacity인 아이템 진행을 만들고, 플레이어에게 playerItems를 준다
    private void CreateItemPhase(int capacity, params ItemType[] playerItems)
    {
        _phaseMachine = new PhaseMachine();
        _items = new ItemState(capacity);
        foreach (ItemType item in playerItems)
        {
            _items.TryAddItem(Side.Player, item);
        }

        var random = new System.Random(0);
        _itemPhase = new ItemPhaseFlow(_phaseMachine, _items, new DealerAI(_rules, _dealerSettings, random), _round, _itemSettings, random);
        _itemPhase.Logged += _logs.Add;
    }

    // 시작 카드(플레이어 firstPlayerShape, 딜러 firstDealerShape)가 맨 위에 있는 카드 더미로 테이블을 만든다
    private static TableState CreateTable(HandShape firstPlayerShape, HandShape firstDealerShape)
    {
        var cards = new[] { firstPlayerShape, firstDealerShape, HandShape.Rock, HandShape.Scissors, HandShape.Paper, HandShape.Rock, HandShape.Scissors }
            .Select(shape => new Card(shape));
        return new TableState(new CardDeck(cards, new KeepOrderRandom()));
    }

    // 시작 카드로 선을 정하고, 턴 플레이어가 뽑아 가진 뒤 아이템 단계를 시작한다
    private TableState StartItemPhase(HandShape firstPlayerShape, HandShape firstDealerShape)
    {
        return StartItemPhase(CreateTable(firstPlayerShape, firstDealerShape));
    }

    private TableState StartItemPhase(TableState table)
    {
        _phaseMachine.CompleteRoundStart();
        _phaseMachine.CompleteCardShuffle();
        table.DealStartCards();
        _phaseMachine.CompleteStartCardDeal(hasClockCarryOver: false);
        table.DecideFirstTurnPlayer();
        _phaseMachine.CompleteDecideFirst(isDraw: false);
        table.BeginMatch();
        table.DrawForTurnPlayer();
        _phaseMachine.CompleteDraw();
        table.Distribute(keep: true);
        _phaseMachine.CompleteDistribute();

        _itemPhase.Start(table);
        return table;
    }

    // 아이템 단계를 끝낸 판을 판 결과까지 진행한 뒤 다음 판의 아이템 단계를 시작한다. 턴 교대는 게임 진행처럼 시계 판단 결과로 한다
    private void StartNextMatchItemPhase(TableState table)
    {
        _phaseMachine.CompleteReveal();
        _phaseMachine.CompleteJudge();
        table.PassTurn(_itemPhase.ApplyClockAtMatchEnd());
        _phaseMachine.CompleteShowResult(someoneAtZeroHealth: false);

        _itemPhase.BeginMatch();
        table.BeginMatch();
        table.DrawForTurnPlayer();
        _phaseMachine.CompleteDraw();
        table.Distribute(keep: true);
        _phaseMachine.CompleteDistribute();
        _itemPhase.Start(table);
    }

    // 보유 한도가 0인 라운드는 두 아이템 단계를 바로 끝내고 카드 공개로 간다
    [Test]
    public void ZeroCapacity_FinishesBothStepsAtStart()
    {
        CreateItemPhase(0);

        StartItemPhase(HandShape.Scissors, HandShape.Paper);

        Assert.IsTrue(_itemPhase.IsFinished);
        Assert.AreEqual(GamePhase.Reveal, _phaseMachine.CurrentPhase);
    }

    // 플레이어가 턴 플레이어면 턴 플레이어 단계에서 입력을 기다리고, 끝내면 딜러 단계는 바로 지나 끝난다.
    // 딜러에게 쓸 수 있는 아이템이 없으면 판단 없이 넘긴다 (아이템 세부 기획서 3-3장)
    [Test]
    public void PlayerTurn_WaitsAtTurnPlayerStep_ThenDealerStepPasses()
    {
        CreateItemPhase(Capacity);

        StartItemPhase(HandShape.Scissors, HandShape.Paper);
        Assert.AreEqual(GamePhase.ItemTurnPlayer, _phaseMachine.CurrentPhase);
        Assert.IsTrue(_itemPhase.IsPlayerInputPhase);
        Assert.IsFalse(_itemPhase.IsFinished);

        Assert.IsTrue(_itemPhase.FinishPlayerPhase());

        Assert.IsTrue(_itemPhase.IsFinished);
        CollectionAssert.Contains(_logs, "딜러가 쓸 수 있는 아이템이 없어 아이템 단계를 넘겼다");
    }

    // ── 딜러 아이템 단계 (딜러 AI 세부 기획서 4장) ──

    // 딜러가 턴 플레이어로 바위를 가졌고 플레이어 자리 카드(가위)를 모른다. 돋보기로 가위를 확인하고,
    // 교환하면 지므로 그대로 둔 뒤, 이길 것을 알아 커피를 쓴다. 끝내면 상대인 플레이어 단계에서 입력을 기다린다
    [Test]
    public void DealerTurn_UsesMagnifierThenCoffee_ThenWaitsForPlayer()
    {
        CreateItemPhase(Capacity);
        _items.TryAddItem(Side.Dealer, ItemType.Magnifier);
        _items.TryAddItem(Side.Dealer, ItemType.Coffee);
        int itemUsedCount = 0;
        _itemPhase.ItemUsed += () => itemUsedCount++;

        StartItemPhase(HandShape.Paper, HandShape.Scissors);

        var expectedUses = new List<(Side, ItemType, bool)>
        {
            (Side.Dealer, ItemType.Magnifier, false),
            (Side.Dealer, ItemType.Coffee, false),
        };
        CollectionAssert.AreEqual(expectedUses, _items.ItemUses);
        Assert.AreEqual(2, itemUsedCount);
        Assert.AreEqual(GamePhase.ItemOpponent, _phaseMachine.CurrentPhase);
        Assert.IsTrue(_itemPhase.IsPlayerInputPhase);
    }

    // 아이템 세부 기획서 5장 I1·I2: 플레이어(턴 플레이어)가 돋보기로 교환한 뒤 딜러가 돋보기로 다시 교환하면
    // 카드가 원래 자리로 돌아와 교환 표시가 꺼진다.
    // 카드풀 바위 3 / 가위 2 / 보 2. 플레이어 가위 vs 딜러 보로 플레이어가 선, 바위를 뽑아 가지고 딜러 자리에는 보가 들어간다.
    // 교환 뒤 딜러는 자기 카드를 모르므로 카드풀 구성으로 센다: 교환 전(? vs 보) 승 2·패 3, 교환 후(보 vs ?) 승 3·패 2 → 다시 교환
    [Test]
    public void PlayerExchange_ThenDealerExchangesBack_ClearsSwapMark()
    {
        CreateItemPhase(Capacity, ItemType.Magnifier);
        _items.TryAddItem(Side.Dealer, ItemType.Magnifier);
        var cards = new[] { HandShape.Scissors, HandShape.Paper, HandShape.Rock, HandShape.Paper, HandShape.Rock, HandShape.Rock, HandShape.Scissors }
            .Select(shape => new Card(shape));
        TableState table = StartItemPhase(new TableState(new CardDeck(cards, new KeepOrderRandom())));

        Assert.IsTrue(_itemPhase.UseItem(ItemType.Magnifier));
        Assert.IsTrue(_itemPhase.ChooseMagnifierExchange(exchange: true));
        Assert.IsTrue(table.IsMatchCardSwapped);

        Assert.IsTrue(_itemPhase.FinishPlayerPhase());

        Assert.IsFalse(table.IsMatchCardSwapped);
        Assert.AreEqual(HandShape.Rock, table.GetMatchCard(Side.Player).Shape);
        var expectedUses = new List<(Side, ItemType, bool)>
        {
            (Side.Player, ItemType.Magnifier, true),
            (Side.Dealer, ItemType.Magnifier, true),
        };
        CollectionAssert.AreEqual(expectedUses, _items.ItemUses);
        Assert.IsTrue(_itemPhase.IsFinished);
    }

    // 딜러가 턴 플레이어면 딜러 단계가 먼저 지나고, 상대인 플레이어 단계에서 입력을 기다린다
    [Test]
    public void DealerTurn_DealerStepPassesFirst_ThenWaitsForPlayer()
    {
        CreateItemPhase(Capacity);

        StartItemPhase(HandShape.Paper, HandShape.Scissors);
        Assert.AreEqual(GamePhase.ItemOpponent, _phaseMachine.CurrentPhase);
        Assert.IsTrue(_itemPhase.IsPlayerInputPhase);

        Assert.IsTrue(_itemPhase.FinishPlayerPhase());

        Assert.IsTrue(_itemPhase.IsFinished);
    }

    // 돋보기 선택을 기다리는 동안에는 다른 아이템 사용과 단계 끝내기를 받지 않고, 고른 뒤에 다시 받는다.
    // 사용 알림은 돋보기 사용, 선택, 커피 사용 때 한 번씩 온다
    [Test]
    public void MagnifierChoice_BlocksOtherInputs_UntilChosen()
    {
        CreateItemPhase(Capacity, ItemType.Magnifier, ItemType.Coffee);
        int itemUsedCount = 0;
        _itemPhase.ItemUsed += () => itemUsedCount++;
        StartItemPhase(HandShape.Scissors, HandShape.Paper);

        Assert.IsTrue(_itemPhase.UseItem(ItemType.Magnifier));
        Assert.IsTrue(_itemPhase.IsWaitingForMagnifierChoice);
        Assert.IsFalse(_itemPhase.IsPlayerInputPhase);
        Assert.IsFalse(_itemPhase.UseItem(ItemType.Coffee));
        Assert.IsFalse(_itemPhase.FinishPlayerPhase());

        Assert.IsTrue(_itemPhase.ChooseMagnifierExchange(exchange: false));
        Assert.IsTrue(_itemPhase.UseItem(ItemType.Coffee));

        Assert.AreEqual(3, itemUsedCount);
        Assert.AreEqual(GamePhase.ItemTurnPlayer, _phaseMachine.CurrentPhase);
    }

    // ── 시계 (아이템 세부 기획서 2-3장 시계, 5장 I6·I7) ──

    // 시계를 쓰지 않은 판은 턴을 유지하지 않고 이월도 없다
    [Test]
    public void ApplyClockAtMatchEnd_WithoutClock_ReturnsFalse()
    {
        CreateItemPhase(Capacity, ItemType.Clock);
        StartItemPhase(HandShape.Scissors, HandShape.Paper);
        _itemPhase.FinishPlayerPhase();
        _phaseMachine.CompleteReveal();
        _phaseMachine.CompleteJudge();

        Assert.IsFalse(_itemPhase.ApplyClockAtMatchEnd());
        Assert.IsFalse(_itemPhase.ApplyClockCarryOver(CreateTable(HandShape.Rock, HandShape.Paper)));
    }

    // 첫 번째 판에 쓴 시계는 다음 판 턴만 유지하고, 다음 카드 셔플로 이월되지 않는다
    [Test]
    public void ClockOnFirstMatch_KeepsTurn_WithoutCarryOver()
    {
        CreateItemPhase(Capacity, ItemType.Clock);
        TableState table = StartItemPhase(HandShape.Scissors, HandShape.Paper);
        Assert.IsTrue(_itemPhase.UseItem(ItemType.Clock));
        _itemPhase.FinishPlayerPhase();

        StartNextMatchItemPhase(table);

        Assert.AreEqual(1, _phaseMachine.MatchIndex);
        Assert.AreEqual(Side.Player, table.TurnPlayer);
        Assert.IsFalse(_itemPhase.ApplyClockCarryOver(CreateTable(HandShape.Rock, HandShape.Paper)));
    }

    // 두 번째 판에 쓴 시계는 다음 카드 셔플로 이월되어 시계를 쓴 쪽이 첫 번째 판 턴 플레이어가 되고, 한 번 쓰면 사라진다.
    // 이월이 턴 플레이어를 실제로 바꾸는지 보려고, 새 테이블은 먼저 선 정하기로 딜러를 선으로 만들어 둔다
    [Test]
    public void ClockOnSecondMatch_CarriesOverOnce()
    {
        CreateItemPhase(Capacity, ItemType.Clock);
        TableState table = StartItemPhase(HandShape.Paper, HandShape.Scissors);
        Assert.AreEqual(Side.Dealer, table.TurnPlayer);
        _itemPhase.FinishPlayerPhase();

        StartNextMatchItemPhase(table);
        Assert.AreEqual(Side.Player, table.TurnPlayer);
        Assert.IsTrue(_itemPhase.UseItem(ItemType.Clock));
        _itemPhase.FinishPlayerPhase();
        _phaseMachine.CompleteReveal();
        _phaseMachine.CompleteJudge();

        Assert.IsTrue(_itemPhase.ApplyClockAtMatchEnd());

        TableState nextTable = CreateTable(HandShape.Paper, HandShape.Scissors);
        nextTable.DealStartCards();
        nextTable.DecideFirstTurnPlayer();
        Assert.AreEqual(Side.Dealer, nextTable.TurnPlayer);

        Assert.IsTrue(_itemPhase.ApplyClockCarryOver(nextTable));
        Assert.AreEqual(Side.Player, nextTable.TurnPlayer);
        Assert.IsFalse(_itemPhase.ApplyClockCarryOver(CreateTable(HandShape.Paper, HandShape.Scissors)));
    }

    // 지급 뒤 보유 아이템 로그는 아이템이 있는 라운드에서만 남긴다
    [Test]
    public void SupplyStartItems_LogsHeldItems_OnlyWhenRoundHasItems()
    {
        var round = new SerializedObject(_round);
        round.FindProperty("_startMagnifierCount").intValue = 1;
        round.ApplyModifiedPropertiesWithoutUndo();

        CreateItemPhase(0);
        _itemPhase.SupplyStartItems();
        Assert.AreEqual(0, _logs.Count);

        CreateItemPhase(Capacity);
        _itemPhase.SupplyStartItems();
        Assert.AreEqual(1, _logs.Count);
        StringAssert.Contains("라운드 시작 지급", _logs[0]);
    }
}
