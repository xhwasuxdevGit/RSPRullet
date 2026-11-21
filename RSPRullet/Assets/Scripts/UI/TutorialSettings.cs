using UnityEngine;

// 1라운드 튜토리얼 설정 (튜토리얼(1라운드) 세부 기획서 6-2장 안내 데이터). 에셋은 하나만 만든다.
// 안내 문구는 코드에 쓰지 않고 이 에셋의 목록에서 인스펙터로 고친다. 단계마다 항목을 하나씩 넣는다
[CreateAssetMenu(fileName = "TutorialSettings", menuName = "RSPRullet/Tutorial Settings")]
public class TutorialSettings : ScriptableObject
{
    [SerializeField] private TutorialGuideEntry[] _entries;

    // 단계에 맞는 항목. 목록에 없으면 null이다
    public TutorialGuideEntry GetEntry(TutorialStep step)
    {
        foreach (TutorialGuideEntry entry in _entries)
        {
            if (entry.Step == step) { return entry; }
        }

        return null;
    }
}
