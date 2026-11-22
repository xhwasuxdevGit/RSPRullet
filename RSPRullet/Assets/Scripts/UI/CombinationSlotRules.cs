using System.Collections.Generic;

// 카드 조합 칸에 보여 줄 카드 수, 칸 사이 화살표, 두 칸이 찼을 때의 가능 패턴을 정한다
// (UI·UX 화면 흐름 세부 기획서 5-6장 조합 칸).
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class CombinationSlotRules
{
    // 가능 패턴을 안내하는 시점: 시작 카드와 첫 번째 판 카드, 두 칸이 찼을 때
    public const int HintCardCount = 2;

    // 조합 칸이 모두 찬 장수 (시작 카드 → 첫 번째 판 → 두 번째 판)
    private const int FullCardCount = 3;

    // 분배하는 순간 이번 판 카드가 조합 칸 목록 끝에 들어가지만, 승패 판정 표시(판-07)가 끝날 때까지는
    // 카드 슬롯에 놓여 있으므로 조합 칸에서는 빼고 보여 준다 (CardFaceRules.HasSlotCards).
    // 시작 카드는 나눠 준 뒤에도 카드 섞기 연출로 조합 칸까지 이동하는 동안 보여 주지 않고, 선 정하기 단계부터 보여 준다 (CardShuffleUI)
    public static int GetShownCount(int slotCount, GamePhase phase)
    {
        if (phase == GamePhase.StartCardDeal) { return 0; }

        if (slotCount > 0 && CardFaceRules.HasSlotCards(phase))
        {
            return slotCount - 1;
        }

        return slotCount;
    }

    // 뒤 카드 기준 비교: Win이면 위쪽 화살표(앞 카드를 이김), Lose면 아래쪽 화살표(앞 카드에 짐), Draw(같은 손 모양)면 표시 없음
    public static MatchResult GetLink(HandShape earlier, HandShape later)
    {
        return HandShapeRules.Compare(later, earlier);
    }

    // 칸 사이 화살표 하나(linkIndex 0은 1·2칸 사이, 1은 2·3칸 사이)에 보여 줄 관계. Draw면 표시하지 않는다.
    // 양옆 칸이 모두 보여야 하고, 마지막 칸(세 번째 카드)으로 가는 화살표는 세 장이 조합을 이룰 때만 보여 준다
    public static MatchResult GetShownLink(IReadOnlyList<Card> slots, int shownCount, int linkIndex)
    {
        int laterIndex = linkIndex + 1;
        if (laterIndex >= shownCount)
        {
            return MatchResult.Draw;
        }

        bool isLastLink = laterIndex == FullCardCount - 1;
        if (isLastLink && CombinationRules.Judge(slots[0].Shape, slots[1].Shape, slots[2].Shape) == CombinationType.None)
        {
            return MatchResult.Draw;
        }

        return GetLink(slots[linkIndex].Shape, slots[laterIndex].Shape);
    }

    // 두 칸이 찼을 때 세 번째 카드에 따라 이룰 수 있는 패턴. 두 장이 같으면 트리플,
    // 다르면 첫 쌍의 방향이 풀세트 종류를 정한다 (CombinationRules.Judge와 같은 기준).
    // 두 장이 같아도 이번 카드풀에 그 손 모양이 3장 미만이면 세 번째 카드가 나올 수 없어 None을 돌려준다
    // (UI·UX 화면 흐름 세부 기획서 5-6장 조합 칸). 남은 카드 수는 보지 않고 카드풀 구성만 본다
    public static CombinationType GetPossibleCombination(HandShape first, HandShape second, int firstShapePoolCount)
    {
        switch (GetLink(first, second))
        {
            case MatchResult.Draw:
                return firstShapePoolCount >= GameRules.TripleShapeCardCount ? CombinationType.Triple : CombinationType.None;
            case MatchResult.Win:
                return CombinationType.AttackFullSet;
            default:
                return CombinationType.HealFullSet;
        }
    }
}
