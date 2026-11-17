using NUnit.Framework;

// 조합 칸 표시 규칙 (UI·UX 화면 흐름 세부 기획서 5-6장 조합 칸)
public class CombinationSlotRulesTests
{
    // 카드 슬롯에 이번 판 카드가 있는 동안에는 조합 칸 목록의 마지막 카드를 빼고 보여 준다
    [TestCase(2, GamePhase.ItemTurnPlayer, 1)]
    [TestCase(2, GamePhase.Reveal, 1)]
    [TestCase(2, GamePhase.ShowResult, 1)]
    [TestCase(3, GamePhase.ShowResult, 2)]
    public void ShownCount_ExcludesCardOnCardSlot(int slotCount, GamePhase phase, int expected)
    {
        Assert.AreEqual(expected, CombinationSlotRules.GetShownCount(slotCount, phase));
    }

    [TestCase(1, GamePhase.Draw, 1)]
    [TestCase(2, GamePhase.Draw, 2)]
    [TestCase(3, GamePhase.Settlement, 3)]
    [TestCase(0, GamePhase.Draw, 0)]
    public void ShownCount_ShowsAllOutsideCardSlotPhases(int slotCount, GamePhase phase, int expected)
    {
        Assert.AreEqual(expected, CombinationSlotRules.GetShownCount(slotCount, phase));
    }

    // ▲ = 뒤 카드가 앞 카드를 이김, ▼ = 짐, = = 같은 손 모양
    [TestCase(HandShape.Scissors, HandShape.Rock, MatchResult.Win)]
    [TestCase(HandShape.Rock, HandShape.Paper, MatchResult.Win)]
    [TestCase(HandShape.Paper, HandShape.Scissors, MatchResult.Win)]
    [TestCase(HandShape.Rock, HandShape.Scissors, MatchResult.Lose)]
    [TestCase(HandShape.Paper, HandShape.Rock, MatchResult.Lose)]
    [TestCase(HandShape.Paper, HandShape.Paper, MatchResult.Draw)]
    public void Link_ComparesLaterCardAgainstEarlier(HandShape earlier, HandShape later, MatchResult expected)
    {
        Assert.AreEqual(expected, CombinationSlotRules.GetLink(earlier, later));
    }

    [TestCase(HandShape.Rock, HandShape.Rock, CombinationType.Triple)]
    [TestCase(HandShape.Scissors, HandShape.Rock, CombinationType.AttackFullSet)]
    [TestCase(HandShape.Rock, HandShape.Scissors, CombinationType.HealFullSet)]
    public void PossibleCombination_FollowsFirstPair(HandShape first, HandShape second, CombinationType expected)
    {
        Assert.AreEqual(expected, CombinationSlotRules.GetPossibleCombination(first, second));
    }

    // 안내한 패턴은 세 번째 카드에 따라 실제로 이룰 수 있어야 한다 (코어 규칙 상세 기획서 4-2장)
    [TestCase(HandShape.Scissors, HandShape.Scissors)]
    [TestCase(HandShape.Scissors, HandShape.Rock)]
    [TestCase(HandShape.Paper, HandShape.Rock)]
    public void PossibleCombination_IsReachableWithSomeThirdCard(HandShape first, HandShape second)
    {
        CombinationType possible = CombinationSlotRules.GetPossibleCombination(first, second);

        bool reachable = false;
        foreach (HandShape third in new[] { HandShape.Scissors, HandShape.Rock, HandShape.Paper })
        {
            reachable |= CombinationRules.Judge(first, second, third) == possible;
        }

        Assert.IsTrue(reachable);
    }

    // 세 장이 모두 보일 때: 조합이 아니면 마지막 화살표만 숨기고, 풀세트면 두 화살표를 모두 보여 준다
    [TestCase(HandShape.Rock, HandShape.Paper, HandShape.Rock, MatchResult.Win, MatchResult.Draw)]
    [TestCase(HandShape.Rock, HandShape.Rock, HandShape.Paper, MatchResult.Draw, MatchResult.Draw)]
    [TestCase(HandShape.Paper, HandShape.Scissors, HandShape.Scissors, MatchResult.Win, MatchResult.Draw)]
    [TestCase(HandShape.Scissors, HandShape.Rock, HandShape.Paper, MatchResult.Win, MatchResult.Win)]
    [TestCase(HandShape.Scissors, HandShape.Paper, HandShape.Rock, MatchResult.Lose, MatchResult.Lose)]
    [TestCase(HandShape.Rock, HandShape.Rock, HandShape.Rock, MatchResult.Draw, MatchResult.Draw)]
    public void ShownLink_LastLinkOnlyWhenCombinationCompleted(HandShape first, HandShape second, HandShape third, MatchResult expectedFirst, MatchResult expectedLast)
    {
        Card[] slots = { new Card(first), new Card(second), new Card(third) };

        Assert.AreEqual(expectedFirst, CombinationSlotRules.GetShownLink(slots, 3, 0));
        Assert.AreEqual(expectedLast, CombinationSlotRules.GetShownLink(slots, 3, 1));
    }

    // 두 번째 판 동안(세 번째 카드가 카드 슬롯에 있음)은 마지막 화살표를 판단하지 않고 숨긴다
    [Test]
    public void ShownLink_HidesLinkToCardNotYetShown()
    {
        Card[] slots = { new Card(HandShape.Scissors), new Card(HandShape.Rock), new Card(HandShape.Paper) };

        Assert.AreEqual(MatchResult.Win, CombinationSlotRules.GetShownLink(slots, 2, 0));
        Assert.AreEqual(MatchResult.Draw, CombinationSlotRules.GetShownLink(slots, 2, 1));
    }
}
