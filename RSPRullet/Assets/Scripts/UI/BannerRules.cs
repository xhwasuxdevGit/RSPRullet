// 진행 안내 배너 규칙: 종류별 문구, 역할(색), 전환·결과 구분, 마지막 라운드에서 띄울지 (RSP-86).
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class BannerRules
{
    // 마지막 라운드의 승패는 게임 종료 팝업(GameEndPopupUI)이 보여 주므로 라운드 승패 배너를 띄우지 않는다
    public static bool IsShown(BannerType type, bool isFinalRound)
    {
        bool isRoundResult = type == BannerType.RoundWin || type == BannerType.RoundLose;
        return !(isRoundResult && isFinalRound);
    }

    // 읽는 시간이 지나도 스스로 사라지지 않고 라운드 전환 화면(ScreenFadeUI)이 지울 때까지 남겨 둘 배너인지 (RSP-91).
    // 마지막 라운드가 아닌 라운드 승패 배너만 남겨, 배너가 떠 있는 채로 화면이 어두워지게 한다.
    // 마지막 라운드의 승패 배너는 띄우지 않으므로(IsShown) 남길 일도 없다
    public static bool IsHeldForTransition(BannerType type, bool isFinalRound)
    {
        bool isRoundResult = type == BannerType.RoundWin || type == BannerType.RoundLose;
        return isRoundResult && !isFinalRound;
    }

    // 결과 배너인지. 결과 배너는 읽는 시간을 전환 배너보다 길게 둔다
    public static bool IsResult(BannerType type)
    {
        switch (type)
        {
            case BannerType.PlayerFirst:
            case BannerType.DealerFirst:
            case BannerType.FirstTurnDraw:
            case BannerType.MatchWin:
            case BannerType.MatchLose:
            case BannerType.MatchDraw:
            case BannerType.SuddenDeath:
                return true;
            default:
                return false;
        }
    }

    // 배너 역할. 화면은 역할마다 배경색과 글자색을 다르게 쓴다
    public static BannerRole GetRole(BannerType type)
    {
        switch (type)
        {
            case BannerType.PlayerFirst:
            case BannerType.DealerFirst:
            case BannerType.FirstTurnDraw:
                return BannerRole.FirstTurn;
            case BannerType.MatchWin:
            case BannerType.MatchLose:
            case BannerType.MatchDraw:
            case BannerType.SuddenDeath:
                return BannerRole.MatchResult;
            case BannerType.RoundStart:
            case BannerType.RoundWin:
            case BannerType.RoundLose:
                return BannerRole.Round;
            default:
                return BannerRole.Transition;
        }
    }

    // 배너 설정 에셋(BannerSettings)의 문구에서 {0} 자리에 번호를 넣는다.
    // 라운드 배너는 라운드 번호, 카드 섞기 배너는 셔플 번호(1부터)를 넣고, 다른 배너는 번호를 쓰지 않는다
    public static string FormatText(string template, BannerType type, int roundNumber, int shuffleCount)
    {
        switch (type)
        {
            case BannerType.RoundStart:
            case BannerType.RoundWin:
            case BannerType.RoundLose:
                return string.Format(template, roundNumber);
            case BannerType.ShuffleStart:
                return string.Format(template, shuffleCount);
            default:
                return template;
        }
    }
}
