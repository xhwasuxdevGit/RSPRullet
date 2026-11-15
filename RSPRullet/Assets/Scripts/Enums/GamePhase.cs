// 진행 단계 (코어 규칙 상세 기획서 2-1장 ①~⑦, 2-2장 5-1~5-7)
public enum GamePhase
{
    RoundStart,       // ① 라운드 시작
    CardShuffle,      // ② 카드 셔플
    StartCardDeal,    // ③ 시작 카드 배분
    DecideFirst,      // ④ 선 정하기
    Draw,             // ⑤ 5-1 뽑기
    Distribute,       // ⑤ 5-2 분배
    ItemTurnPlayer,   // ⑤ 5-3 아이템 사용(턴 플레이어). 아이템 이슈 전까지는 빈 단계
    ItemOpponent,     // ⑤ 5-4 아이템 사용(상대). 아이템 이슈 전까지는 빈 단계
    Reveal,           // ⑤ 5-5 카드 공개
    Judge,            // ⑤ 5-6 한 판의 판정
    ShowResult,       // ⑤ 5-7 단계별 표시
    Settlement,       // ⑥ 정산
    RoundEnd          // ⑦ 라운드 종료
}
