using System.Collections.Generic;
using NUnit.Framework;

public class PhaseMachineTests
{
    // 라운드 시작부터 선 정하기까지 진행한 상태를 만든다
    private static PhaseMachine MakeAtDecideFirst()
    {
        var machine = new PhaseMachine();
        machine.CompleteRoundStart();
        machine.CompleteCardShuffle();
        machine.CompleteStartCardDeal(hasClockCarryOver: false);
        return machine;
    }

    // 뽑기부터 결과 표시 직전까지 한 판을 진행한다
    private static void PlayMatchUntilShowResult(PhaseMachine machine)
    {
        machine.CompleteDraw();
        machine.CompleteDistribute();
        machine.CompleteItemUse();
        machine.CompleteItemUse();
        machine.CompleteReveal();
        machine.CompleteJudge();
    }

    [Test]
    public void StartsAtRoundStart()
    {
        Assert.AreEqual(GamePhase.RoundStart, new PhaseMachine().CurrentPhase);
    }

    [Test]
    public void WithoutClockCarryOver_GoesToDecideFirst()
    {
        Assert.AreEqual(GamePhase.DecideFirst, MakeAtDecideFirst().CurrentPhase);
    }

    [Test]
    public void WithClockCarryOver_SkipsDecideFirst()
    {
        var machine = new PhaseMachine();
        machine.CompleteRoundStart();
        machine.CompleteCardShuffle();

        machine.CompleteStartCardDeal(hasClockCarryOver: true);

        Assert.AreEqual(GamePhase.Draw, machine.CurrentPhase);
        Assert.AreEqual(0, machine.MatchIndex);
    }

    [Test]
    public void DecideFirstDraw_ReturnsToCardShuffle()
    {
        var machine = MakeAtDecideFirst();

        machine.CompleteDecideFirst(isDraw: true);

        Assert.AreEqual(GamePhase.CardShuffle, machine.CurrentPhase);
    }

    [Test]
    public void OneMatch_FollowsSubPhaseOrder()
    {
        var machine = MakeAtDecideFirst();
        var visited = new List<GamePhase>();
        machine.PhaseChanged += (previous, next) => visited.Add(next);

        machine.CompleteDecideFirst(isDraw: false);
        PlayMatchUntilShowResult(machine);

        CollectionAssert.AreEqual(new[]
        {
            GamePhase.Draw, GamePhase.Distribute, GamePhase.ItemTurnPlayer, GamePhase.ItemOpponent,
            GamePhase.Reveal, GamePhase.Judge, GamePhase.ShowResult
        }, visited);
    }

    [Test]
    public void FirstMatchEnd_GoesToSecondMatchDraw()
    {
        var machine = MakeAtDecideFirst();
        machine.CompleteDecideFirst(isDraw: false);
        PlayMatchUntilShowResult(machine);

        machine.CompleteShowResult(someoneAtZeroHealth: false);

        Assert.AreEqual(GamePhase.Draw, machine.CurrentPhase);
        Assert.AreEqual(1, machine.MatchIndex);
    }

    [Test]
    public void SecondMatchEnd_GoesToSettlement()
    {
        var machine = MakeAtDecideFirst();
        machine.CompleteDecideFirst(isDraw: false);
        PlayMatchUntilShowResult(machine);
        machine.CompleteShowResult(someoneAtZeroHealth: false);
        PlayMatchUntilShowResult(machine);

        machine.CompleteShowResult(someoneAtZeroHealth: false);

        Assert.AreEqual(GamePhase.Settlement, machine.CurrentPhase);
    }

    [Test]
    public void ZeroHealthInFirstMatch_EndsRoundWithoutSettlement()
    {
        var machine = MakeAtDecideFirst();
        machine.CompleteDecideFirst(isDraw: false);
        PlayMatchUntilShowResult(machine);

        machine.CompleteShowResult(someoneAtZeroHealth: true);

        Assert.AreEqual(GamePhase.RoundEnd, machine.CurrentPhase);
    }

    [TestCase(true, GamePhase.RoundEnd)]
    [TestCase(false, GamePhase.CardShuffle)]
    public void Settlement_GoesToRoundEndOrNextShuffle(bool isRoundOver, GamePhase expected)
    {
        var machine = MakeAtDecideFirst();
        machine.CompleteDecideFirst(isDraw: false);
        PlayMatchUntilShowResult(machine);
        machine.CompleteShowResult(someoneAtZeroHealth: false);
        PlayMatchUntilShowResult(machine);
        machine.CompleteShowResult(someoneAtZeroHealth: false);

        machine.CompleteSettlement(isRoundOver);

        Assert.AreEqual(expected, machine.CurrentPhase);
    }

    [Test]
    public void NextShuffle_StartsFromFirstMatchAgain()
    {
        var machine = MakeAtDecideFirst();
        machine.CompleteDecideFirst(isDraw: false);
        PlayMatchUntilShowResult(machine);
        machine.CompleteShowResult(someoneAtZeroHealth: false);
        PlayMatchUntilShowResult(machine);
        machine.CompleteShowResult(someoneAtZeroHealth: false);
        machine.CompleteSettlement(isRoundOver: false);

        machine.CompleteCardShuffle();
        machine.CompleteStartCardDeal(hasClockCarryOver: false);
        machine.CompleteDecideFirst(isDraw: false);

        Assert.AreEqual(0, machine.MatchIndex);
    }

    [Test]
    public void InputInWrongPhase_IsIgnored()
    {
        var machine = new PhaseMachine();
        int changeCount = 0;
        machine.PhaseChanged += (previous, next) => changeCount++;

        bool accepted = machine.CompleteDraw();

        Assert.IsFalse(accepted);
        Assert.AreEqual(GamePhase.RoundStart, machine.CurrentPhase);
        Assert.AreEqual(0, changeCount);
    }

    [Test]
    public void PhaseChanged_ReportsPreviousAndNext()
    {
        var machine = new PhaseMachine();
        GamePhase reportedPrevious = GamePhase.RoundEnd;
        GamePhase reportedNext = GamePhase.RoundEnd;
        machine.PhaseChanged += (previous, next) =>
        {
            reportedPrevious = previous;
            reportedNext = next;
        };

        machine.CompleteRoundStart();

        Assert.AreEqual(GamePhase.RoundStart, reportedPrevious);
        Assert.AreEqual(GamePhase.CardShuffle, reportedNext);
    }
}
