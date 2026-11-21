using UnityEngine;

// 딜러 얼굴이 메인 카메라(플레이어)를 마주 보도록 머리·상체 방향을 맞춘다 (RSP-98).
// 대기 애니메이션의 머리는 테이블 쪽을 보므로, 애니메이션 위에 시선 IK를 덧입힌다.
// 딜러 모델(Animator가 있는 오브젝트)에 붙이며, 딜러 Animator Controller의 Base Layer에서
// IK Pass가 켜져 있어야 동작한다. 시선은 Play 중에만 적용된다 (정지 상태의 Scene 뷰에서는 원래 자세로 보인다)
public class DealerLookAt : MonoBehaviour
{
    // 딜러가 바라볼 대상. 보통 Main Camera를 넣는다
    [SerializeField] private Transform _lookTarget;

    // 바라볼 위치를 대상 기준으로 옮기는 값 (월드 좌표, 미터). 시선을 조금 위·아래·옆으로 옮길 때 쓴다
    [SerializeField] private Vector3 _targetOffset = Vector3.zero;

    // 시선 전체 세기. 0이면 애니메이션 그대로, 1이면 대상을 완전히 바라본다
    [SerializeField, Range(0f, 1f)] private float _weight = 1f;

    // 상체가 시선을 따라 도는 정도. 머리만 크게 꺾이면 조금 올린다
    [SerializeField, Range(0f, 1f)] private float _bodyWeight = 0.2f;

    // 머리가 시선을 따라 도는 정도
    [SerializeField, Range(0f, 1f)] private float _headWeight = 1f;

    // 돌릴 수 있는 각도 제한. 0이면 제한 없음, 1이면 돌리지 않음. 0.5는 정면 기준 약 90도까지
    [SerializeField, Range(0f, 1f)] private float _clampWeight = 0.5f;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    // Animator가 자세를 계산할 때 매 프레임 호출된다
    private void OnAnimatorIK(int layerIndex)
    {
        if (_animator == null || _lookTarget == null) { return; }

        _animator.SetLookAtWeight(_weight, _bodyWeight, _headWeight, 0f, _clampWeight);
        _animator.SetLookAtPosition(_lookTarget.position + _targetOffset);
    }
}
