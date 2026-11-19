using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// 아이템 세부 기획서 4장 지급 규칙, 6장 예시 2, 7-2장 I-F2 아이템 지급 기준
public class ItemSupplyTests
{
    private const int SeedCount = 50;

    private ItemSettings _settings;

    [SetUp]
    public void SetUp()
    {
        _settings = ScriptableObject.CreateInstance<ItemSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_settings);
    }

    private void SetWeights(int coffee, int magnifier, int clock)
    {
        var serialized = new SerializedObject(_settings);
        serialized.FindProperty("_coffeeWeight").intValue = coffee;
        serialized.FindProperty("_magnifierWeight").intValue = magnifier;
        serialized.FindProperty("_clockWeight").intValue = clock;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // 2·3라운드 라운드 시작: 각자 돋보기 1개 (아이템 세부 기획서 4-2장 지급 시점)
    [Test]
    public void SupplyStartItems_GivesMagnifiersToBothSides()
    {
        var items = new ItemState(4);

        ItemSupply.SupplyStartItems(items, 1);

        CollectionAssert.AreEqual(new[] { ItemType.Magnifier }, items.GetItems(Side.Player));
        CollectionAssert.AreEqual(new[] { ItemType.Magnifier }, items.GetItems(Side.Dealer));
    }

    // 1라운드는 지급 개수와 보유 한도가 모두 0이라 받지 않는다
    [Test]
    public void SupplyStartItems_ZeroCount_GivesNothing()
    {
        var items = new ItemState(0);

        ItemSupply.SupplyStartItems(items, 0);

        Assert.AreEqual(0, items.GetItems(Side.Player).Count);
        Assert.AreEqual(0, items.GetItems(Side.Dealer).Count);
    }

    // 3라운드 셔플 지급: 각자 2개씩
    [Test]
    public void SupplyShuffleItems_GivesCountItemsToEachSide()
    {
        var items = new ItemState(6);

        ItemSupply.SupplyShuffleItems(items, 2, _settings, new System.Random(0));

        Assert.AreEqual(2, items.GetItems(Side.Player).Count);
        Assert.AreEqual(2, items.GetItems(Side.Dealer).Count);
    }

    // 3라운드에서 1칸 남았을 때 2개를 지급하면 먼저 뽑힌 1개만 받는다 (아이템 세부 기획서 6장 예시 2).
    // 플레이어가 먼저 받으므로, 같은 시드의 첫 번째 뽑기 결과가 플레이어의 마지막 아이템이 된다
    [Test]
    public void SupplyShuffleItems_OneSlotLeft_KeepsOnlyFirstPicked()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var items = new ItemState(6);
            for (int i = 0; i < 5; i++)
            {
                items.TryAddItem(Side.Player, ItemType.Coffee);
            }
            ItemType firstPicked = ItemSupply.PickRandomItem(_settings, new System.Random(seed));

            ItemSupply.SupplyShuffleItems(items, 2, _settings, new System.Random(seed));

            IReadOnlyList<ItemType> playerItems = items.GetItems(Side.Player);
            Assert.AreEqual(6, playerItems.Count, $"seed {seed}");
            Assert.AreEqual(firstPicked, playerItems[5], $"seed {seed}");
            Assert.AreEqual(2, items.GetItems(Side.Dealer).Count, $"seed {seed}");
        }
    }

    // 같은 난수 시드면 양쪽이 같은 아이템을 같은 순서로 받는다 (테스트 재현용)
    [Test]
    public void SupplyShuffleItems_SameSeed_SameResult()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var first = new ItemState(6);
            var second = new ItemState(6);

            ItemSupply.SupplyShuffleItems(first, 2, _settings, new System.Random(seed));
            ItemSupply.SupplyShuffleItems(second, 2, _settings, new System.Random(seed));

            CollectionAssert.AreEqual(first.GetItems(Side.Player), second.GetItems(Side.Player), $"seed {seed}");
            CollectionAssert.AreEqual(first.GetItems(Side.Dealer), second.GetItems(Side.Dealer), $"seed {seed}");
        }
    }

    // 가중치가 0인 종류는 뽑히지 않는다
    [Test]
    public void PickRandomItem_ZeroWeight_NeverPicked()
    {
        SetWeights(coffee: 50, magnifier: 50, clock: 0);
        var random = new System.Random(0);

        for (int i = 0; i < 200; i++)
        {
            Assert.AreNotEqual(ItemType.Clock, ItemSupply.PickRandomItem(_settings, random));
        }
    }

    // 한 종류만 가중치가 있으면 항상 그 종류가 뽑힌다
    [TestCase(ItemType.Coffee, 1, 0, 0)]
    [TestCase(ItemType.Magnifier, 0, 1, 0)]
    [TestCase(ItemType.Clock, 0, 0, 1)]
    public void PickRandomItem_OnlyOneWeighted_AlwaysPicksIt(ItemType expected, int coffee, int magnifier, int clock)
    {
        SetWeights(coffee, magnifier, clock);
        var random = new System.Random(0);

        for (int i = 0; i < 50; i++)
        {
            Assert.AreEqual(expected, ItemSupply.PickRandomItem(_settings, random));
        }
    }

    // 기본 가중치(커피 40 / 돋보기 40 / 시계 20)로 많이 뽑으면 세 종류가 모두 나온다
    [Test]
    public void PickRandomItem_DefaultWeights_PicksAllTypes()
    {
        var random = new System.Random(0);
        var picked = new HashSet<ItemType>();

        for (int i = 0; i < 200; i++)
        {
            picked.Add(ItemSupply.PickRandomItem(_settings, random));
        }

        Assert.AreEqual(3, picked.Count);
    }
}
