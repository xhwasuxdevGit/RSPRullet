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

    [Test]
    public void TakeMedicine_CountsUpAndKeepsRound()
    {
        var progress = new RoundProgress();
        progress.AdvanceRound();

        Assert.AreEqual(0, progress.MedicineCount);
        Assert.IsTrue(progress.TakeMedicine());
        Assert.IsTrue(progress.TakeMedicine());
        Assert.AreEqual(2, progress.MedicineCount);
        Assert.AreEqual(2, progress.RoundNumber);
    }

    [Test]
    public void TakeMedicine_AtFinalRound_Fails()
    {
        var progress = new RoundProgress();
        progress.AdvanceRound();
        progress.AdvanceRound();

        Assert.IsFalse(progress.TakeMedicine());
        Assert.AreEqual(0, progress.MedicineCount);
    }
}
