using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Inputsystem _input;

    [Header("이동 설정")]
    [SyncVar] public float Speed = 9.5f;
    [SerializeField] private float forceSpeed = 0.3f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("상태 변수")]
    [SyncVar] public bool IsStunned = false;

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
            if (Camera_manager.instance != null) Camera_manager.instance.SetCamera(this.transform);
            if (_input != null) _input.ESCEvent += HandleMenu;
        }
    }

    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            CheckAndCommandParticle();// 이동속도+땅 체크로  파티클 실행
        }

        if (IsStunned)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }
        Playermove();
    }

    private void Playermove()
    {
        //이동 최솟값
        if (_input == null || _input.move_input.sqrMagnitude < 0.01f) return;
        //Vector2를 Vector3로 바꾸면서 x,z 방향 이동
        Vector3 moveDirection = new Vector3(_input.move_input.x, 0, _input.move_input.y).normalized;
        //Dot은 수학의 내적. 두 벡터를 비교해서 둘이 얼마나 같은 방향을 보고 있는지 체크
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

    #region 이동 Particle
    [Header("효과 설정")]
    [SerializeField] private ParticleSystem moveParticleSystem;
    [SerializeField] private float particleThreshold = 4.0f;
    [SerializeField] private float groundCheckDistance = 1.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("동기화 변수")]
    [SyncVar(hook = nameof(OnMoveParticleChanged))]
    private bool _shouldShowParticle = false;

    private void OnMoveParticleChanged(bool oldVal, bool newVal)
    {
        if (moveParticleSystem == null) return;

        if (newVal)
        {
            //재생 전 Clear를 통해 파티클 시스템 초기화 (재시작 보장)
            moveParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            moveParticleSystem.Play();
        }
        else
        {
            if (moveParticleSystem.isPlaying) moveParticleSystem.Stop();
        }
    }

    private float _checkInterval = 0.2f; // 사용자 요청에 따라 0.2초 유지
    private float _nextCheckTime;

    private void CheckAndCommandParticle()
    {
        if (moveParticleSystem == null) return;

        // 인터벌 체크
        if (Time.fixedTime < _nextCheckTime) return;
        _nextCheckTime = Time.fixedTime + _checkInterval;

        // 땅 체크 레이캐스트로.
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, groundCheckDistance, groundLayer);

        // 속도 체크
        float currentSpeed = rb.linearVelocity.magnitude;

        // 리스폰 체크(리스폰 중에는 파티클이 안나오겠지만)
        bool isRespawning = false;
        if (TryGetComponent(out PlayerRespawn res)) isRespawning = res.isRespawning;

        // 최종 상태 bool값
        bool shouldShow = isGrounded && (currentSpeed > particleThreshold) && !IsStunned && !isRespawning;

        // 상태가 변할 때만 Command 호출
        // 멈췄을 때 false로 변해야, 다음에 출발할 때 true로 인식되어 Hook이 실행됨
        if (_shouldShowParticle != shouldShow)
        {
            CmdSetMoveParticle(shouldShow);
        }
    }

    [Command]
    private void CmdSetMoveParticle(bool state)
    {
        _shouldShowParticle = state;
    }
    #endregion

    private void OnDisable()
    {
        if (isLocalPlayer && _input != null) _input.ESCEvent -= HandleMenu;
    }

    private void HandleMenu() => Debug.Log("Menu");
}