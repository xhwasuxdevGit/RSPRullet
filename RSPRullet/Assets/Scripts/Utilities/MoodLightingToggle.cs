using UnityEngine;
using UnityEngine.Rendering;

// 테스트용 분위기 조명 켜기/끄기 (RSP-82).
// 연출 조명(황동색 스포트라이트, 분위기)은 연출·아트·사운드 세부 기획서 4장 10번에서 여유 연출이므로,
// 확정 전까지 Scene에서 이 오브젝트를 켜고 꺼서 비교한다.
// 자식의 스포트라이트·Global Volume은 오브젝트와 함께 켜지고 꺼지며,
// 오브젝트로 켜고 끌 수 없는 카메라 배경·환경광·주 조명 세기는 이 컴포넌트가 바꾼다.
// 에디터 정지 상태에서도 바로 보이도록 [ExecuteAlways]로 둔다
[ExecuteAlways]
public class MoodLightingToggle : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Light _mainLight;

    [Header("켰을 때")]
    [SerializeField] private Color _moodBackgroundColor = new Color(0.03f, 0.025f, 0.02f);
    [SerializeField] private Color _moodAmbientColor = new Color(0.10f, 0.085f, 0.075f);
    [SerializeField, Min(0f)] private float _moodMainLightIntensity = 0.15f;

    [Header("껐을 때 (기존 Scene 값)")]
    [SerializeField] private Color _defaultAmbientColor = new Color(0.212f, 0.227f, 0.259f);
    [SerializeField, Min(0f)] private float _defaultMainLightIntensity = 1f;

    private void OnEnable()
    {
        // 컴포넌트를 막 붙여 참조가 비어 있을 때는 아무것도 바꾸지 않는다
        if (_camera == null || _mainLight == null)
        {
            return;
        }

        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = _moodBackgroundColor;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = _moodAmbientColor;
        _mainLight.intensity = _moodMainLightIntensity;
    }

    private void OnDisable()
    {
        if (_camera == null || _mainLight == null)
        {
            return;
        }

        _camera.clearFlags = CameraClearFlags.Skybox;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientLight = _defaultAmbientColor;
        _mainLight.intensity = _defaultMainLightIntensity;
    }
}
