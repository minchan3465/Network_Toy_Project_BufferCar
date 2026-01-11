using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    [SerializeField]private Inputsystem _input;//확인용 필드
    private Rigidbody rb;

    [SerializeField] private float Speed = 8.5f;
    [SerializeField] private float forceSpeed = 0.3f;
    [SerializeField] private float turnSpeed = 10f;  // 회전 속도

    private void Start()
    {
        transform.TryGetComponent(out rb);
        if (isLocalPlayer)
        {
            _input = FindAnyObjectByType<Inputsystem>();
            if (_input != null) _input.ESCEvent += HandleMenu;
        }
    }

    private void Playermove()
    {
        if (_input == null || _input.move_input.sqrMagnitude < 0.01f) return;

        Vector3 moveDirection = new Vector3(_input.move_input.x, 0, _input.move_input.y).normalized;

        // 1. 현재 내 '입력 방향'으로의 속도만 측정합니다.
        // Dot(내적)을 사용하면 내가 가고자 하는 방향으로의 현재 속도 성분만 가져옵니다.
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, moveDirection);

        // 2. 입력에 의한 가속 (Speed까지만 힘을 보태줌)
        // 내가 이미 Speed보다 빠르게 날아가고 있다면(충돌 등으로), 이동 힘은 가하지 않습니다.
        if (currentForwardSpeed < Speed)
        {
            // 부족한 속도만큼만 보충 (Acceleration 모드는 질량 무시)
            float speedDiff = Speed - currentForwardSpeed;
            rb.AddForce(moveDirection * speedDiff * 10f, ForceMode.Acceleration);
        }

        // 3. 추가 가속도 (forceSpeed)
        // 이것도 Speed 제한 없이 더 빨라지게 하고 싶다면 if문 밖으로 뺍니다.
        rb.AddForce(moveDirection * forceSpeed, ForceMode.Acceleration);

        // 4. 회전
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed);
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        Playermove();
    }

    
    private void OnDisable()
    {
        if (isLocalPlayer&& _input != null) _input.ESCEvent -= HandleMenu;
    }
    
    private void HandleMenu()
    {
        Debug.Log("ESC 또는 패드 스타트 버튼이 눌렸습니다!");
    }
}
