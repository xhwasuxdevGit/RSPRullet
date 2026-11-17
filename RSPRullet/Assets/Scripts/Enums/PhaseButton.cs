using System;

// 하단 HUD의 단계별 버튼 (UI·UX 화면 흐름 세부 기획서 6-1장). 한 화면 상태에 버튼이 여러 개 켜질 수 있어 비트 조합으로 둔다
[Flags]
public enum PhaseButton
{
    None = 0,
    DrawCard = 1 << 0,      // 판-01 카드 뽑기
    Keep = 1 << 1,          // 판-02 가진다
    Give = 1 << 2,          // 판-02 넘겨준다
    ItemDone = 1 << 3,      // 판-04 아이템 사용 완료
    NoItem = 1 << 4,        // 판-04 아이템 없음 (쓸 수 있는 아이템이 0개일 때)
    NextShuffle = 1 << 5,   // 정산-02 다음 카드 셔플
    NextRound = 1 << 6,     // 종료-01 다음 라운드
    TakeMedicine = 1 << 7,  // 종료-02 응급약 복용
    Restart = 1 << 8        // 종료-03·04 다시하기
}
