using UnityEngine;
using UnityEngine.UI;

// 버튼 잠금 표시. 잠긴 버튼은 이미지와 글자를 함께 흐리게 해서 누를 수 없음을 보여준다 (UI·UX 화면 흐름 세부 기획서 6-2장 입력 잠금).
// Unity 버튼의 상태 전환은 이미지 하나에만 적용되므로, 버튼에 붙인 CanvasGroup의 투명도로 글자까지 함께 흐리게 한다
public static class ButtonLock
{
    public const float LockedAlpha = 0.4f;

    public static void SetLocked(Button button, bool isLocked)
    {
        button.interactable = !isLocked;
        if (button.TryGetComponent(out CanvasGroup group))
        {
            group.alpha = isLocked ? LockedAlpha : 1f;
        }
    }
}
