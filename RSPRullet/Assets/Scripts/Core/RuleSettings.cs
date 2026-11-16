using UnityEngine;

// 모든 라운드에 공통으로 적용되는 밸런스 조절용 값 모음. 에셋은 하나만 만든다.
[CreateAssetMenu(fileName = "RuleSettings", menuName = "RSPRullet/Rule Settings")]
public class RuleSettings : ScriptableObject
{
    [SerializeField] private int _maxHealth = 10;
    [SerializeField] private int _matchDamage = 1;
    [SerializeField] private int _coffeeMultiplier = 2;
    [SerializeField] private int _attackFullSetDamage = 1;
    [SerializeField] private int _healFullSetAmount = 1;
    [SerializeField] private int _tripleDamage = 3;

    public int MaxHealth => _maxHealth;
    public int MatchDamage => _matchDamage;
    public int CoffeeMultiplier => _coffeeMultiplier;
    public int AttackFullSetDamage => _attackFullSetDamage;
    public int HealFullSetAmount => _healFullSetAmount;
    public int TripleDamage => _tripleDamage;
}
