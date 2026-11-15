using UnityEngine;

// 모든 라운드에 공통인 밸런스 값 (코어 규칙 상세 기획서 7-1-1장, 게임 컨셉 기획서 9장). 에셋은 하나만 만든다.
[CreateAssetMenu(fileName = "RuleSettings", menuName = "RSPRullet/Rule Settings")]
public class RuleSettings : ScriptableObject
{
    [SerializeField] private int _maxHealth = 10;
    [SerializeField] private int _matchDamage = 1;
    [SerializeField] private int _coffeeMultiplier = 2;
    [SerializeField] private int _attackFullSetDamage = 1;
    [SerializeField] private int _healFullSetAmount = 1;
    [SerializeField] private int _tripleDamage = 3;

    // 다음 판이 원래 플레이어 차례일 때 딜러가 시계를 쓸 확률
    [SerializeField, Range(0f, 1f)] private float _dealerClockChance = 0.85f;

    public int MaxHealth => _maxHealth;
    public int MatchDamage => _matchDamage;
    public int CoffeeMultiplier => _coffeeMultiplier;
    public int AttackFullSetDamage => _attackFullSetDamage;
    public int HealFullSetAmount => _healFullSetAmount;
    public int TripleDamage => _tripleDamage;
    public float DealerClockChance => _dealerClockChance;
}
