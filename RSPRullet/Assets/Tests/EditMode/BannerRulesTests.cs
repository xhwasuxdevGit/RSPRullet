using NUnit.Framework;

// 진행 안내 배너 규칙 (RSP-86)
public class BannerRulesTests
{
    // 문구는 배너 설정 에셋에서 오고, {0}에는 라운드 배너면 라운드 번호, 카드 섞기 배너면 셔플 번호가 들어간다
    [TestCase(BannerType.RoundStart, "{0}라운드", "2라운드")]
    [TestCase(BannerType.RoundWin, "{0}라운드 클리어", "2라운드 클리어")]
    [TestCase(BannerType.RoundLose, "{0}라운드 패배", "2라운드 패배")]
    [TestCase(BannerType.ShuffleStart, "{0}번째 카드 섞기", "3번째 카드 섞기")]
    [TestCase(BannerType.SettlementStart, "카드 정산", "카드 정산")]
    [TestCase(BannerType.SuddenDeath, "서든 데스\n<size=60%>체력 1 단판 승부</size>", "서든 데스\n<size=60%>체력 1 단판 승부</size>")]
    public void FormatText_PutsNumberPerType(BannerType type, string template, string expected)
    {
        Assert.AreEqual(expected, BannerRules.FormatText(template, type, roundNumber: 2, shuffleCount: 3));
    }

    // 마지막 라운드의 라운드 승패는 게임 종료 팝업이 보여 주므로 배너를 띄우지 않는다. 다른 배너는 마지막 라운드에도 띄운다
    [TestCase(BannerType.RoundWin, false, true)]
    [TestCase(BannerType.RoundLose, false, true)]
    [TestCase(BannerType.RoundWin, true, false)]
    [TestCase(BannerType.RoundLose, true, false)]
    [TestCase(BannerType.RoundStart, true, true)]
    [TestCase(BannerType.MatchWin, true, true)]
    public void IsShown_HidesRoundResultOnlyInFinalRound(BannerType type, bool isFinalRound, bool expected)
    {
        Assert.AreEqual(expected, BannerRules.IsShown(type, isFinalRound));
    }

    // 라운드 전환 화면이 지울 때까지 남겨 두는 배너는 마지막 라운드가 아닌 라운드 승패뿐이다 (RSP-91)
    [TestCase(BannerType.RoundWin, false, true)]
    [TestCase(BannerType.RoundLose, false, true)]
    [TestCase(BannerType.RoundWin, true, false)]
    [TestCase(BannerType.RoundLose, true, false)]
    [TestCase(BannerType.RoundStart, false, false)]
    [TestCase(BannerType.MatchWin, false, false)]
    public void IsHeldForTransition_HoldsOnlyNonFinalRoundResult(BannerType type, bool isFinalRound, bool expected)
    {
        Assert.AreEqual(expected, BannerRules.IsHeldForTransition(type, isFinalRound));
    }

    [TestCase(BannerType.RoundStart, false)]
    [TestCase(BannerType.ShuffleStart, false)]
    [TestCase(BannerType.SettlementStart, false)]
    [TestCase(BannerType.RoundWin, false)]
    [TestCase(BannerType.PlayerFirst, true)]
    [TestCase(BannerType.FirstTurnDraw, true)]
    [TestCase(BannerType.MatchDraw, true)]
    [TestCase(BannerType.SuddenDeath, true)]
    public void IsResult_SeparatesResultFromTransition(BannerType type, bool expected)
    {
        Assert.AreEqual(expected, BannerRules.IsResult(type));
    }

    // 역할은 진행 전환·선 정하기 결과·판 결과·라운드 4가지다
    [TestCase(BannerType.ShuffleStart, BannerRole.Transition)]
    [TestCase(BannerType.SettlementStart, BannerRole.Transition)]
    [TestCase(BannerType.PlayerFirst, BannerRole.FirstTurn)]
    [TestCase(BannerType.DealerFirst, BannerRole.FirstTurn)]
    [TestCase(BannerType.FirstTurnDraw, BannerRole.FirstTurn)]
    [TestCase(BannerType.MatchWin, BannerRole.MatchResult)]
    [TestCase(BannerType.MatchLose, BannerRole.MatchResult)]
    [TestCase(BannerType.MatchDraw, BannerRole.MatchResult)]
    [TestCase(BannerType.SuddenDeath, BannerRole.MatchResult)]
    [TestCase(BannerType.RoundStart, BannerRole.Round)]
    [TestCase(BannerType.RoundWin, BannerRole.Round)]
    [TestCase(BannerType.RoundLose, BannerRole.Round)]
    public void GetRole_GroupsTypesIntoFourRoles(BannerType type, BannerRole expected)
    {
        Assert.AreEqual(expected, BannerRules.GetRole(type));
    }
}
