using System.Collections.Generic;

// 화면이 읽는 아이템 상태 (GameFlow.Items). 아이템 입력은 GameFlow.UseItem으로 넘긴다
public interface IItemView
{
    // 양쪽 보유 칸 수. 0이면 아이템이 없는 라운드다
    int Capacity { get; }

    // 이번 판에 양쪽이 쓴 아이템과 돋보기 뒤 교환 여부
    IReadOnlyList<(Side User, ItemType Item, bool Exchanged)> ItemUses { get; }

    IReadOnlyList<ItemType> GetItems(Side side);

    // 이번 판에 user가 item을 썼는지. 시계는 턴 플레이어만 쓰므로 HasUsedItem(턴 플레이어, 시계)로 시계를 쓴 쪽을 알 수 있다
    bool HasUsedItem(Side user, ItemType item);
}
