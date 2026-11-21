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

    // 교환 표시는 두 카드 슬롯 사이에 붙으므로 슬롯에 이번 판 카드가 있을 때만 켜고, 승패 판정 때 지운다. 판정 뒤에는 카드 내용이 보이므로 필요 없다.
    // isSwapped는 분배 직후와 지금 카드 위치를 비교한 값(GameFlow.Table.IsMatchCardSwapped)이다. 두 번 교환해 원래 자리로 돌아오면 false다.
    // 이 값은 다음 분배 때 다시 정해지므로 다음 판 뽑기·분배 중에는 앞 판 결과가 남아 있다. 그래서 슬롯 카드가 없는 뽑기·분배에서는 켜지 않는다
    public static bool ShowsSwapMarker(bool isSwapped, GamePhase phase)
    {
        return isSwapped && HasSlotCards(phase) && IsBeforeJudgment(phase);
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
