using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 인트로·엔딩 화면 (RSP-80). 이미지를 차례로 정해진 시간씩 보여 주고, 다 보여 주면 다음 씬을 불러온다.
// 이미지마다 검은 화면에서 밝아졌다가 다시 검게 어두워진 뒤 다음 이미지로 넘어간다. 카메라 배경이 검정이라 이미지가 투명해지면 검은 화면이 된다.
// 인트로와 엔딩은 같은 스크립트를 쓰고 이미지·시간·다음 씬만 Inspector에서 다르게 넣는다.
// 배경음악은 같은 씬의 AudioSource가 씬 시작과 함께 재생한다(Play On Awake).
// 배경음악 파일은 "장당 시간 × 장 수" 길이로 잘라 두었으므로, 장당 시간을 바꾸면 음악 파일도 다시 잘라야 한다
public class StorySlidesUI : MonoBehaviour
{
    [SerializeField] private Image _slideImage;
    [SerializeField] private Sprite[] _slides;

    // 한 장을 보여 주는 전체 시간. 밝아지는 시간과 어두워지는 시간을 포함한다
    [SerializeField, Min(0.1f)] private float _secondsPerSlide = 4f;

    // 밝아지는 시간과 어두워지는 시간 (각각)
    [SerializeField, Min(0f)] private float _fadeSeconds = 0.5f;

    // 빌드 목록(File > Build Profiles)에 같은 이름의 씬이 있어야 한다
    [SerializeField] private string _nextSceneName;

    private void Start()
    {
        SetBrightness(0f);
        StartCoroutine(PlaySlides());
    }

    private IEnumerator PlaySlides()
    {
        // 장당 시간이 페이드 두 번보다 짧으면 페이드를 장당 시간의 절반으로 줄인다
        float fadeSeconds = Mathf.Min(_fadeSeconds, _secondsPerSlide / 2f);
        float holdSeconds = _secondsPerSlide - fadeSeconds * 2f;

        foreach (Sprite slide in _slides)
        {
            _slideImage.sprite = slide;
            yield return StartCoroutine(Fade(0f, 1f, fadeSeconds));
            yield return new WaitForSeconds(holdSeconds);
            yield return StartCoroutine(Fade(1f, 0f, fadeSeconds));
        }

        SceneManager.LoadScene(_nextSceneName);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            SetBrightness(Mathf.Lerp(from, to, elapsed / seconds));
            yield return null;
        }

        SetBrightness(to);
    }

    // brightness 0: 이미지가 투명해 검은 화면, 1: 이미지가 다 보임
    private void SetBrightness(float brightness)
    {
        Color color = _slideImage.color;
        color.a = brightness;
        _slideImage.color = color;
    }
}
