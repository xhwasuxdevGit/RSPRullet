using UnityEngine;

// 딜러 AI (딜러 AI 세부 기획서). 딜러의 판단을 이 클래스 한 곳에 모은다 (딜러 AI 세부 기획서 2장 공통 원칙).
// 지금은 카드 분배 판단(딜러 AI 세부 기획서 7-2장 D1)만 있다. 아이템·교환 판단은 이후 이슈에서 이 클래스에 더한다.
// 딜러는 선택만 하고, 카드를 어느 자리에 놓는지는 셔플 테이블이 처리한다.
public class DealerAI
{
    private readonly System.Random _random;

    // 같은 시드의 난수를 넘기면 같은 선택이 나오므로 테스트에서 상황을 재현할 수 있다
    public DealerAI(System.Random random)
    {
        _random = random;
    }

    // 딜러가 턴 플레이어이고 뽑은 카드가 있을 때만 분배한다. 그 외에는 아무것도 하지 않고 false를 돌려준다.
    // 판단에는 테이블 대신 딜러가 아는 정보(DealerKnownInfo)만 넘긴다
    public bool TryDistribute(ShuffleTable table, int matchIndex)
    {
        if (table.TurnPlayer != Side.Dealer || table.DrawnCard == null)
        {
            Debug.Log("[DealerAI] 딜러가 분배할 차례가 아니라 무시");
            return false;
        }

        bool keep = ChooseKeep(new DealerKnownInfo(table, matchIndex));
        Debug.Log($"[DealerAI] 분배: {(keep ? "가진다" : "준다")}");
        table.Distribute(keep);
        return true;
    }

    // 카드 분배 판단 (딜러 AI 세부 기획서 3장). 딜러가 아는 정보만 받아 가진다(true) / 준다(false)를 돌려준다.
    // ② 이번 판 판단(RSP-46), ① 조합 판단(RSP-45), ③ 충돌 해결과 무작위 단계(RSP-47)가 들어오기 전까지는 50:50으로 고른다
    private bool ChooseKeep(DealerKnownInfo knownInfo)
    {
        return _random.Next(2) == 0;
    }
}
