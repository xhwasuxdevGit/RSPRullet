using UnityEngine;

// 라운드 동안의 양쪽 체력. 한 판의 판정(코어 규칙 상세 기획서 3장, 7-2장 F6)과 정산(4-3장, F8)으로만 바뀐다.
// 카드 셔플이 바뀌어도 체력은 이어지므로, 카드 셔플마다 새로 만드는 ShuffleTable과 따로 두고 라운드마다 새로 만든다.
public class HealthState
{
    private readonly RuleSettings _rules;
    private int _playerHealth;
    private int _dealerHealth;

    // 라운드 종료 신호. 진행 단계의 CompleteShowResult에 넘긴다
    public bool IsAnyoneAtZero => _playerHealth == GameRules.MinHealth || _dealerHealth == GameRules.MinHealth;

    // startHealth는 라운드 설정(RoundSettings.StartHealth)에서 읽은 값
    public HealthState(RuleSettings rules, int startHealth)
    {
        _rules = rules;
        _playerHealth = startHealth;
        _dealerHealth = startHealth;
    }

    public int GetHealth(Side side)
    {
        return side == Side.Player ? _playerHealth : _dealerHealth;
    }

    // 한 판의 승패 판정. 진 쪽 체력이 기본 데미지만큼 줄고, 비기면 변화 없다. 결과는 플레이어 기준이다.
    // 커피는 이긴 쪽이 썼을 때만 데미지에 배수를 곱하고, 진 쪽이나 비겼을 때 쓴 커피는 효과 없이 사라진다 (S3 커피 이슈에서 연결)
    public MatchResult ApplyMatch(HandShape playerShape, HandShape dealerShape, bool playerUsedCoffee = false, bool dealerUsedCoffee = false)
    {
        MatchResult playerResult = HandShapeRules.Compare(playerShape, dealerShape);
        string matchLog = $"{DisplayNames.GetShapeName(playerShape)} vs {DisplayNames.GetShapeName(dealerShape)}";
        if (playerResult == MatchResult.Draw)
        {
            Debug.Log($"[HealthState] {matchLog} → 무승부 → 체력 변화 없음");
            return playerResult;
        }

        Side loser = playerResult == MatchResult.Win ? Side.Dealer : Side.Player;
        bool winnerUsedCoffee = loser == Side.Dealer ? playerUsedCoffee : dealerUsedCoffee;
        int damage = winnerUsedCoffee ? _rules.MatchDamage * _rules.CoffeeMultiplier : _rules.MatchDamage;
        SetHealth(loser, Mathf.Max(GetHealth(loser) - damage, GameRules.MinHealth));

        string resultName = playerResult == MatchResult.Win ? "승리" : "패배";
        Debug.Log($"[HealthState] {matchLog} → {resultName} → {DisplayNames.GetSideName(loser)} 체력 -{damage}");
        return playerResult;
    }

    // 정산 결과 적용. 양쪽 조합 효과를 대상별로 합산해 한 번에 반영한 뒤 0 하한과 최대 체력 상한을 적용한다.
    // 차례로 반영하면 순서에 따라 결과가 달라지므로 반드시 합산 후 반영한다.
    // 둘 다 0이면 둘 다 1로 되돌리고 계속한다 (예외 E4). 한쪽만 0이면 라운드가 끝나므로 true를 돌려준다
    public bool ApplySettlement(CombinationType playerCombination, CombinationType dealerCombination)
    {
        int playerChange = GetSelfEffect(playerCombination) + GetOpponentEffect(dealerCombination);
        int dealerChange = GetSelfEffect(dealerCombination) + GetOpponentEffect(playerCombination);
        _playerHealth = Mathf.Clamp(_playerHealth + playerChange, GameRules.MinHealth, _rules.MaxHealth);
        _dealerHealth = Mathf.Clamp(_dealerHealth + dealerChange, GameRules.MinHealth, _rules.MaxHealth);

        if (_playerHealth == GameRules.MinHealth && _dealerHealth == GameRules.MinHealth)
        {
            _playerHealth = GameRules.HealthWhenBothZero;
            _dealerHealth = GameRules.HealthWhenBothZero;
            Debug.Log("[HealthState] 정산: 양쪽 모두 체력 0 → 둘 다 체력 1로 두고 계속");
        }

        Debug.Log($"[HealthState] 정산: 플레이어 {playerCombination}, 딜러 {dealerCombination} → 플레이어 체력 {_playerHealth}, 딜러 체력 {_dealerHealth}");
        return IsAnyoneAtZero;
    }

    // 조합 완성 효과 중 자신에게 적용되는 효과: 회복형 풀세트만 해당한다
    private int GetSelfEffect(CombinationType combination)
    {
        return combination == CombinationType.HealFullSet ? _rules.HealFullSetAmount : 0;
    }

    // 조합 완성 효과 중 상대에게 적용되는 효과: 트리플과 공격형 풀세트
    private int GetOpponentEffect(CombinationType combination)
    {
        switch (combination)
        {
            case CombinationType.Triple:
                return -_rules.TripleDamage;
            case CombinationType.AttackFullSet:
                return -_rules.AttackFullSetDamage;
            default:
                return 0;
        }
    }

    private void SetHealth(Side side, int health)
    {
        if (side == Side.Player)
        {
            _playerHealth = health;
            return;
        }

        _dealerHealth = health;
    }
}
