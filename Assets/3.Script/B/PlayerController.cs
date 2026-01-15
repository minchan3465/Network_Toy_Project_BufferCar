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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }
        Playermove();
    }
    #endregion

    #region 이동 Particle
    [Header("--- 효과 설정 ---")]//파티클 예시
    [SerializeField] private ParticleSystem moveParticleSystem;
    [SerializeField] private float particleThreshold = 4.0f; // 파티클이 나올 최소 속도
    [SerializeField] private float groundCheckDistance = 1f; // 바닥 감지 거리 (차 높이에 따라 조절)
    [SerializeField] private LayerMask groundLayer; // 바닥 레이어 (Inspector에서 설정 필수)
    [Header("--- 동기화 변수 ---")]
    [SyncVar(hook = nameof(OnMoveParticleChanged))]
    private bool _shouldShowParticle = false;

    // SyncVar Hook: 값이 변할 때 모든 클라이언트에서 실행됨

    private void OnMoveParticleChanged(bool oldVal, bool newVal)
    {
        if (moveParticleSystem == null) return;

        if (newVal)
        {
            // 재생 중이 아닐 때만 Play 호출 (중복 방지)
            if (!moveParticleSystem.isPlaying) moveParticleSystem.Play();
        }
        else
        {
            // 재생 중일 때만 Stop 호출 (중복 방지)
            // Stop()을 하면 새로 생성만 안 될 뿐, 이미 나온 입자는 수명만큼 유지됨
            if (moveParticleSystem.isPlaying) moveParticleSystem.Stop();
        }
    }

    private float _checkInterval = 0.2f; // 0.2초마다 검사 연산량 줄임
    private float _nextCheckTime;
    private void CheckAndCommandParticle()
    {
        if (moveParticleSystem == null) return;

        if (Time.fixedTime < _nextCheckTime) return;
        _nextCheckTime = Time.fixedTime + _checkInterval;

        // 땅에 닿아 있는지 체크
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
        // 일정 속도 이상인지 체크
        float currentSpeed = rb.linearVelocity.magnitude;

        // 땅에 닿음 + 속도 기준 통과 + 스턴 아님 -> Play 조건
        bool shouldShow = isGrounded && (currentSpeed > particleThreshold) && !IsStunned;

        // 상태가 바뀔 때만 서버에 보고 (네트워크 최적화)
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
