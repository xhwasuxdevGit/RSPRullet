// 진행 단계별로 켤 버튼과 입력 잠금 여부를 정한다 (UI·UX 화면 흐름 세부 기획서 6-1장 단계별 버튼, 6-2장 입력 잠금, 8장 개발 명세 U3).
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
// 돋보기 선택의 바꾼다·그대로 둔다는 진행 단계가 아니라 아이템 단계 안의 대기(GameFlow.IsWaitingForMagnifierChoice)라서,
// 아이템 칸 UI와 사용 버튼 이슈(RSP-57)에서 그 값으로 버튼을 바꾸도록 추가한다.
public static class PhaseButtonRules
{
    // hasUsableItem: 플레이어에게 지금 쓸 수 있는 아이템이 있는지 (UI·UX 화면 흐름 세부 기획서 5-9장 버튼 전환)
    // playerWonRound, isFinalRound: 라운드 종료 화면(종료-01~04)을 고르는 값
    public static PhaseButton GetButtons(GamePhase phase, Side turnPlayer, bool hasUsableItem, bool playerWonRound, bool isFinalRound)
    {
        switch (phase)
        {
            case GamePhase.Draw:
                return turnPlayer == Side.Player ? PhaseButton.DrawCard : PhaseButton.None;
            case GamePhase.Distribute:
                return turnPlayer == Side.Player ? PhaseButton.Keep | PhaseButton.Give : PhaseButton.None;
            case GamePhase.ItemTurnPlayer:
                return GetItemButtons(turnPlayer, hasUsableItem);
            case GamePhase.ItemOpponent:
                return GetItemButtons(GetOpponent(turnPlayer), hasUsableItem);
            case GamePhase.Settlement:
                return PhaseButton.NextShuffle;
            case GamePhase.RoundEnd:
                return GetRoundEndButton(playerWonRound, isFinalRound);
            default:
                return PhaseButton.None;
        }
    }

    // 켤 단계별 버튼이 없으면 딜러 차례이거나 자동 진행 중이므로 입력을 잠근다. 잠금 중에는 규칙·설정 버튼도 잠근다
    public static bool IsInputLocked(PhaseButton buttons)
    {
        return buttons == PhaseButton.None;
    }

    private static PhaseButton GetItemButtons(Side actingSide, bool hasUsableItem)
    {
        if (actingSide != Side.Player)
        {
            return PhaseButton.None;
        }

        return hasUsableItem ? PhaseButton.ItemDone : PhaseButton.NoItem;
    }

    // 3라운드는 이겨도 져도 클리어·게임 오버 팝업의 다시하기만 둔다 (UI·UX 화면 흐름 세부 기획서 3-1장 종료-03·04)
    private static PhaseButton GetRoundEndButton(bool playerWonRound, bool isFinalRound)
    {
        if (isFinalRound)
        {
            return PhaseButton.Restart;
        }

        return playerWonRound ? PhaseButton.NextRound : PhaseButton.TakeMedicine;
    }

    private static Side GetOpponent(Side side)
    {
        return side == Side.Player ? Side.Dealer : Side.Player;
    }
}
