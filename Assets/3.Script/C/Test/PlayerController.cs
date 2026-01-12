using UnityEngine;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    [Header("--- 이동 설정 ---")]
    public float maxSpeed = 10f;
    public float turnSpeed = 150f;

    [Header("--- 상태 변수 ---")]
    // [SyncVar] 서버가 이 값을 true로 바꾸면, 해당 클라이언트는 즉시 입력을 차단함
    [SyncVar] public bool IsStunned = false;

    public Rigidbody Rb { get; private set; }

    void Awake()
    {
        Rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // 1. 내 캐릭터가 아니면 연산하지 않음
        if (!isLocalPlayer) return;

        // 2. [입력 차단] 스턴 상태라면 입력 자체를 처리하지 않음 (서버로 데이터 전송 안 됨)
        if (IsStunned)
        {
            // 움직이던 관성이 남지 않게 정지 (선택 사항)
            Rb.linearVelocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            return;
        }

        // 3. 정상 이동 로직
        HandleMovement();
    }

    private void HandleMovement()
    {
        float moveInput = Input.GetAxis("Vertical");     // W, S
        float turnInput = Input.GetAxis("Horizontal");   // A, D

        transform.Rotate(0, turnInput * turnSpeed * Time.deltaTime, 0);

        Vector3 newVelocity = transform.forward * moveInput * maxSpeed;
        Rb.linearVelocity = new Vector3(newVelocity.x, Rb.linearVelocity.y, newVelocity.z);
    }
}

