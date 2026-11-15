using UnityEngine;

// 라운드별 밸런스 값 (코어 규칙 상세 기획서 7-1-1장, 게임 컨셉 기획서 2장). 라운드마다 에셋을 하나씩 만든다.
[CreateAssetMenu(fileName = "RoundSettings", menuName = "RSPRullet/Round Settings")]
public class RoundSettings : ScriptableObject
{
    [SerializeField] private int _startHealth = 3;
    [SerializeField] private int _itemCapacity;
    [SerializeField] private int _itemsPerShuffle;

    // 아이템 데이터(RSP-51)가 생기기 전까지 시작 아이템은 돋보기 개수로만 둔다
    [SerializeField] private int _startMagnifierCount;

    public int StartHealth => _startHealth;
    public int ItemCapacity => _itemCapacity;
    public int ItemsPerShuffle => _itemsPerShuffle;
    public int StartMagnifierCount => _startMagnifierCount;
}
