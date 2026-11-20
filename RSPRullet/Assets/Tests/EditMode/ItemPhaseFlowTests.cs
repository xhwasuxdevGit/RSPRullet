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
        _logs.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_round);
        Object.DestroyImmediate(_itemSettings);
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

        _itemPhase = new ItemPhaseFlow(_phaseMachine, _items, _round, _itemSettings, new System.Random(0));
        _itemPhase.Logged += _logs.Add;
    }

    // 시작 카드(플레이어 firstPlayerShape, 딜러 firstDealerShape)로 선을 정하고, 턴 플레이어가 뽑아 가진 뒤 아이템 단계를 시작한다
    private void StartItemPhase(HandShape firstPlayerShape, HandShape firstDealerShape)
    {
        var cards = new[] { firstPlayerShape, firstDealerShape, HandShape.Rock, HandShape.Scissors, HandShape.Paper, HandShape.Rock, HandShape.Scissors }
            .Select(shape => new Card(shape));
        var table = new TableState(new CardDeck(cards, new KeepOrderRandom()));

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

    // 플레이어가 턴 플레이어면 턴 플레이어 단계에서 입력을 기다리고, 끝내면 딜러 단계는 바로 지나 끝난다
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
        CollectionAssert.Contains(_logs, "딜러가 아이템 단계를 끝냈다");
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
