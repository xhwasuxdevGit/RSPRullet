using NUnit.Framework;

// 아이템 세부 기획서 7-2장 I-F1 보유 칸, 4-1장 보유 아이템이 이어지는 범위, 5장 예외 I9 기준
public class ItemSlotsTests
{
    [Test]
    public void TryAdd_BelowCapacity_AddsItemAndReducesRemainingCount()
    {
        var slots = new ItemSlots(4);

        bool added = slots.TryAdd(ItemType.Coffee);

        Assert.IsTrue(added);
        CollectionAssert.AreEqual(new[] { ItemType.Coffee }, slots.Items);
        Assert.AreEqual(3, slots.RemainingCount);
    }

    // 3라운드에서 1칸 남았을 때 2개를 지급하면 먼저 뽑힌 1개만 받는다 (아이템 세부 기획서 6장 예시 2)
    [Test]
    public void TryAdd_WhenFull_RejectsItem()
    {
        var slots = new ItemSlots(1);
        slots.TryAdd(ItemType.Magnifier);

        bool added = slots.TryAdd(ItemType.Clock);

        Assert.IsFalse(added);
        CollectionAssert.AreEqual(new[] { ItemType.Magnifier }, slots.Items);
        Assert.AreEqual(0, slots.RemainingCount);
    }

    // 1라운드는 보유 한도가 0이라 아이템을 받지 않는다 (아이템 세부 기획서 4-2장 지급 시점)
    [Test]
    public void TryAdd_ZeroCapacity_RejectsItem()
    {
        var slots = new ItemSlots(0);

        Assert.IsFalse(slots.TryAdd(ItemType.Coffee));
        Assert.AreEqual(0, slots.Items.Count);
    }

    [Test]
    public void TryAdd_SameTypeTwice_KeepsBoth()
    {
        var slots = new ItemSlots(4);

        slots.TryAdd(ItemType.Clock);
        slots.TryAdd(ItemType.Clock);

        CollectionAssert.AreEqual(new[] { ItemType.Clock, ItemType.Clock }, slots.Items);
    }

    [Test]
    public void Remove_SameTypeHeldTwice_RemovesOnlyOne()
    {
        var slots = new ItemSlots(4);
        slots.TryAdd(ItemType.Clock);
        slots.TryAdd(ItemType.Clock);

        bool removed = slots.Remove(ItemType.Clock);

        Assert.IsTrue(removed);
        CollectionAssert.AreEqual(new[] { ItemType.Clock }, slots.Items);
    }

    [Test]
    public void Remove_NotHeld_ReturnsFalse()
    {
        var slots = new ItemSlots(4);
        slots.TryAdd(ItemType.Coffee);

        Assert.IsFalse(slots.Remove(ItemType.Magnifier));
        CollectionAssert.AreEqual(new[] { ItemType.Coffee }, slots.Items);
    }

    [Test]
    public void Clear_RemovesAllItems()
    {
        var slots = new ItemSlots(4);
        slots.TryAdd(ItemType.Coffee);
        slots.TryAdd(ItemType.Magnifier);

        slots.Clear();

        Assert.AreEqual(0, slots.Items.Count);
        Assert.AreEqual(4, slots.RemainingCount);
    }

    // 변경 알림은 실제로 바뀐 때만 온다: 추가 성공·제거 성공·비우기 각 1회, 거부·없는 아이템 제거·빈 칸 비우기는 0회
    [Test]
    public void Changed_RaisedOnlyWhenItemsActuallyChange()
    {
        var slots = new ItemSlots(1);
        int changedCount = 0;
        slots.Changed += () => changedCount++;

        slots.Clear();
        Assert.AreEqual(0, changedCount);

        slots.TryAdd(ItemType.Coffee);
        Assert.AreEqual(1, changedCount);

        slots.TryAdd(ItemType.Clock);
        Assert.AreEqual(1, changedCount);

        slots.Remove(ItemType.Clock);
        Assert.AreEqual(1, changedCount);

        slots.Remove(ItemType.Coffee);
        Assert.AreEqual(2, changedCount);

        slots.TryAdd(ItemType.Magnifier);
        slots.Clear();
        Assert.AreEqual(4, changedCount);
    }
}
