// 판정 기록에 남기는 결과 문구: 선 정하기, 각 판의 판정, 정산(남은 카드, 양쪽 조합 효과)
// (UI·UX 화면 흐름 세부 기획서 5-5장 판정 기록). 행동 기록(가지기·주기, 아이템 사용)은 만들지 않는다.
// 손 모양과 조합 이름은 [ ]로 감싸고, 선 정하기·판정 결과와 체력 변화는 다음 줄에 들여 써서 한 줄이 판정 기록판 너비를 넘지 않게 한다 (RSP-41).
// 진행 흐름(GameFlow)이 결과가 나올 때 부르고, 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다
public static class JudgmentRecordRules
{
    // 결과·체력 변화를 다음 줄로 내릴 때 앞에 붙이는 들여쓰기와 화살표
    private const string NextLine = "\n  → ";

    // 예: "선 정하기: 당신 [바위] vs 딜러 [가위]\n  → 당신 선". 체력 변화를 적지 않는 것으로 판정과 구분한다 (RSP-41에서 "(데미지 없음)" 표기 제외).
    // 비기면 다시 섞는다 (UI·UX 화면 흐름 세부 기획서 3-1장 셔플-06)
    public static string GetFirstTurnText(HandShape playerShape, HandShape dealerShape)
    {
        MatchResult playerResult = HandShapeRules.Compare(playerShape, dealerShape);
        string result = playerResult == MatchResult.Draw
            ? "비김, 다시 섞음"
            : $"{GetSideName(playerResult == MatchResult.Win ? Side.Player : Side.Dealer)} 선";
        return $"선 정하기: {GetVersusText(playerShape, dealerShape)}{NextLine}{result}";
    }

    // 예: "첫 번째 판: 당신 [보] vs 딜러 [바위]\n  → 당신 승리, 딜러: 체력 -1". 비기면 "\n  → 무승부, 체력 변화 없음".
    // 체력 변화량은 실제로 바뀐 값(판정 전후 체력 차이)을 넘긴다.
    // isSwapped는 돋보기로 판 카드를 맞바꾼 채 판정했는지다. 손 모양이 다르면 맞바꾼 판의 승패는 반드시 뒤집히므로,
    // 이긴 쪽·진 쪽 결과 뒤에만 " [돋보기]"를 붙인다. 예: "  → 당신 승리 [돋보기], 딜러: 체력 -2". 비긴 판은 붙이지 않는다 (RSP-100)
    public static string GetMatchText(int matchIndex, HandShape playerShape, HandShape dealerShape,
        int playerHealthChange, int dealerHealthChange, bool isSwapped)
    {
        MatchResult playerResult = HandShapeRules.Compare(playerShape, dealerShape);
        string result;
        switch (playerResult)
        {
            case MatchResult.Win:
                result = "당신 승리";
                break;
            case MatchResult.Lose:
                result = "딜러 승리";
                break;
            default:
                result = "무승부";
                break;
        }

        if (isSwapped && playerResult != MatchResult.Draw)
        {
            result += " [돋보기]";
        }

        return $"{DisplayNames.GetMatchName(matchIndex)}: {GetVersusText(playerShape, dealerShape)}{NextLine}{result}, " +
            GetHealthChangeText(playerHealthChange, dealerHealthChange);
    }

    // 정산 첫 결과. 예: "정산: 남은 카드 [가위]"
    public static string GetRemainingCardText(HandShape remainingShape)
    {
        return $"정산: 남은 카드 [{DisplayNames.GetShapeName(remainingShape)}]";
    }

    // 정산 둘째 결과. 예: "조합: 당신 [공격형], 딜러 [없음]\n  → 딜러: 체력 -2"
    public static string GetCombinationText(CombinationType playerCombination, CombinationType dealerCombination,
        int playerHealthChange, int dealerHealthChange)
    {
        return $"조합: 당신 [{GetCombinationName(playerCombination)}], 딜러 [{GetCombinationName(dealerCombination)}]{NextLine}" +
            GetHealthChangeText(playerHealthChange, dealerHealthChange);
    }

    // 바뀐 쪽만 적는다. 예: "딜러: 체력 -1", "당신: 체력 +1, 딜러: 체력 -2", 둘 다 그대로면 "체력 변화 없음"
    private static string GetHealthChangeText(int playerHealthChange, int dealerHealthChange)
    {
        string playerText = HealthChangeRules.IsShown(playerHealthChange)
            ? $"당신: 체력 {HealthChangeRules.GetChangeText(playerHealthChange)}"
            : null;
        string dealerText = HealthChangeRules.IsShown(dealerHealthChange)
            ? $"딜러: 체력 {HealthChangeRules.GetChangeText(dealerHealthChange)}"
            : null;

        if (playerText == null && dealerText == null)
        {
            return "체력 변화 없음";
        }

        if (playerText == null || dealerText == null)
        {
            return playerText ?? dealerText;
        }

        return $"{playerText}, {dealerText}";
    }

    private static string GetVersusText(HandShape playerShape, HandShape dealerShape)
    {
        return $"당신 [{DisplayNames.GetShapeName(playerShape)}] vs 딜러 [{DisplayNames.GetShapeName(dealerShape)}]";
    }

    // 판정 기록은 플레이어에게 보여 주는 문구라 플레이어를 "당신"으로 적는다 (UI·UX 화면 흐름 세부 기획서 5-5장 예시)
    private static string GetSideName(Side side)
    {
        return side == Side.Player ? "당신" : "딜러";
    }

    // 판정 기록판 너비 안에서 양쪽 조합이 한 줄에 들어가도록 "풀세트"를 빼고 줄여 쓴다 (RSP-41)
    private static string GetCombinationName(CombinationType combination)
    {
        switch (combination)
        {
            case CombinationType.Triple:
                return "트리플";
            case CombinationType.AttackFullSet:
                return "공격형";
            case CombinationType.HealFullSet:
                return "회복형";
            default:
                return "없음";
        }
    }
}
