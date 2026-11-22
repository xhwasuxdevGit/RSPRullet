// 화면 가운데 진행 안내 배너의 종류 (RSP-86).
// 전환 배너는 단계가 바뀔 때 그 단계에서 가장 먼저 나오고, 결과 배너는 결과가 드러난 직후에 나온다 (BannerRules.IsResult)
public enum BannerType
{
    RoundStart,         // 라운드: n라운드
    ShuffleStart,       // 전환: n번째 카드 섞기 (라운드의 첫 셔플과 선 비김 재섞기에는 띄우지 않는다)
    SettlementStart,    // 전환: 카드 정산 (두 번째 판 결과 표시 뒤, 정산 결과를 반영하기 전)
    PlayerFirst,        // 결과: 당신이 선입니다 (시계 이월로 정해졌을 때도 띄운다)
    DealerFirst,        // 결과: 딜러가 선입니다 (시계 이월로 정해졌을 때도 띄운다)
    FirstTurnDraw,      // 결과: 비겼습니다. 다시 섞습니다
    MatchWin,           // 결과: 당신 승리
    MatchLose,          // 결과: 딜러 승리
    MatchDraw,          // 결과: 무승부
    SuddenDeath,        // 결과: 서든 데스 / 체력 1 단판 승부 (정산에서 양쪽 체력 0 → 둘 다 1로 되돌린 뒤)
    RoundWin,           // 라운드: n라운드 클리어 (마지막 라운드는 띄우지 않는다)
    RoundLose           // 라운드: n라운드 패배 (마지막 라운드는 띄우지 않는다)
}
