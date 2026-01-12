using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Inputsystem _input; // 확인용 필드
    private Rigidbody rb;

    [Header("--- 이동 설정 ---")]
    // [수정] ItemEffectHandler에서 속도를 조절해야 하므로 public으로 변경
    public float Speed = 9.5f;
    [SerializeField] private float forceSpeed = 0.3f;
    [SerializeField] private float turnSpeed = 10f;  // 회전 속도

    [Header("--- 상태 변수 ---")]
    // [추가] EMP 피격 시 상태 동기화
    [SyncVar] public bool IsStunned = false;

    // [추가] 외부에서 Rigidbody에 접근하기 위한 프로퍼티
    public Rigidbody Rb => rb;

    private void Start()
    {
        // rb 할당 (TryGetComponent 대신 GetComponent가 더 명확할 수 있음)
        if (!TryGetComponent(out rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        if (isLocalPlayer)
        {
            _input = FindAnyObjectByType<Inputsystem>();
            if (_input != null) _input.ESCEvent += HandleMenu;
        }
    }

    // 실제 물리 이동 로직
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
        if (!isLocalPlayer) return;

        // 스턴 상태라면 이동 로직 차단
        if (IsStunned)
        {
            // 움직임을 확실히 멈추고 싶다면 속도 초기화 (선택 사항)
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        Playermove();
    }

    private void OnDisable()
    {
        if (isLocalPlayer && _input != null) _input.ESCEvent -= HandleMenu;
    }

    private void HandleMenu()
    {
        Debug.Log("ESC or Xbox Gamepad menu!");
    }
}
