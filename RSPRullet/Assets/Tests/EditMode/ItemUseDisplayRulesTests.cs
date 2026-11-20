using NUnit.Framework;

// 아이템 사용 표시를 지우는 시점 (UI·UX 화면 흐름 세부 기획서 5-3장 아이템 사용 표시)
public class ItemUseDisplayRulesTests
{
    // 아이템 단계부터 판정까지는 세 종류 모두 보여 준다
    [TestCase(GamePhase.ItemTurnPlayer)]
    [TestCase(GamePhase.ItemOpponent)]
    [TestCase(GamePhase.Reveal)]
    [TestCase(GamePhase.Judge)]
    public void ItemPhaseToJudge_ShowsAllItems(GamePhase phase)
    {
        Assert.IsTrue(ItemUseDisplayRules.IsShown(ItemType.Coffee, phase));
        Assert.IsTrue(ItemUseDisplayRules.IsShown(ItemType.Magnifier, phase));
        Assert.IsTrue(ItemUseDisplayRules.IsShown(ItemType.Clock, phase));
    }

    // 판 결과 표시: 돋보기는 판정 때 지우고, 커피·시계는 남긴다
    [TestCase(ItemType.Coffee, true)]
    [TestCase(ItemType.Magnifier, false)]
    [TestCase(ItemType.Clock, true)]
    public void ShowResult_HidesMagnifierOnly(ItemType item, bool expected)
    {
        Assert.AreEqual(expected, ItemUseDisplayRules.IsShown(item, GamePhase.ShowResult));
    }

    // 정산: 두 번째 판에 쓴 시계만 다음 카드 셔플의 첫 번째 판까지 남긴다
    [TestCase(ItemType.Coffee, false)]
    [TestCase(ItemType.Magnifier, false)]
    [TestCase(ItemType.Clock, true)]
    public void Settlement_ShowsClockOnly(ItemType item, bool expected)
    {
        Assert.AreEqual(expected, ItemUseDisplayRules.IsShown(item, GamePhase.Settlement));
    }

    // 다음 판이 시작되면(뽑기·분배) 지난 판 기록이 남아 있어도 보여 주지 않는다
    [TestCase(GamePhase.Draw)]
    [TestCase(GamePhase.Distribute)]
    [TestCase(GamePhase.RoundEnd)]
    public void NewMatchOrRoundEnd_HidesAllItems(GamePhase phase)
    {
        Assert.IsFalse(ItemUseDisplayRules.IsShown(ItemType.Coffee, phase));
        Assert.IsFalse(ItemUseDisplayRules.IsShown(ItemType.Magnifier, phase));
        Assert.IsFalse(ItemUseDisplayRules.IsShown(ItemType.Clock, phase));
    }
}
