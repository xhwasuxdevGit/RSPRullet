using System.Collections;
using UnityEngine;

// 테이블 카드 자리 하나(PFB_TableCard)의 움직임 연출: 이동, 곡선 이동, 3D 뒤집기, 들어 올리기, 강조
// (연출·아트·사운드 세부 기획서 3장 카드 연출, 8장 P2·P3·P5).
// 메서드는 모두 코루틴이다. 부르는 쪽은 yield return StartCoroutine(…)으로 끝날 때까지 기다리거나,
// StartCoroutine(…)만 불러 다음 연출과 겹쳐 출발시킨다.
// 이동은 항상 "어디선가 출발해 이 자리(씬에 놓아 둔 위치)로 도착"한다. 카드 자리는 움직이지 않고 그 자리에 보일 카드만 바뀌기 때문이다.
// 어떤 그림으로 뒤집을지와 시간 값은 부르는 쪽(TableCardsUI)이 정해서 넘긴다.
// 같은 오브젝트의 CardView로 뒤집기 중간에 그림을 바꾼다
public class CardMotion : MonoBehaviour
{
    // 카드가 테이블에 누워 있을 때 카드 면의 회전 (카드 면은 위를 향한다)
    private static readonly Quaternion FlatFaceRotation = Quaternion.Euler(90f, 0f, 0f);

    // 뒤집기 1박자의 절반에서 카드가 옆으로 선 각도. 이때 그림을 바꾸면 바뀌는 순간이 보이지 않는다
    private const float FlipHalfAngle = 90f;

    // 뒤집으며 세운 카드의 가장자리와 테이블 사이에 두는 여유(월드 단위)
    private const float FlipClearance = 0.005f;

    // 카드 그림과 알고 있음 표시(눈 아이콘·문구)를 함께 담은 카드 면. 기울기·뒤집기·강조 때 표시도 카드와 같이 움직여
    // 어느 각도에서든 눈 아이콘은 카드 위 바깥, 문구는 아래 바깥에 붙어 있다 (UI·UX 화면 흐름 세부 기획서 4-1장)
    [SerializeField] private Transform _face;

    // 강조 효과로 들썩이는 높이(월드 단위)와 커지는 배율
    [SerializeField, Min(0f)] private float _highlightHeight = 0.04f;
    [SerializeField, Min(1f)] private float _highlightScale = 1.1f;

    // 곡선 이동에서 카드가 옆으로 돌아가는 최대 폭(월드 단위)
    [SerializeField, Min(0f)] private float _curveWidth = 0.15f;

    private CardView _view;
    private Vector3 _homeLocalPosition;
    private Vector3 _faceHomeScale;

    private void Awake()
    {
        _view = GetComponent<CardView>();
        _homeLocalPosition = transform.localPosition;
        _faceHomeScale = _face.localScale;
    }

    // 직선 이동: from에서 이 자리까지 미끄러져 온다 (연출·아트·사운드 세부 기획서 3-4장 분배 이동)
    public IEnumerator MoveFrom(Vector3 fromWorldPosition, float seconds) => Move(fromWorldPosition, seconds, 0f);

    // 곡선 이동: from에서 이 자리까지 옆으로 돌아서 온다 (연출·아트·사운드 세부 기획서 3-5장 교환).
    // 서로 반대 방향으로 오는 두 카드에 같은 값을 주면, 진행 방향 기준으로 같은 쪽(오른쪽)으로 돌아서 화면에서는 서로 엇갈린다
    public IEnumerator CurveFrom(Vector3 fromWorldPosition, float seconds) => Move(fromWorldPosition, seconds, _curveWidth);

    // 3D 뒤집기 1박자: 카드 면을 세로축으로 90° 세운 순간 그림을 바꾸고 다시 눕힌다 (연출·아트·사운드 세부 기획서 3-6장).
    // 테이블에 누운 카드를 세우면 폭의 절반이 테이블 아래로 들어가므로, 세우는 만큼 카드를 띄워 가장자리가 테이블 위에 머물게 한다
    public IEnumerator Flip(Sprite nextSprite, bool showsKnownMarker, float seconds)
    {
        Quaternion startRotation = _face.localRotation;
        Vector3 startLocalPosition = transform.localPosition;
        float riseHeight = _view.CardHalfWidth + FlipClearance;
        float halfSeconds = seconds * 0.5f;

        // 코루틴 안에서 다른 코루틴 메서드를 yield return 하면 그 회전이 끝날 때까지 기다린 뒤 다음 줄로 간다
        yield return RotateFace(startRotation, startLocalPosition, riseHeight, 0f, FlipHalfAngle, halfSeconds);
        _view.Show(nextSprite, showsKnownMarker);
        yield return RotateFace(startRotation, startLocalPosition, riseHeight, FlipHalfAngle, 0f, halfSeconds);

        _face.localRotation = startRotation;
        transform.localPosition = startLocalPosition;
    }

    // 강조 1박자: 이미 앞면인 카드가 제자리에서 들썩이며 잠깐 커진다 (연출·아트·사운드 세부 기획서 3-6장, 1장 강조 효과)
    public IEnumerator Highlight(float seconds)
    {
        Vector3 homePosition = transform.localPosition;
        Vector3 bigScale = _faceHomeScale * _highlightScale;

        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            // 0에서 올라갔다가 0으로 돌아오는 값
            float rise = Mathf.Sin(Mathf.PI * elapsed / seconds);
            transform.position = transform.parent.TransformPoint(homePosition) + Vector3.up * (_highlightHeight * rise);
            _face.localScale = Vector3.Lerp(_faceHomeScale, bigScale, rise);
            yield return null;
        }

        transform.localPosition = homePosition;
        _face.localScale = _faceHomeScale;
    }

    // 들어 올리기: 더미 위 from에서 누운 채로 출발해, 이 자리로 떠오르면서 딜러 쪽(테이블 위쪽) 가장자리가 tiltDegrees만큼 들린 경사로 기운다
    // (연출·아트·사운드 세부 기획서 3-3장 뽑기). 누가 뽑았든 같은 경사이고, 플레이어에게 보이는 그림만 다르다.
    // revealSprite가 있으면 떠오르는 동안 뒤집기 1박자와 같은 방식(세로축으로 90°까지 세웠다가 눕힘)으로 그 그림으로 뒤집는다.
    // 플레이어가 뽑은 카드는 뒷면으로 출발해 앞면으로 뒤집히고, 딜러가 뽑은 카드는 플레이어가 모르므로 뒷면 그대로다
    public IEnumerator LiftFrom(Vector3 fromWorldPosition, float tiltDegrees, Sprite revealSprite, bool showsKnownMarker, float seconds)
    {
        // 누운 카드 면(90°)에서 각도를 줄이면 딜러 쪽 가장자리가 들리고 카드 면이 플레이어를 향한다
        Quaternion tiltedRotation = Quaternion.Euler(90f - tiltDegrees, 0f, 0f);
        Vector3 toPosition = transform.parent.TransformPoint(_homeLocalPosition);
        bool isRevealed = revealSprite == null;

        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float progress = elapsed / seconds;
            float t = Mathf.SmoothStep(0f, 1f, progress);
            transform.position = Vector3.Lerp(fromWorldPosition, toPosition, t);

            // 절반까지는 세로축으로 세우고, 절반에서 그림을 바꾼 뒤 다시 눕힌다. 그림을 바꾸는 순간 카드가 옆면이라 바뀌는 것이 보이지 않는다
            float flipAngle = revealSprite == null ? 0f : FlipHalfAngle * (1f - Mathf.Abs(progress * 2f - 1f));

            if (!isRevealed && progress >= 0.5f)
            {
                _view.Show(revealSprite, showsKnownMarker);
                isRevealed = true;
            }

            _face.rotation = Quaternion.Slerp(FlatFaceRotation, tiltedRotation, t) * Quaternion.Euler(0f, flipAngle, 0f);
            yield return null;
        }

        if (!isRevealed) { _view.Show(revealSprite, showsKnownMarker); }

        transform.localPosition = _homeLocalPosition;
        _face.rotation = tiltedRotation;
    }

    // from에서 이 자리까지 옮긴다. curveWidth가 0보다 크면 진행 방향의 오른쪽으로 부풀어 돌아온다
    private IEnumerator Move(Vector3 fromWorldPosition, float seconds, float curveWidth)
    {
        Vector3 toPosition = transform.parent.TransformPoint(_homeLocalPosition);
        Vector3 direction = toPosition - fromWorldPosition;
        // 위쪽과 진행 방향의 외적은 테이블 위에서 진행 방향의 오른쪽을 가리킨다
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;

        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            Vector3 curveOffset = side * (curveWidth * Mathf.Sin(Mathf.PI * t));
            transform.position = Vector3.Lerp(fromWorldPosition, toPosition, t) + curveOffset;
            yield return null;
        }

        transform.localPosition = _homeLocalPosition;
    }

    // 카드 면을 startRotation 기준으로 세로축(카드 그림의 위아래 축)을 따라 fromAngle에서 toAngle까지 돌린다
    // 세운 각도에 맞춰 카드를 riseHeight까지 띄운다 (90°에서 가장 높다)
    private IEnumerator RotateFace(Quaternion startRotation, Vector3 startLocalPosition, float riseHeight,
        float fromAngle, float toAngle, float seconds)
    {
        Vector3 startPosition = transform.parent.TransformPoint(startLocalPosition);
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float angle = Mathf.Lerp(fromAngle, toAngle, elapsed / seconds);
            _face.localRotation = startRotation * Quaternion.Euler(0f, angle, 0f);
            transform.position = startPosition + Vector3.up * (riseHeight * Mathf.Sin(angle * Mathf.Deg2Rad));
            yield return null;
        }
    }
}
