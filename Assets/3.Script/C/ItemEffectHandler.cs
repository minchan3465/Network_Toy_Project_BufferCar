using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    private PlayerController controller;
    private Rigidbody rb;

    // 복구용 기본 수치
    private float defaultMass;
    private float defaultMaxSpeed;
    private Vector3 defaultScale;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        rb = controller.Rb;

        defaultMass = rb.mass;
        defaultMaxSpeed = controller.maxSpeed;
        defaultScale = transform.localScale;
    }

    [Server]
    public void Svr_ApplyItemEffect(int index)
    {
        switch (index)
        {
            case 0: StartCoroutine(IronBodyRoutine()); break;
            case 1: StartCoroutine(NitroChargeRoutine()); break;
            // 2번 EMP: 전체 공격
            case 2: Svr_GlobalStun(); break;
        }

        TargetShowItemMessage(connectionToClient, index);
    }

    #region 아이템 스킬 구현

    // [EMP] 모든 플레이어 이동 차단
    [Server]
    private void Svr_GlobalStun()
    {
        // 서버의 모든 연결(Connection)을 순회
        foreach (NetworkConnection conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                // 시전자는 제외
                if (conn.identity.gameObject == gameObject) continue;

                var targetHandler = conn.identity.GetComponent<ItemEffectHandler>();
                if (targetHandler)
                {
                    targetHandler.Svr_ApplyStun(0.2f);
                }
            }
        }
    }

    [Server]
    public void Svr_ApplyStun(float time) => StartCoroutine(StunRoutine(time));

    private IEnumerator StunRoutine(float time)
    {
        // SyncVar 값을 변경 -> 클라이언트에게 자동 전파 -> 클라이언트 입력 차단됨
        controller.IsStunned = true;

        yield return new WaitForSeconds(time);

        controller.IsStunned = false;
    }

    // [Iron Body] 무게 증가
    [Server]
    private IEnumerator IronBodyRoutine()
    {
        rb.mass = defaultMass * 5f;
        RpcSetScale(defaultScale * 1.5f);
        yield return new WaitForSeconds(5f);
        rb.mass = defaultMass;
        RpcSetScale(defaultScale);
    }

    // [Nitro] 속도 증가
    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        controller.maxSpeed = defaultMaxSpeed * 2.5f;
        rb.AddForce(transform.forward * 50f, ForceMode.Impulse);
        yield return new WaitForSeconds(3f);
        controller.maxSpeed = defaultMaxSpeed;
    }

    #endregion

    #region 클라이언트 시각 처리

    [ClientRpc] private void RpcSetScale(Vector3 s) => transform.localScale = s;

    [TargetRpc]
    private void TargetShowItemMessage(NetworkConnection target, int index)
    {
        string[] itemNames = { "Iron Body", "Nitro", "EMP" };
        string msg = (index >= 0 && index < itemNames.Length) ? itemNames[index] : "Unknown";
        Debug.Log($"[아이템 획득] {msg} 사용!");
    }

    #endregion
}
