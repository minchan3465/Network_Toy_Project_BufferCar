using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerCollision : NetworkBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float pushForce = 19; // 밀어내는 힘의 세기
    private Inputsystem input;

    [SyncVar] private bool isPushing = false;
    [SerializeField] private double pushCooldown = 0.2f;

    public override void OnStartLocalPlayer()
    {
        // 내 캐릭터가 네트워크상에서 준비되었을 때 딱 한 번만 호출
        // 따라서 Start에서 if(isLocalPlayer)를 쓰는 것보다 명확
        input = FindAnyObjectByType<Inputsystem>();
        rb = GetComponent<Rigidbody>();
        Debug.Log($"[LocalPlayer] {name} Ready. RB: {rb != null}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isLocalPlayer) return;

        if (NetworkTime.time < lastPushTime + pushCooldown) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (isPushing) return;
            NetworkIdentity targetIdentity = collision.gameObject.GetComponent<NetworkIdentity>();

            if (targetIdentity != null)
            {
                Debug.Log("OnCollisionEnter!");

                // 미는 방향
                Vector3 dirToTarget = (collision.transform.position - transform.position).normalized;

                // 서버에 나(netIdentity)와 상대(targetIdentity) 모두를 밀어달라고 요청
                CmdPushBoth(netIdentity, targetIdentity, dirToTarget * pushForce);
            }
        }
    }

    [SyncVar] private double lastPushTime; // 서버 시간 기록

    [Command]
    public void CmdPushBoth(NetworkIdentity self, NetworkIdentity target, Vector3 force)
    {
        if (NetworkTime.time < lastPushTime + pushCooldown) return;

        lastPushTime = NetworkTime.time; // 현재 서버 시간 저장

        // [수정] 서버에서 두 플레이어의 상태를 모두 체크
        if (this.isPushing) return;

        if (target.TryGetComponent(out PlayerCollision targetCol))
        {
            if (targetCol.isPushing)
            {
                Debug.Log("Target is already being pushed. Ignore.");
                return;
            }

            this.isPushing = true;
            targetCol.isPushing = true;

            // TargetRpc는 첫 번째 인자로 해당 클라이언트의 Connection을 받아야 합니다.
            this.RpcApplyImpulse(-force); // 본인은 뒤로 살짝 (작용 반작용)
            targetCol.RpcApplyImpulse(force);    // 상대는 앞으로

            // 일정 시간 후 서버에서 변수만 false로 돌려줌
            Invoke(nameof(ServerResetPushStatus), (float)pushCooldown);
            targetCol.Invoke(nameof(targetCol.ServerResetPushStatus), (float)pushCooldown);
        }
    }

    [Server]
    private void ServerResetPushStatus()
    {
        this.isPushing = false;
    }

    [TargetRpc]
    public void RpcApplyImpulse(Vector3 force)
    {
        //if (!isLocalPlayer) return;

        Debug.Log($"{name} RPC execution. IsLocal: {isLocalPlayer}");
        // 각 플레이어의 화면에서 실행
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"can't find {name}'s Rigidbody!");
            return;
        }

        // 조작 일시 정지
        if (input != null) input.Enter();

        //StartCoroutine(PushCooldownRoutine());

        // 물리 적용
        //Vector3 currentVel = rb.linearVelocity;
        //rb.linearVelocity = new Vector3(0, currentVel.y, 0);
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force + Vector3.up * 2f, ForceMode.Impulse); // 살짝 띄워줌
        Debug.Log($"Addforce {force} power to {name} player");
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때 변수를 초기화하여 다음 활성화 때 버그 방지
        isPushing = false;
    }
}
