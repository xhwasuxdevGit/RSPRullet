using NUnit.Framework;

// 쓸 수 있는 아이템 판정, 아이템 사용과 공개 기록, 판마다 기록 초기화
public class ItemStateTests
{
    private const int Capacity = 6;

    private static ItemState CreateState(Side side, params ItemType[] items)
    {
        var state = new ItemState(Capacity);
        foreach (ItemType item in items)
        {
            state.TryAddItem(side, item);
        }

        return state;
    }

    [Test]
    public void TryAddItem_AddsOnlyToThatSide()
    {
        var state = new ItemState(Capacity);

        Assert.IsTrue(state.TryAddItem(Side.Dealer, ItemType.Coffee));

        CollectionAssert.AreEqual(new[] { ItemType.Coffee }, state.GetItems(Side.Dealer));
        Assert.AreEqual(0, state.GetItems(Side.Player).Count);
    }

    [Test]
    public void TryAddItem_ZeroCapacity_Rejects()
    {
        var state = new ItemState(0);

        Assert.IsFalse(state.TryAddItem(Side.Player, ItemType.Coffee));
        Assert.AreEqual(0, state.Capacity);
    }

    // 보유 아이템이 실제로 바뀔 때만 알린다
    [Test]
    public void Changed_ComesOnlyWhenItemsChange()
    {
        var state = new ItemState(1);
        int count = 0;
        state.Changed += () => count++;

        state.TryAddItem(Side.Player, ItemType.Coffee);
        state.TryAddItem(Side.Player, ItemType.Coffee);
        state.TryUseItem(Side.Player, ItemType.Coffee, Side.Player);
        state.TryUseItem(Side.Player, ItemType.Coffee, Side.Player);

        Assert.AreEqual(2, count);
    }

    [Test]
    public void CanUseItem_HeldAndNotUsed_ReturnsTrue()
    {
        ItemState state = CreateState(Side.Player, ItemType.Coffee);

        Assert.IsTrue(state.CanUseItem(Side.Player, ItemType.Coffee, Side.Dealer));
    }

    [Test]
    public void CanUseItem_NotHeld_ReturnsFalse()
    {
        ItemState state = CreateState(Side.Player, ItemType.Coffee);

        Assert.IsFalse(state.CanUseItem(Side.Player, ItemType.Magnifier, Side.Player));
    }

    // 같은 종류를 2개 가지고 있어도 한 판에 1번만 쓴다
    [Test]
    public void CanUseItem_SameTypeAlreadyUsedThisMatch_ReturnsFalse()
    {
        ItemState state = CreateState(Side.Player, ItemType.Coffee, ItemType.Coffee);
        state.TryUseItem(Side.Player, ItemType.Coffee, Side.Player);

        Assert.IsFalse(state.CanUseItem(Side.Player, ItemType.Coffee, Side.Player));
    }

    // 한 사람당 1번이므로 상대가 같은 종류를 쓴 것은 막지 않는다
    [Test]
    public void CanUseItem_OpponentUsedSameType_ReturnsTrue()
    {
        ItemState state = CreateState(Side.Player, ItemType.Coffee);
        state.TryAddItem(Side.Dealer, ItemType.Coffee);
        state.TryUseItem(Side.Player, ItemType.Coffee, Side.Player);

        Assert.IsTrue(state.CanUseItem(Side.Dealer, ItemType.Coffee, Side.Player));
    }

    [Test]
    public void CanUseItem_ClockAsTurnPlayer_ReturnsTrue()
    {
        ItemState state = CreateState(Side.Dealer, ItemType.Clock);

        Assert.IsTrue(state.CanUseItem(Side.Dealer, ItemType.Clock, Side.Dealer));
    }

    // 상대 단계에서는 시계를 쓸 수 없다
    [Test]
    public void CanUseItem_ClockAsOpponent_ReturnsFalse()
    {
        ItemState state = CreateState(Side.Player, ItemType.Clock);

        Assert.IsFalse(state.CanUseItem(Side.Player, ItemType.Clock, Side.Dealer));
    }

    // 턴 플레이어가 시계를 쓰면 같은 판에서 양쪽 모두 더 쓸 수 없다
    [Test]
    public void CanUseItem_ClockAfterTurnPlayerUsedClock_FalseForBothSides()
    {
        ItemState state = CreateState(Side.Player, ItemType.Clock, ItemType.Clock);
        state.TryAddItem(Side.Dealer, ItemType.Clock);
        state.TryUseItem(Side.Player, ItemType.Clock, Side.Player);

        Assert.IsFalse(state.CanUseItem(Side.Player, ItemType.Clock, Side.Player));
        Assert.IsFalse(state.CanUseItem(Side.Dealer, ItemType.Clock, Side.Player));
    }

    // 쓸 수 없는 시계는 "쓸 수 있는 아이템이 없음"을 판단할 때 세지 않는다
    [Test]
    public void HasUsableItem_OnlyClockAsOpponent_ReturnsFalse()
    {
        ItemState state = CreateState(Side.Player, ItemType.Clock);

        Assert.IsFalse(state.HasUsableItem(Side.Player, Side.Dealer));
    }

    [Test]
    public void HasUsableItem_NoItems_ReturnsFalse()
    {
        var state = new ItemState(Capacity);

        Assert.IsFalse(state.HasUsableItem(Side.Player, Side.Player));
    }

    // 아이템을 써서 쓸 수 있는 아이템이 0개가 되면 없음으로 바뀐다
    [Test]
    public void HasUsableItem_AfterUsingLastUsableItem_ReturnsFalse()
    {
        ItemState state = CreateState(Side.Player, ItemType.Magnifier, ItemType.Magnifier);
        Assert.IsTrue(state.HasUsableItem(Side.Player, Side.Player));

        state.TryUseItem(Side.Player, ItemType.Magnifier, Side.Player);

        Assert.IsFalse(state.HasUsableItem(Side.Player, Side.Player));
    }

    [Test]
    public void TryUseItem_Usable_RemovesFromSlotsAndRecordsUse()
    {
        ItemState state = CreateState(Side.Dealer, ItemType.Coffee, ItemType.Magnifier);

        bool used = state.TryUseItem(Side.Dealer, ItemType.Magnifier, Side.Player);

        Assert.IsTrue(used);
        CollectionAssert.AreEqual(new[] { ItemType.Coffee }, state.GetItems(Side.Dealer));
        Assert.AreEqual(1, state.ItemUses.Count);
        Assert.AreEqual((Side.Dealer, ItemType.Magnifier, false), state.ItemUses[0]);
    }

    [Test]
    public void TryUseItem_NotUsable_KeepsSlotsAndRecord()
    {
        ItemState state = CreateState(Side.Player, ItemType.Clock);

        bool used = state.TryUseItem(Side.Player, ItemType.Clock, Side.Dealer);

        Assert.IsFalse(used);
        CollectionAssert.AreEqual(new[] { ItemType.Clock }, state.GetItems(Side.Player));
        Assert.AreEqual(0, state.ItemUses.Count);
    }

    // 돋보기 뒤 교환 여부는 공개 정보로 남는다
    [Test]
    public void MarkExchanged_AfterMagnifier_RecordsExchange()
    {
        ItemState state = CreateState(Side.Player, ItemType.Coffee, ItemType.Magnifier);
        state.TryUseItem(Side.Player, ItemType.Coffee, Side.Player);
        state.TryUseItem(Side.Player, ItemType.Magnifier, Side.Player);

        bool marked = state.MarkExchanged(Side.Player);

        Assert.IsTrue(marked);
        Assert.IsFalse(state.ItemUses[0].Exchanged);
        Assert.IsTrue(state.ItemUses[1].Exchanged);
    }

    [Test]
    public void MarkExchanged_WithoutMagnifier_ReturnsFalse()
    {
        ItemState state = CreateState(Side.Player, ItemType.Coffee);
        state.TryUseItem(Side.Player, ItemType.Coffee, Side.Player);

        Assert.IsFalse(state.MarkExchanged(Side.Player));
        Assert.IsFalse(state.ItemUses[0].Exchanged);
    }

    // 판이 바뀌면 기록을 비워 같은 종류를 다시 쓸 수 있다
    [Test]
    public void ClearItemUses_AfterUse_EmptiesRecordAndAllowsSameTypeAgain()
    {
        ItemState state = CreateState(Side.Player, ItemType.Coffee, ItemType.Coffee);
        state.TryUseItem(Side.Player, ItemType.Coffee, Side.Player);

        state.ClearItemUses();

        Assert.AreEqual(0, state.ItemUses.Count);
        Assert.IsTrue(state.CanUseItem(Side.Player, ItemType.Coffee, Side.Player));
    }
}
