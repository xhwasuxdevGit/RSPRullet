// 카드 조합 판정 규칙 (코어 규칙 상세 기획서 4-1·4-2장). 화면·진행 상태와 무관하게 호출할 수 있다.
public static class CombinationRules
{
    // 받은 순서(시작 카드 → 첫 번째 판 → 두 번째 판)대로 넣은 3장의 패턴
    public static CombinationType Judge(HandShape first, HandShape second, HandShape third)
    {
        if (first == second && second == third)
        {
            return CombinationType.Triple;
        }

        bool allDifferent = first != second && second != third && first != third;
        if (!allDifferent)
        {
            return CombinationType.None;
        }

        // 세 손 모양이 모두 다르면 뒤 카드가 앞 카드를 이기는 방향이 두 쌍 모두 같으므로 첫 쌍만 본다
        return HandShapeRules.Beats(second, first) ? CombinationType.AttackFullSet : CombinationType.HealFullSet;
    }
}
