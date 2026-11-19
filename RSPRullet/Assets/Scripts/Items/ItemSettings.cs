using UnityEngine;

// 아이템 지급 가중치 값 (아이템 세부 기획서 7-1-1장). 인스펙터에서 조절하며, 에셋은 하나만 만든다.
// 아이템 구분은 ItemType, 이름은 DisplayNames가 맡고, 이 에셋은 조절할 값만 가진다
[CreateAssetMenu(fileName = "ItemSettings", menuName = "RSPRullet/Item Settings")]
public class ItemSettings : ScriptableObject
{
    // 무작위 지급에서 종류별로 뽑힐 비율 (아이템 세부 기획서 4-3장 무작위 지급). 합이 100일 필요는 없고 비율로 해석한다.
    // 시계는 연속 사용으로 한쪽이 계속 턴 플레이어로 남지 않도록 낮게 둔다
    [SerializeField, Min(0)] private int _coffeeWeight = 40;
    [SerializeField, Min(0)] private int _magnifierWeight = 40;
    [SerializeField, Min(0)] private int _clockWeight = 20;

    public int GetWeight(ItemType item)
    {
        switch (item)
        {
            case ItemType.Coffee:
                return _coffeeWeight;
            case ItemType.Magnifier:
                return _magnifierWeight;
            default:
                return _clockWeight;
        }
    }
}
