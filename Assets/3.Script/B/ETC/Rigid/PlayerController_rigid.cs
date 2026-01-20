using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class PlayerController_rigid : NetworkBehaviour
{
    [SerializeField] private Inputsystem _input;

    [Header("--- 이동 설정 ---")]
    [SyncVar] public float Speed = 9.5f;
    [SerializeField] private float forceSpeed = 0.3f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("--- 상태 변수 ---")]
    [SyncVar] public bool IsStunned = false;

    public Rigidbody Rb => rb;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>(); // 본체 RB 캐싱

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
            CheckAndCommandParticle();

            // [예측 적용] 1. 로컬 클라이언트 입력 처리
            if (_input != null && _input.move_input.sqrMagnitude >= 0.01f)
            {
                // 클라이언트는 '예측용 리지드바디'에 힘을 가함
                Rigidbody predictedRb = GetComponent<PredictedRigidbody>().predictedRigidbody;
                ApplyMovementLogic(predictedRb, _input.move_input);

                // 서버에게도 입력 전달
                CmdPlayermove(_input.move_input);
            }
        }

        // 스턴 상태 처리 (물리 정지)
        if (IsStunned)
        {
            // 스턴 시에는 양쪽 다 멈춰야 함
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (isLocalPlayer)
            {
                // 로컬이라면 예측용 RB도 멈춰줌
                GetComponent<PredictedRigidbody>().predictedRigidbody.linearVelocity = Vector3.zero;
                GetComponent<PredictedRigidbody>().predictedRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    [Command]
    private void CmdPlayermove(Vector2 input)
    {
        // [서버 적용] 2. 서버는 '실제 리지드바디'에 힘을 가함
        ApplyMovementLogic(rb, input);
    }

    // 물리 계산 로직 분리 (대상 RB를 매개변수로 받음)
    private void ApplyMovementLogic(Rigidbody targetRb, Vector2 input)
    {
        if (IsStunned || targetRb == null) return;

        Vector3 moveDirection = new Vector3(input.x, 0, input.y).normalized;
        float currentForwardSpeed = Vector3.Dot(targetRb.linearVelocity, moveDirection);

        if (currentForwardSpeed < Speed)
        {
            float speedDiff = Speed - currentForwardSpeed;
            targetRb.AddForce(moveDirection * speedDiff * 10f, ForceMode.Acceleration);
        }

        targetRb.AddForce(moveDirection * forceSpeed, ForceMode.Acceleration);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            targetRb.rotation = Quaternion.Slerp(targetRb.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed);
        }
    }

    #region 이동 Particle (기존 유지)
    [Header("--- 효과 설정 ---")]
    [SerializeField] private ParticleSystem moveParticleSystem;
    [SerializeField] private float particleThreshold = 4.0f;
    [SerializeField] private float groundCheckDistance = 1.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("--- 동기화 변수 ---")]
    [SyncVar(hook = nameof(OnMoveParticleChanged))]
    private bool _shouldShowParticle = false;

    private void OnMoveParticleChanged(bool oldVal, bool newVal)
    {
        if (moveParticleSystem == null) return;
        if (newVal)
        {
            moveParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            moveParticleSystem.Play();
        }
        else
        {
            if (moveParticleSystem.isPlaying) moveParticleSystem.Stop();
        }
    }

    private float _checkInterval = 0.2f;
    private float _nextCheckTime;

    private void CheckAndCommandParticle()
    {
        if (moveParticleSystem == null) return;
        if (Time.fixedTime < _nextCheckTime) return;
        _nextCheckTime = Time.fixedTime + _checkInterval;

        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, groundCheckDistance, groundLayer);

        // 파티클 체크 시 로컬은 예측용 RB 속도를 기준으로 판단하는 것이 자연스러움
        Rigidbody checkRb = GetComponent<PredictedRigidbody>().predictedRigidbody;
        float currentSpeed = checkRb.linearVelocity.magnitude;

        bool isRespawning = false;
        if (TryGetComponent(out PlayerRespawn_rigid res)) isRespawning = res.isRespawning;

        bool shouldShow = isGrounded && (currentSpeed > particleThreshold) && !IsStunned && !isRespawning;

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

    private void HandleMenu() => //Debug.Log("Menu");
}