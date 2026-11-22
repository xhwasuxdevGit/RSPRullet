// 1라운드 튜토리얼 안내를 언제 띄우고 지우고, 자동 진행 화면이 언제 넘어가도 되는지 정한다 (튜토리얼(1라운드) 세부 기획서 6-3장 TU-F2~F4).
// 화면 상태는 진행 단계와 턴 플레이어로 정한다 (UI·UX 화면 흐름 세부 기획서 3-1장 화면 상태 목록).
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class TutorialGuideRules
{
    // 튜토리얼은 1라운드에서만 한다 (튜토리얼(1라운드) 세부 기획서 6-1-2장 고정 규칙)
    public const int TutorialRoundNumber = 1;

    // 안내는 첫 카드 셔플에서 모두 나오고, 두 번째 카드 셔플을 시작할 때 튜토리얼 종료 안내를 띄운다
    private const int FirstShuffle = 1;
    private const int TutorialEndShuffle = 2;

    // 진행 단계가 바뀌었을 때 그 화면에 맞는 안내 단계. 안내가 없는 화면이면 null이다.
    // 이미 띄운 단계인지와 라운드 번호는 TutorialGuide가 따로 확인한다
    public static TutorialStep? GetStep(GamePhase phase, Side turnPlayer, int shuffleCount)
    {
        switch (phase)
        {
            // 카드풀 공개는 첫 카드 셔플이면 카드풀 안내, 두 번째 카드 셔플이면 튜토리얼 종료 안내다
            case GamePhase.StartCardDeal:
                if (shuffleCount == FirstShuffle) { return TutorialStep.CardPool; }
                return shuffleCount == TutorialEndShuffle ? TutorialStep.TutorialEnd : (TutorialStep?)null;
            // 선 정하기는 비겨도 같은 안내를 띄운다. 비기면 다시 섞는다는 설명이 안내 문구에 들어 있다
            case GamePhase.DecideFirst:
                return TutorialStep.DecideFirst;
            // 플레이어 차례의 안내는 [카드 뽑기] 버튼이 켜지는 뽑기 화면에 띄운다. 딜러 차례의 뽑기는 자동이라 안내가 없다
            case GamePhase.Draw:
                return turnPlayer == Side.Player ? TutorialStep.PlayerDistribute : (TutorialStep?)null;
            // 분배는 딜러 차례일 때만 안내한다. 딜러가 뽑고 고민하는 화면이다 (플레이어 차례는 뽑기 화면에서 이미 안내했다)
            case GamePhase.Distribute:
                return turnPlayer == Side.Dealer ? TutorialStep.DealerDistribute : (TutorialStep?)null;
            case GamePhase.ShowResult:
                return TutorialStep.MatchResult;
            case GamePhase.Settlement:
                return TutorialStep.Settlement;
            // 라운드 종료의 승리·응급약 안내는 말풍선에 띄우지 않는다. 화면이 어두워진 뒤 라운드 전환 화면(ScreenFadeUI)이 GetRoundEndStep으로 정해 보여 준다 (RSP-91)
            default:
                return null;
        }
    }

    // 1라운드가 끝났을 때 검은 화면에 보여 줄 안내 단계. 이기면 승리 안내, 지면 응급약을 처음 먹기 전(0회)에만 응급약 안내다
    // (튜토리얼(1라운드) 세부 기획서 5장 예외 TU3)
    public static TutorialStep? GetRoundEndStep(bool playerWonRound, int medicineCount)
    {
        if (playerWonRound) { return TutorialStep.RoundWin; }
        return medicineCount == 0 ? TutorialStep.RoundLose : (TutorialStep?)null;
    }

    // 말풍선에 남아 있던 안내를 지울지. 정산에서 [다음 카드 셔플]을 눌러 넘어갈 때와 라운드가 끝날 때 지운다.
    // 튜토리얼 종료 안내는 그 카드 셔플의 첫 판 결과를 보여 줄 때 지운다.
    // 그 밖에는 다음 안내가 나올 때까지 남긴다 (튜토리얼(1라운드) 세부 기획서 2-2장 표시 방식).
    // lastPhase는 단계 변경 알림의 이전 단계가 아니라 마지막으로 알림을 받은 단계다.
    // 진행 흐름(GameFlow)은 카드 셔플 단계를 알리지 않아서, [다음 카드 셔플] 다음 알림의 이전 단계는 다음 카드 셔플 대기가 아니라 카드 셔플로 온다.
    // 정산 결과 표시에서 다음 카드 셔플 대기로 넘어갈 때는 정산 안내를 남긴다
    public static bool ShouldClearGuide(TutorialStep? currentStep, GamePhase lastPhase, GamePhase next)
    {
        if (currentStep == TutorialStep.TutorialEnd && next == GamePhase.ShowResult) { return true; }

        return lastPhase == GamePhase.SettlementWait || next == GamePhase.RoundEnd;
    }

    // 자동 진행 화면이 다음으로 넘어가도 되는지. 원래 표시 시간이 지났고, 안내가 클릭을 기다리고 있지 않아야 한다.
    // 빨리 클릭해도 원래 표시 시간은 지키고, 다 읽은 뒤 클릭하면 바로 넘어간다 (튜토리얼(1라운드) 세부 기획서 2-3장)
    public static bool CanAdvance(bool isTimeOver, bool isWaitingForClick)
    {
        return isTimeOver && !isWaitingForClick;
    }
}
