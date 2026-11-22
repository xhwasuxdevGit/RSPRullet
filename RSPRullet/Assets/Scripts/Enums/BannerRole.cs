// 진행 안내 배너의 역할. 역할마다 배경색과 글자색, 모양이 다르다 (RSP-86, BannerRules.GetRole)
public enum BannerRole
{
    Transition,     // 진행 전환: 카드 섞기·카드 정산
    FirstTurn,      // 선 정하기 결과: 선 결과·비김
    MatchResult,    // 판 결과: 판 승패·무승부·서든 데스
    Round           // 라운드: 라운드 시작·클리어·패배 (화면 3등분 레이어)
}
