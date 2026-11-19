// 카드 앞·뒷면, 눈 아이콘·"상대가 알고 있음" 문구, 교환 표시를 켤지 정한다
// (UI·UX 화면 흐름 세부 기획서 4-1장 기본 원칙, 4-2장 표시 범위, 4-4장 교환 표시, 8장 개발 명세 U2).
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class CardFaceRules
{
    // 앞·뒷면의 기준은 플레이어다. 플레이어가 아는 카드면 앞면, 모르면 뒷면
    public static bool ShowsFront(Card card)
    {
        return card.KnownByPlayer;
    }

    // 딜러가 아는 카드에 붙이되, 딜러가 아는지는 승패 판정 전에만 의미가 있어 판정 뒤에는 붙이지 않는다
    public static bool ShowsKnownMarker(Card card, GamePhase phase)
    {
        return card.KnownByDealer && IsBeforeJudgment(phase);
    }

    // 교환 표시도 승패 판정 때 지운다. 판정 뒤에는 카드 내용이 보이므로 필요 없다.
    // 실제로 교환됐는지는 돋보기 기능(RSP-54)에서 분배 직후 위치와 비교해 넘긴다
    public static bool ShowsSwapMarker(bool isSwapped, GamePhase phase)
    {
        return isSwapped && IsBeforeJudgment(phase);
    }

    // 카드 슬롯에 이번 판 카드가 놓여 있는 단계: 분배가 끝난 뒤부터 승패 판정 표시(판-07)까지.
    // 이 동안 슬롯 카드는 각자 카드 조합 칸의 마지막 카드다 (TableState.Distribute)
    public static bool HasSlotCards(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.ItemTurnPlayer:
            case GamePhase.ItemOpponent:
            case GamePhase.Reveal:
            case GamePhase.Judge:
            case GamePhase.ShowResult:
                return true;
            default:
                return false;
        }
    }

    // 뽑기부터 카드 공개까지가 승패 판정 전이다. 판정(5-6) 이후와 판 밖의 단계는 판정 전이 아니다
    private static bool IsBeforeJudgment(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Draw:
            case GamePhase.Distribute:
            case GamePhase.ItemTurnPlayer:
            case GamePhase.ItemOpponent:
            case GamePhase.Reveal:
                return true;
            default:
                return false;
        }
    }
}
