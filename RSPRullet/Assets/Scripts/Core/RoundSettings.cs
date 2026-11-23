using UnityEngine;

// 라운드별 밸런스 값 (코어 규칙 상세 기획서 7-1-1장, 게임 컨셉 기획서 2장). 라운드마다 에셋을 하나씩 만든다.
[CreateAssetMenu(fileName = "RoundSettings", menuName = "RSPRullet/Round Settings")]
public class RoundSettings : ScriptableObject
{
    // 몇 번째 라운드인지 (UI·UX 화면 흐름 세부 기획서 5-1장 상단 정보 UI의 라운드 표시). 라운드 에셋마다 1·2·3을 넣는다
    [SerializeField, Min(1)] private int _roundNumber = 1;
    [SerializeField] private int _startHealth = 3;
    [SerializeField] private int _itemCapacity;
    [SerializeField] private int _itemsPerShuffle;

    // 라운드 시작 지급은 돋보기만 고정 개수로 준다 (아이템 세부 기획서 4-2장 지급 시점)
    [SerializeField] private int _startMagnifierCount;

    // 조합 칸의 패턴 이름 안내를 보여 줄지. 플레이어·딜러 양쪽 조합 칸에 함께 적용한다 (UI·UX 화면 흐름 세부 기획서 5-6장 조합 칸).
    // 기본은 보여 주고, 3라운드 에셋에서만 끈다 (RSP-107)
    [SerializeField] private bool _showsCombinationHint = true;

    public int RoundNumber => _roundNumber;
    public int StartHealth => _startHealth;
    public int ItemCapacity => _itemCapacity;
    public int ItemsPerShuffle => _itemsPerShuffle;
    public int StartMagnifierCount => _startMagnifierCount;
    public bool ShowsCombinationHint => _showsCombinationHint;
}
