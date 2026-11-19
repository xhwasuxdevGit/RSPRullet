using System;

// 아이템 보유 칸 상태는 ItemState가 가지고, 이 클래스는 무엇을 몇 개 넣을지만 정해 넣는다.
// 언제 지급할지(라운드 시작, 카드 셔플, 선 정하기 무승부 때는 지급하지 않음)는 진행 흐름(GameFlow)이 정한다
public static class ItemSupply
{
    // 플레이어 → 딜러 순서로 지급한다. 같은 시드면 같은 순서로 난수를 써서 결과가 같다
    private static readonly Side[] SupplyOrder = { Side.Player, Side.Dealer };

    // 라운드 시작 지급: 양쪽에 돋보기를 magnifierCount개씩 넣는다 (아이템 세부 기획서 4-2장 지급 시점)
    public static void SupplyStartItems(ItemState items, int magnifierCount)
    {
        foreach (Side side in SupplyOrder)
        {
            for (int i = 0; i < magnifierCount; i++)
            {
                if (!items.TryAddItem(side, ItemType.Magnifier))
                {
                    break;
                }
            }
        }
    }

    // 카드 셔플 지급: 플레이어와 딜러가 따로 1개씩 뽑아 넣고, 칸이 꽉 차면 그쪽의 남은 지급은 버린다 (아이템 세부 기획서 4-3장 무작위 지급)
    public static void SupplyShuffleItems(ItemState items, int itemsPerShuffle, ItemSettings settings, Random random)
    {
        foreach (Side side in SupplyOrder)
        {
            for (int i = 0; i < itemsPerShuffle; i++)
            {
                if (!items.TryAddItem(side, PickRandomItem(settings, random)))
                {
                    break;
                }
            }
        }
    }

    // 지급 가중치를 비율로 해석해 아이템 종류 하나를 뽑는다. 가중치가 0인 종류는 뽑히지 않는다
    public static ItemType PickRandomItem(ItemSettings settings, Random random)
    {
        int totalWeight = 0;
        foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
        {
            totalWeight += settings.GetWeight(item);
        }

        if (totalWeight <= 0)
        {
            throw new InvalidOperationException("[ItemSupply] 아이템 지급 가중치의 합이 0입니다. ItemSettings 에셋 값을 확인하세요");
        }

        // 0 ~ 합-1 사이 값이 어느 종류의 구간에 들어가는지로 고른다
        int roll = random.Next(totalWeight);
        foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
        {
            int weight = settings.GetWeight(item);
            if (roll < weight)
            {
                return item;
            }

            roll -= weight;
        }

        // 위 반복에서 반드시 고르므로 여기에는 오지 않는다
        throw new InvalidOperationException("[ItemSupply] 아이템 종류를 고르지 못했습니다");
    }
}
