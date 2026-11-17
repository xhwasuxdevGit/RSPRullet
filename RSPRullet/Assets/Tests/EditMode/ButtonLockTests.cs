using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// 잠긴 버튼은 누를 수 없고 흐리게 보인다 (UI·UX 화면 흐름 세부 기획서 6-2장 입력 잠금)
public class ButtonLockTests
{
    private GameObject _gameObject;
    private Button _button;
    private CanvasGroup _group;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("Button", typeof(Image), typeof(Button), typeof(CanvasGroup));
        _button = _gameObject.GetComponent<Button>();
        _group = _gameObject.GetComponent<CanvasGroup>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void SetLocked_True_DisablesAndFades()
    {
        ButtonLock.SetLocked(_button, true);

        Assert.IsFalse(_button.interactable);
        Assert.AreEqual(ButtonLock.LockedAlpha, _group.alpha, 0.0001f);
    }

    [Test]
    public void SetLocked_False_EnablesAndRestoresAlpha()
    {
        ButtonLock.SetLocked(_button, true);
        ButtonLock.SetLocked(_button, false);

        Assert.IsTrue(_button.interactable);
        Assert.AreEqual(1f, _group.alpha, 0.0001f);
    }
}
