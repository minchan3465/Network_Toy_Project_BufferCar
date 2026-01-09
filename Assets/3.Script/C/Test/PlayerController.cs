using UnityEngine;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    [Header("--- 물리 및 이동 설정 ---")]
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float turnSpeed = 150f;

    [Header("--- 아이템 에셋 ---")]
    [SerializeField] private GameObject oilPrefab;

    private Rigidbody rb;

    // 복구를 위한 원본 데이터 저장용 변수들
    private float originalMass;           // <--- 추가됨
    private float originalLinearDamping;
    private float originalAngularDamping;
    private Vector3 originalScale;

    private float currentMaxSpeed;        // 니트로 차지 등 가변 속도 대응용
    private bool isSlipping = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        originalMass = rb.mass;           // <--- 초기화 추가
        originalLinearDamping = rb.linearDamping;
        originalAngularDamping = rb.angularDamping;
        originalScale = transform.localScale;

        currentMaxSpeed = maxSpeed;       // 초기 속도 설정
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if (!isSlipping)
        {
            HandleMovement();
        }
        else
        {
            // 미끄러짐 연출
            rb.AddTorque(Vector3.up * 50f, ForceMode.Acceleration);
        }
    }

    private void HandleMovement()
    {
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        if (Mathf.Abs(moveInput) > 0.1f)
        {
            rb.AddForce(transform.forward * moveInput * acceleration, ForceMode.Acceleration);
        }

        transform.Rotate(0, turnInput * turnSpeed * Time.deltaTime, 0);

        // maxSpeed 대신 currentMaxSpeed를 사용하여 아이템 효과 반영
        if (rb.linearVelocity.magnitude > currentMaxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
        }
    }

    [Server]
    public void Svr_ApplyItemEffect(int index)
    {
        switch (index)
        {
            case 0: StartCoroutine(IronBodyRoutine()); break;
            case 1: StartCoroutine(NitroChargeRoutine()); break;
            case 2: StartCoroutine(MeteorSmashRoutine()); break;
            case 3: StartCoroutine(OilSlickRoutine()); break;
            case 4: Svr_EMPShock(); break;
        }
    }

    #region 아이템 상세 로직

    [Server]
    private IEnumerator IronBodyRoutine()
    {
        rb.mass = originalMass * 5f; // 원본 질량의 5배
        RpcSetScale(originalScale * 1.5f);
        yield return new WaitForSeconds(5f);

        rb.mass = originalMass;      // 원본 질량으로 정확히 복구
        RpcSetScale(originalScale);
    }

    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        currentMaxSpeed = maxSpeed * 2.5f; // 속도 제한 해제
        rb.AddForce(transform.forward * 50f, ForceMode.Impulse);
        yield return new WaitForSeconds(3f);
        currentMaxSpeed = maxSpeed;        // 원복
    }

    [Server]
    private IEnumerator MeteorSmashRoutine()
    {
        rb.isKinematic = true;
        transform.position += Vector3.up * 10f;
        yield return new WaitForSeconds(0.5f);
        rb.isKinematic = false;
        rb.AddForce(Vector3.down * 50f, ForceMode.Impulse);

        yield return new WaitForSeconds(0.2f);
        Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject && hit.attachedRigidbody)
                hit.attachedRigidbody.AddExplosionForce(1000f, transform.position, 5f);
        }
    }

    [Server]
    private IEnumerator OilSlickRoutine()
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject oil = Instantiate(oilPrefab, transform.position - transform.forward, Quaternion.identity);
            NetworkServer.Spawn(oil);
            yield return new WaitForSeconds(0.5f);
        }
    }

    [Server]
    private void Svr_EMPShock()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, 15f);
        foreach (var enemy in enemies)
        {
            if (enemy.gameObject != gameObject && enemy.CompareTag("Player"))
            {
                var targetController = enemy.GetComponent<PlayerController>();
                if (targetController) targetController.Svr_ApplyEmpDebuff(3f);
            }
        }
    }

    [Server]
    public void Svr_ApplyEmpDebuff(float time) => StartCoroutine(EmpRoutine(time));

    private IEnumerator EmpRoutine(float time)
    {
        rb.linearDamping = 15f;
        yield return new WaitForSeconds(time);
        rb.linearDamping = originalLinearDamping;
    }

    [Server]
    public void Svr_StartSlip(float time) => StartCoroutine(SlipRoutine(time));

    private IEnumerator SlipRoutine(float time)
    {
        isSlipping = true;
        RpcSyncSlip(true);
        yield return new WaitForSeconds(time);
        isSlipping = false;
        RpcSyncSlip(false);
    }

    [ClientRpc] private void RpcSetScale(Vector3 s) => transform.localScale = s;
    [ClientRpc] private void RpcSyncSlip(bool b) => isSlipping = b;

    #endregion
}

