using NUnit.Framework;

public class RoundProgressTests
{
    [Test]
    public void StartsAtFirstRound()
    {
        var progress = new RoundProgress();

        Assert.AreEqual(1, progress.RoundNumber);
        Assert.IsFalse(progress.IsFinalRound);
    }

    [Test]
    public void AdvanceRound_GoesUpToFinalRound()
    {
        var progress = new RoundProgress();

        Assert.IsTrue(progress.AdvanceRound());
        Assert.AreEqual(2, progress.RoundNumber);
        Assert.IsFalse(progress.IsFinalRound);

        Assert.IsTrue(progress.AdvanceRound());
        Assert.AreEqual(3, progress.RoundNumber);
        Assert.IsTrue(progress.IsFinalRound);
    }

    [Test]
    public void AdvanceRound_AtFinalRound_StaysAndFails()
    {
        var progress = new RoundProgress();
        progress.AdvanceRound();
        progress.AdvanceRound();

        Assert.IsFalse(progress.AdvanceRound());
        Assert.AreEqual(RoundProgress.RoundCount, progress.RoundNumber);
    }
}
