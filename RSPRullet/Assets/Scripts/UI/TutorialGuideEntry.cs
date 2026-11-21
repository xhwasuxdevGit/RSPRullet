using UnityEngine;

// 튜토리얼 안내 말풍선 하나의 데이터 (튜토리얼(1라운드) 세부 기획서 6-2장 안내 데이터).
// TutorialSettings 에셋 안에 목록으로 들어간다.
// [System.Serializable]은 ScriptableObject가 아닌 이 작은 클래스도 인스펙터에 펼쳐 보이고 에셋에 저장되게 하는 표시다
[System.Serializable]
public class TutorialGuideEntry
{
    [SerializeField] private TutorialStep _step;

    // [TextArea]는 인스펙터의 입력 칸을 여러 줄로 보여 준다. 말풍선은 최대 4줄이다 (튜토리얼(1라운드) 세부 기획서 2-5장)
    [SerializeField, TextArea(3, 5)] private string _text;

    // 안내를 띄운 화면이 플레이어의 클릭을 기다린 뒤 넘어갈지. 버튼을 눌러야 넘어가는 화면은 기다리지 않는다
    // (튜토리얼(1라운드) 세부 기획서 2-3장)
    [SerializeField] private bool _waitsForClick;

    public TutorialStep Step => _step;
    public string Text => _text;
    public bool WaitsForClick => _waitsForClick;
}
