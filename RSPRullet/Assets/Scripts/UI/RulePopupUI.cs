using UnityEngine;
using UnityEngine.UI;

// 규칙 팝업 (UI·UX 화면 흐름 세부 기획서 6-3장 규칙 팝업). 상단 정보 UI 왼쪽 아래 [규칙] 버튼으로 열고 [닫기]로 닫는다.
// 규칙 문구(한 판의 흐름 / 판정 / 카드 조합 / 아이템)는 Scene의 팝업 글자에 둔다.
// 규칙 버튼의 입력 잠금은 PhaseButtonsUI가 맡고, 이 클래스는 팝업 열기·닫기만 맡는다
public class RulePopupUI : MonoBehaviour
{
    [SerializeField] private Button _ruleButton;
    [SerializeField] private GameObject _rulePopup;
    [SerializeField] private Button _closeButton;

    private void Awake()
    {
        _ruleButton.onClick.AddListener(OpenPopup);
        _closeButton.onClick.AddListener(ClosePopup);
        _rulePopup.SetActive(false);
    }

    private void OnDestroy()
    {
        _ruleButton.onClick.RemoveListener(OpenPopup);
        _closeButton.onClick.RemoveListener(ClosePopup);
    }

    private void OpenPopup()
    {
        _rulePopup.SetActive(true);
    }

    private void ClosePopup()
    {
        _rulePopup.SetActive(false);
    }
}
