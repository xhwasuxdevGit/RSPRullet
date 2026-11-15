// 가위바위보 상성 규칙. 화면·진행 상태와 무관하게 호출할 수 있다.
public static class HandShapeRules
{
    // mine 기준으로 other와 비교한 결과
    public static MatchResult Compare(HandShape mine, HandShape other)
    {
        if (mine == other)
        {
            return MatchResult.Draw;
        }

        return Beats(mine, other) ? MatchResult.Win : MatchResult.Lose;
    }

    // 가위는 보를, 바위는 가위를, 보는 바위를 이긴다
    public static bool Beats(HandShape attacker, HandShape defender)
    {
        return (attacker == HandShape.Scissors && defender == HandShape.Paper)
            || (attacker == HandShape.Rock && defender == HandShape.Scissors)
            || (attacker == HandShape.Paper && defender == HandShape.Rock);
    }
}
