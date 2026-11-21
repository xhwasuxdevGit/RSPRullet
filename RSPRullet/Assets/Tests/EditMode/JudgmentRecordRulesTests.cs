using NUnit.Framework;

// 판정 기록 문구 (UI·UX 화면 흐름 세부 기획서 5-5장 판정 기록).
// 손 모양·조합 이름은 [ ]로 감싸고, 선 정하기 결과와 체력 변화는 다음 줄에 "  → "로 들여 쓴다
public class JudgmentRecordRulesTests
{
    // 선 정하기는 이긴 쪽이 선이고 체력 변화를 적지 않는다. 비기면 다시 섞는다
    [TestCase(HandShape.Rock, HandShape.Scissors, "선 정하기: 당신 [바위] vs 딜러 [가위]\n  → 당신 선")]
    [TestCase(HandShape.Rock, HandShape.Paper, "선 정하기: 당신 [바위] vs 딜러 [보]\n  → 딜러 선")]
    [TestCase(HandShape.Paper, HandShape.Paper, "선 정하기: 당신 [보] vs 딜러 [보]\n  → 비김, 다시 섞음")]
    public void FirstTurnText(HandShape player, HandShape dealer, string expected)
    {
        Assert.AreEqual(expected, JudgmentRecordRules.GetFirstTurnText(player, dealer));
    }

    // 판 번호와 손 모양을 한 줄에 쓰고, 승패와 체력 변화는 다음 줄에 함께 쓴다
    [TestCase(0, HandShape.Paper, HandShape.Rock, 0, -1, "첫 번째 판: 당신 [보] vs 딜러 [바위]\n  → 당신 승리, 딜러: 체력 -1")]
    [TestCase(1, HandShape.Scissors, HandShape.Rock, -1, 0, "두 번째 판: 당신 [가위] vs 딜러 [바위]\n  → 딜러 승리, 당신: 체력 -1")]
    [TestCase(0, HandShape.Rock, HandShape.Rock, 0, 0, "첫 번째 판: 당신 [바위] vs 딜러 [바위]\n  → 무승부, 체력 변화 없음")]
    public void MatchText(int matchIndex, HandShape player, HandShape dealer, int playerChange, int dealerChange, string expected)
    {
        Assert.AreEqual(expected, JudgmentRecordRules.GetMatchText(matchIndex, player, dealer, playerChange, dealerChange, false));
    }

    // 돋보기로 카드를 맞바꿔 승패가 바뀐 판은 이긴 쪽·진 쪽 결과 뒤에 [돋보기]를 붙이고, 비긴 판은 붙이지 않는다 (RSP-100)
    [TestCase(HandShape.Paper, HandShape.Rock, 0, -1, "첫 번째 판: 당신 [보] vs 딜러 [바위]\n  → 당신 승리 [돋보기], 딜러: 체력 -1")]
    [TestCase(HandShape.Scissors, HandShape.Rock, -2, 0, "첫 번째 판: 당신 [가위] vs 딜러 [바위]\n  → 딜러 승리 [돋보기], 당신: 체력 -2")]
    [TestCase(HandShape.Rock, HandShape.Rock, 0, 0, "첫 번째 판: 당신 [바위] vs 딜러 [바위]\n  → 무승부, 체력 변화 없음")]
    public void MatchText_Swapped(HandShape player, HandShape dealer, int playerChange, int dealerChange, string expected)
    {
        Assert.AreEqual(expected, JudgmentRecordRules.GetMatchText(0, player, dealer, playerChange, dealerChange, true));
    }

    [Test]
    public void RemainingCardText()
    {
        Assert.AreEqual("정산: 남은 카드 [가위]", JudgmentRecordRules.GetRemainingCardText(HandShape.Scissors));
    }

    // 조합 이름은 "풀세트"를 빼고 줄여 쓰고, 조합이 없는 쪽은 [없음], 체력은 바뀐 쪽만 적는다
    [TestCase(CombinationType.AttackFullSet, CombinationType.None, 0, -2, "조합: 당신 [공격형], 딜러 [없음]\n  → 딜러: 체력 -2")]
    [TestCase(CombinationType.HealFullSet, CombinationType.Triple, -1, 0, "조합: 당신 [회복형], 딜러 [트리플]\n  → 당신: 체력 -1")]
    [TestCase(CombinationType.HealFullSet, CombinationType.AttackFullSet, 1, -2, "조합: 당신 [회복형], 딜러 [공격형]\n  → 당신: 체력 +1, 딜러: 체력 -2")]
    [TestCase(CombinationType.None, CombinationType.None, 0, 0, "조합: 당신 [없음], 딜러 [없음]\n  → 체력 변화 없음")]
    public void CombinationText(CombinationType player, CombinationType dealer, int playerChange, int dealerChange, string expected)
    {
        Assert.AreEqual(expected, JudgmentRecordRules.GetCombinationText(player, dealer, playerChange, dealerChange));
    }
}
