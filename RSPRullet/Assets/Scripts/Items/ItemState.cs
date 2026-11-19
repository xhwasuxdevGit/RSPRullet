using System;
using System.Collections.Generic;

// 양쪽 보유 칸과 이번 판의 아이템 사용 기록.
// "쓸 수 있는 아이템" 판정에 보유 칸과 사용 기록이 모두 필요해 한 클래스에 둔다.
// 사용 기록(어떤 아이템을 썼는지, 돋보기 뒤 교환했는지)은 양쪽에 공개되는 정보다
public class ItemState
{
    private readonly ItemSlots _playerSlots;
    private readonly ItemSlots _dealerSlots;
    private readonly List<(Side User, ItemType Item, bool Exchanged)> _itemUses = new List<(Side User, ItemType Item, bool Exchanged)>();

    // 어느 쪽이든 보유 아이템이 실제로 바뀌면 알린다
    public event Action Changed;

    // 양쪽 보유 한도는 같다. 0이면 아이템이 없는 라운드다
    public int Capacity => _playerSlots.Capacity;

    // 이번 판에 쓴 순서대로의 기록. Exchanged는 돋보기를 쓴 뒤 교환했는지이며, 다른 아이템은 항상 false다
    public IReadOnlyList<(Side User, ItemType Item, bool Exchanged)> ItemUses => _itemUses;

    public ItemState(int capacity)
    {
        _playerSlots = new ItemSlots(capacity);
        _dealerSlots = new ItemSlots(capacity);

        // 보유 칸은 이 클래스가 만들어 함께 사라지므로 구독을 해제하지 않는다
        _playerSlots.Changed += OnSlotsChanged;
        _dealerSlots.Changed += OnSlotsChanged;
    }

    public IReadOnlyList<ItemType> GetItems(Side side)
    {
        return GetSlots(side).Items;
    }

    // 칸이 꽉 차면 받지 않고 false를 돌려준다
    public bool TryAddItem(Side side, ItemType item)
    {
        return GetSlots(side).TryAdd(item);
    }

    public bool HasUsedItem(Side user, ItemType item)
    {
        foreach ((Side User, ItemType Item, bool Exchanged) use in _itemUses)
        {
            if (use.User == user && use.Item == item)
            {
                return true;
            }
        }

        return false;
    }

    // 쓸 수 있는 아이템: 보유 칸에 있고, 이번 판에 같은 종류를 아직 쓰지 않았고, 시계라면 이번 판 턴 플레이어다.
    // 턴 플레이어는 한 판 동안 바뀌지 않으므로 시계는 양쪽 합쳐 한 판에 1번만 쓰인다
    public bool CanUseItem(Side user, ItemType item, Side turnPlayer)
    {
        if (!IsHeld(GetItems(user), item) || HasUsedItem(user, item))
        {
            return false;
        }

        return item != ItemType.Clock || user == turnPlayer;
    }

    // [아이템 사용 완료]·[아이템 없음] 버튼 전환과 딜러 단계 생략에 쓴다
    public bool HasUsableItem(Side user, Side turnPlayer)
    {
        foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
        {
            if (CanUseItem(user, item, turnPlayer))
            {
                return true;
            }
        }

        return false;
    }

    // 쓸 수 있으면 보유 칸에서 빼고 기록한다. 효과는 상태를 가진 곳(체력 상태, 셔플 테이블)이 맡는다
    public bool TryUseItem(Side user, ItemType item, Side turnPlayer)
    {
        if (!CanUseItem(user, item, turnPlayer))
        {
            return false;
        }

        GetSlots(user).Remove(item);
        _itemUses.Add((user, item, false));
        return true;
    }

    // 돋보기를 쓴 뒤 교환을 고르면 그 사용 기록에 교환을 남긴다. 이번 판에 돋보기를 쓰지 않았으면 false를 돌려준다
    public bool MarkExchanged(Side user)
    {
        for (int i = 0; i < _itemUses.Count; i++)
        {
            if (_itemUses[i].User == user && _itemUses[i].Item == ItemType.Magnifier)
            {
                _itemUses[i] = (user, ItemType.Magnifier, true);
                return true;
            }
        }

        return false;
    }

    // 같은 종류는 한 판에 한 번이므로 판이 시작될 때 양쪽 기록을 비운다
    public void ClearItemUses()
    {
        _itemUses.Clear();
    }

    private ItemSlots GetSlots(Side side)
    {
        return side == Side.Player ? _playerSlots : _dealerSlots;
    }

    private void OnSlotsChanged()
    {
        Changed?.Invoke();
    }

    // 보유 아이템 목록에 그 종류가 하나라도 있는지 확인한다.
    private static bool IsHeld(IReadOnlyList<ItemType> items, ItemType item)
    {
        foreach (ItemType heldItem in items)
        {
            if (heldItem == item)
            {
                return true;
            }
        }

        return false;
    }
}
