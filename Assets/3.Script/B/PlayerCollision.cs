using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerCollision : NetworkBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float pushForce = 12; // 밀어내는 힘의 세기
    private Inputsystem input;

    private bool isPushing = false;
    [SerializeField] private float pushCooldown = 0.2f;

    public override void OnStartLocalPlayer()
    {
        // 내 캐릭터가 네트워크상에서 준비되었을 때 딱 한 번만 호출
        // 따라서 Start에서 if(isLocalPlayer)를 쓰는 것보다 명확
        input = FindAnyObjectByType<Inputsystem>();
        rb = GetComponent<Rigidbody>();
        Debug.Log($"[LocalPlayer] {name} 준비 완료. RB: {rb != null}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isLocalPlayer || isPushing) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            NetworkIdentity targetIdentity = collision.gameObject.GetComponent<NetworkIdentity>();

            if (targetIdentity != null)
            {
                Debug.Log("상대방과 부딪힘!");

                // 미는 방향
                Vector3 dirToTarget = (collision.transform.position - transform.position).normalized;

                // 서버에 나(netIdentity)와 상대(targetIdentity) 모두를 밀어달라고 요청
                CmdPushBoth(netIdentity, targetIdentity, dirToTarget * pushForce);
            }
        }
    }

    [Command]
    public void CmdPushBoth(NetworkIdentity self, NetworkIdentity target, Vector3 force)
    {
        // 1. 나에게 반대 방향으로 힘 적용
        if (self.TryGetComponent(out PlayerCollision selfCol))
            selfCol.RpcApplyImpulse(-force);

        // 2. 상대방에게 정방향으로 힘 적용
        if (target.TryGetComponent(out PlayerCollision targetCol))
            targetCol.RpcApplyImpulse(force);
    }

    [ClientRpc]
    public void RpcApplyImpulse(Vector3 force)
    {
        if (!isLocalPlayer) return;

        Debug.Log($"{name} RPC 실행됨. IsLocal: {isLocalPlayer}");
        // 각 플레이어의 화면에서 실행
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"{name}의 Rigidbody를 찾을 수 없음!");
            return;
        }

        // 조작 일시 정지
        if (isLocalPlayer && input != null) input.Enter();

        StartCoroutine(PushCooldownRoutine());

        // 물리 적용
        //Vector3 currentVel = rb.linearVelocity;
        //rb.linearVelocity = new Vector3(0, currentVel.y, 0);
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force + Vector3.up * 2f, ForceMode.Impulse); // 살짝 띄워줌
        Debug.Log($"힘 적용 완료 {name}에게 {force}만큼의 힘을 가함");
    }

    private IEnumerator PushCooldownRoutine()
    {
        isPushing = true;
        yield return new WaitForSeconds(pushCooldown);
        isPushing = false;
    }
}
