// 체력 변화량 표기 규칙: 체력이 바뀌면 숫자 옆에 변화량을 짧게 보여 준다. 감소는 빨강 -1, 회복은 초록 +1
// (UI·UX 화면 흐름 세부 기획서 5-7장 체력).
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class HealthChangeRules
{
    // 변화가 없으면(같은 값) 표기하지 않는다
    public static bool IsShown(int change)
    {
        return change != 0;
    }

    // 회복이면 초록, 감소면 빨강으로 보여 준다
    public static bool IsHeal(int change)
    {
        return change > 0;
    }

    // 부호를 붙인 변화량 문구. 회복 "+1", 감소 "-1"
    public static string GetChangeText(int change)
    {
        return change > 0 ? $"+{change}" : change.ToString();
    }
}
