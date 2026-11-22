using UnityEngine;

// 진행 안내 배너 하나의 문구 데이터 (RSP-86). BannerSettings 에셋 안에 목록으로 들어간다.
// [System.Serializable]은 ScriptableObject가 아닌 이 작은 클래스도 인스펙터에 펼쳐 보이고 에셋에 저장되게 하는 표시다
[System.Serializable]
public class BannerEntry
{
    [SerializeField] private BannerType _type;

    // {0} 자리에 라운드 배너는 라운드 번호, 카드 섞기 배너는 셔플 번호가 들어간다 (BannerRules.FormatText).
    // TMP 태그(<size=60%> 등)와 줄바꿈을 쓸 수 있다. {0}이 아닌 중괄호는 쓰지 않는다
    [SerializeField, TextArea(1, 3)] private string _text;

    // 켜면 역할의 글자색 대신 아래 글자색을 쓴다. 예: 카드 섞기는 상단 정보 UI의 카드 섞기 글자와 같은 색
    [SerializeField] private bool _usesCustomTextColor;
    [SerializeField] private Color _textColor = Color.white;

    public BannerType Type => _type;
    public string Text => _text;
    public bool UsesCustomTextColor => _usesCustomTextColor;
    public Color TextColor => _textColor;
}
