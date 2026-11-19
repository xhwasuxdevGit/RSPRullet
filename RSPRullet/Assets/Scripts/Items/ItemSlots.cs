using System;
using System.Collections.Generic;

// 한쪽 참가자의 아이템 보유 칸 (아이템 세부 기획서 7-2장 I-F1 보유 칸). 플레이어와 딜러가 하나씩 가진다.
// 보유 아이템은 항상 공개되므로(아이템 세부 기획서 3-4장 공개되는 정보) 목록을 그대로 조회할 수 있다
public class ItemSlots
{
    private readonly List<ItemType> _items = new List<ItemType>();

    // 아이템이 추가·제거되거나 칸을 비울 때 알린다. 실제로 바뀌지 않으면 알리지 않는다
    public event Action Changed;

    public int Capacity { get; }
    public IReadOnlyList<ItemType> Items => _items;
    public int RemainingCount => Capacity - _items.Count;

    // capacity는 라운드 설정(RoundSettings.ItemCapacity)에서 읽은 보유 한도
    public ItemSlots(int capacity)
    {
        Capacity = capacity;
    }

    // 칸이 꽉 차면 받지 않고 false를 돌려준다 (아이템 세부 기획서 5장 예외 I9). 남은 지급을 버리는 것은 지급하는 쪽이 맡는다
    public bool TryAdd(ItemType item)
    {
        if (RemainingCount <= 0)
        {
            return false;
        }

        _items.Add(item);
        Changed?.Invoke();
        return true;
    }

    // 같은 종류가 여러 개면 하나만 뺀다. 가지고 있지 않으면 false를 돌려준다
    public bool Remove(ItemType item)
    {
        if (!_items.Remove(item))
        {
            return false;
        }

        Changed?.Invoke();
        return true;
    }

    // 다음 라운드나 응급약 복용(재도전)으로 보유 칸을 비운다 (아이템 세부 기획서 4-1장 보유 아이템이 이어지는 범위)
    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        Changed?.Invoke();
    }
}
