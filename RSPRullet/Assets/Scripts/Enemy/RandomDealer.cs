using UnityEngine;

// S1 임시 딜러 (컨셉 기획서 7장). 딜러가 턴 플레이어인 판에서 가진다 / 준다를 무작위로 고른다.
// 가진다 / 준다 판단은 ChooseKeep 한 곳에만 두어, S2에서 규칙 기반 딜러로 이 부분만 바꿀 수 있게 한다.
public class RandomDealer
{
    private readonly System.Random _random;

    // 같은 시드의 난수를 넘기면 같은 선택이 나오므로 테스트에서 상황을 재현할 수 있다
    public RandomDealer(System.Random random)
    {
        _random = random;
    }

    // 딜러가 턴 플레이어이고 뽑은 카드가 있을 때만 분배한다. 그 외에는 아무것도 하지 않고 false를 돌려준다
    public bool TryDistribute(ShuffleTable table)
    {
        if (table.TurnPlayer != Side.Dealer || table.DrawnCard == null)
        {
            Debug.Log("[RandomDealer] 딜러가 분배할 차례가 아니라 무시");
            return false;
        }

        bool keep = ChooseKeep();
        Debug.Log($"[RandomDealer] 분배: {(keep ? "가진다" : "준다")}");
        table.Distribute(keep);
        return true;
    }

    // 딜러의 분배 판단. S2에서는 딜러가 아는 정보(ShuffleTable.GetKnownCards 등)를 받아 판단하도록 바꾼다
    private bool ChooseKeep()
    {
        return _random.Next(2) == 0;
    }
}
