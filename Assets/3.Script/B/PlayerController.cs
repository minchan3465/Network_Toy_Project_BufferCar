using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Inputsystem _input; // 확인용 필드

    [Header("--- 이동 설정 ---")]
    // [남훈님] ItemEffectHandler에서 Speed를 조절을 위해public으로 변경 //+OK
    [SyncVar] public float Speed = 9.5f;
    [SerializeField] private float forceSpeed = 0.3f;
    [SerializeField] private float turnSpeed = 10f;  // 회전 속도

    [Header("--- 상태 변수 ---")]//[남훈님] EMP 피격 시 상태 동기화
    [SyncVar] public bool IsStunned = false;

    // [남훈님] 외부에서 Rigidbody에 접근하기 위한 프로퍼티
    public Rigidbody Rb => rb;
    private Rigidbody rb;

    private void Start()
    {
        if (!TryGetComponent(out rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        if (isLocalPlayer)
        {
            _input = FindAnyObjectByType<Inputsystem>();
            Camera_manager.instance.SetCamera(this.transform);
            if (_input != null) _input.ESCEvent += HandleMenu;
        }
    }

    #region 이동 로직
    private void Playermove()
    {
        if (_input == null || _input.move_input.sqrMagnitude < 0.01f) return;

        Vector3 moveDirection = new Vector3(_input.move_input.x, 0, _input.move_input.y).normalized;

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, moveDirection);

        if (currentForwardSpeed < Speed)
        {
            float speedDiff = Speed - currentForwardSpeed;
            rb.AddForce(moveDirection * speedDiff * 10f, ForceMode.Acceleration);
        }

        rb.AddForce(moveDirection * forceSpeed, ForceMode.Acceleration);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed);
        }
    }

    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            CheckAndCommandParticle();
        }
        if (IsStunned)
        {
            // 움직임을 확실히 멈추고 싶다면 속도 초기화??
            // [희수]+이러면 스턴 상태에선 충돌했을때 안밀리는 상태가 되려나요
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }
        Playermove();
    }
    #endregion

    #region 이동 Particle
    [Header("--- 효과 설정 ---")]//파티클 예시
    [SerializeField] private GameObject moveParticle; // 발밑 이동 불꽃같은거 파티클
    [SerializeField] private float particleThreshold = 4.0f; // 파티클이 나올 최소 속도
    [SerializeField] private float groundCheckDistance = 1f; // 바닥 감지 거리 (차 높이에 따라 조절)
    [SerializeField] private LayerMask groundLayer; // 바닥 레이어 (Inspector에서 설정 필수)
    [Header("--- 동기화 변수 ---")]
    [SyncVar(hook = nameof(OnMoveParticleChanged))]
    private bool _shouldShowParticle = false;

    // SyncVar Hook: 값이 변할 때 모든 클라이언트에서 실행됨

    private void OnMoveParticleChanged(bool oldVal, bool newVal)
    {
        if (moveParticle != null)
        {
            moveParticle.SetActive(newVal);
        }
    }

    private void CheckAndCommandParticle()
    {
        if (moveParticle == null) return;

        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
        float currentSpeed = rb.linearVelocity.magnitude;
        bool shouldShow = isGrounded && (currentSpeed > particleThreshold);

        // 현재 상태가 서버에 기록된 상태와 다를 때만 명령 보냄 (네트워크 트래픽 최적화)
        if (_shouldShowParticle != shouldShow)
        {
            CmdSetMoveParticle(shouldShow);
        }
    }

    [Command]
    private void CmdSetMoveParticle(bool state)
    {
        _shouldShowParticle = state; // 서버에서 값을 바꾸면 모든 클라이언트의 Hook 실행
    }
    #endregion

    #region input key 추가키 할당방식
    private void OnDisable()
    {
        if (isLocalPlayer && _input != null) _input.ESCEvent -= HandleMenu;
    }

    private void HandleMenu()
    {
        Debug.Log("ESC or Xbox Gamepad menu!");
    }
    #endregion
}
