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
        HandleMovementEffects();//여기서 파티클 실행해주시면 됩니다 이동 자체는 여러 클라이언트가 다 봐야됍니다

        if (!isLocalPlayer) return;
        // 스턴 상태라면 이동 로직 차단
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
    [SerializeField] private ParticleSystem moveParticle; // 발밑 이동 불꽃같은거 파티클
    [SerializeField] private float particleThreshold = 2.0f; // 파티클이 나올 최소 속도

    private void HandleMovementEffects()
    {
        if (moveParticle == null) return;

        // 현재 실제 물리 속도(크기)를 계산
        float currentSpeed = rb.linearVelocity.magnitude;

        // 일정 속도 이상이면
        if (currentSpeed > particleThreshold)
        {
            if (!moveParticle.isPlaying) moveParticle.Play();
        }
        else
        {
            if (moveParticle.isPlaying) moveParticle.Stop();
        }
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
