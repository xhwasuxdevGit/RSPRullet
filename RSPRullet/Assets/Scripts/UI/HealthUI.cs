using TMPro;
using UnityEngine;

// 한쪽(플레이어 또는 딜러)의 체력: 현재 체력 숫자와, 바뀔 때 숫자 옆에 잠깐 보여 주는 변화량
// (UI·UX 화면 흐름 세부 기획서 2-2장 딜러 체력·하단 HUD 영역, 5-7장 체력).
// 최대 체력은 표시하지 않는다. 딜러 체력과 내 체력은 Scene에서 같은 x(1920×1080 기준 700)에 둔다.
// 진행 흐름(GameFlow)에 체력 변경 이벤트가 없고 변화량 표시 시간도 재야 해서 매 프레임 상태를 읽는다.
// 정산은 양쪽 조합 효과를 합산해 한 번에 반영하므로(HealthState.ApplySettlement) 변화량도 한 번만 나온다
public class HealthUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private Side _side;
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _changeText;

    // 변화량을 보여 주는 시간(초). 기획서에 값이 없어 판-07 승패 판정 표시 시간(1.6초) 안에서 사라지도록 시작값을 둔다
    [SerializeField, Min(0f)] private float _changeShowSeconds = 1.2f;

    // 감소는 빨강, 회복은 초록 (UI·UX 화면 흐름 세부 기획서 5-7장 체력)
    [SerializeField] private Color _damageColor = new Color(0.85f, 0.25f, 0.25f);
    [SerializeField] private Color _healColor = new Color(0.25f, 0.7f, 0.35f);

    private GameFlow _flow;
    private int _shownHealth;
    private float _changeTimeLeft;

    private void Awake()
    {
        _flow = _runner.Flow;

        // 시작 체력은 변화가 아니므로 변화량 없이 숫자만 보여 준다
        _shownHealth = _flow.GetHealth(_side);
        _healthText.text = _shownHealth.ToString();
        _changeText.gameObject.SetActive(false);
    }

    private void Update()
    {
        int health = _flow.GetHealth(_side);
        int change = health - _shownHealth;
        if (HealthChangeRules.IsShown(change))
        {
            _shownHealth = health;
            _healthText.text = health.ToString();
            ShowChange(change);
        }

        if (_changeTimeLeft <= 0f)
        {
            return;
        }

        _changeTimeLeft -= Time.deltaTime;
        if (_changeTimeLeft <= 0f)
        {
            _changeText.gameObject.SetActive(false);
        }
    }

    private void ShowChange(int change)
    {
        _changeText.text = HealthChangeRules.GetChangeText(change);
        _changeText.color = HealthChangeRules.IsHeal(change) ? _healColor : _damageColor;
        _changeText.gameObject.SetActive(true);
        _changeTimeLeft = _changeShowSeconds;
    }
}
