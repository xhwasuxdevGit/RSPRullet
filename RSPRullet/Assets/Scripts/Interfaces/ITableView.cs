using System.Collections.Generic;

// 화면이 읽는 테이블 상태 (GameFlow.Table)
public interface ITableView
{
    Side TurnPlayer { get; }

    // 턴 플레이어가 뽑아서 혼자 확인 중인 카드. 분배 전에만 값이 있다
    Card DrawnCard { get; }

    int RemainingDeckCount { get; }

    // 카드풀 정보의 "n장 → n장"
    int MatchStartDeckCount { get; }
    int MatchEndDeckCount { get; }

    // 이번 판 카드가 분배 직후와 다른 자리에 있는지
    bool IsMatchCardSwapped { get; }

    IReadOnlyList<Card> GetCombinationSlots(Side side);

    // 이번 카드 셔플의 카드풀 구성. 카드 셔플 동안 바뀌지 않는다
    int GetPoolCardCount(HandShape shape);
}
