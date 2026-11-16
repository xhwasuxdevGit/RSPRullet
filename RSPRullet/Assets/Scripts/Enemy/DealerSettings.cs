using UnityEngine;

// 딜러 AI 밸런스 값 (딜러 AI 세부 기획서 7-1-1장). 인스펙터에서 조절하며, 에셋은 하나만 만든다.
// 라운드별 딜러 강화도 이 값으로만 조절한다 (딜러 AI 세부 기획서 5장 라운드별 적용).
[CreateAssetMenu(fileName = "DealerSettings", menuName = "RSPRullet/Dealer Settings")]
public class DealerSettings : ScriptableObject
{
    // 손해가 분명한 상황(2번째 판, 선택 간 HP 차이 2 이상)에서 규칙상 결론을 따를 확률
    [SerializeField, Range(0.5f, 1f)] private float _strengthCorrectionRate = 1f;

    // 두 선택 모두 근거가 있는 상황에서 규칙상 결론을 따를 확률
    [SerializeField, Range(0.5f, 1f)] private float _ruleComplianceRate = 1f;

    // 시계 조건이 맞을 때 사용할 확률
    [SerializeField, Range(0f, 1f)] private float _clockUseChance = 0.85f;

    // 이길 경우가 전체 경우 중 이 비율 이상이면 커피를 사용한다
    [SerializeField, Range(0f, 1f)] private float _coffeeUseThreshold = 0.5f;

    public float StrengthCorrectionRate => _strengthCorrectionRate;
    public float RuleComplianceRate => _ruleComplianceRate;
    public float ClockUseChance => _clockUseChance;
    public float CoffeeUseThreshold => _coffeeUseThreshold;
}
