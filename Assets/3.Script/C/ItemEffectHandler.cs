using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    private PlayerController controller;
    private Rigidbody rb;

    // 복구용 기본 수치 저장
    private float defaultMass;
    private float defaultSpeed; // 변수명 변경 (MaxSpeed -> Speed)
    private Vector3 defaultScale;

    void Start()
    {
        controller = GetComponent<PlayerController>();

        // PlayerController에서 추가한 프로퍼티(Rb)를 통해 접근
        rb = controller.Rb;

        defaultMass = rb.mass;
        defaultSpeed = controller.Speed; // PlayerController.Speed 저장
        defaultScale = transform.localScale;
    }

    [Server]
    public void Svr_ApplyItemEffect(int index)
    {
        switch (index)
        {
            case 0: StartCoroutine(IronBodyRoutine()); break;
            case 1: StartCoroutine(NitroChargeRoutine()); break;
            case 2: Svr_GlobalStun(); break; // EMP
        }

        TargetShowItemMessage(connectionToClient, index);
    }

    #region 아이템 스킬 구현

    // [EMP] 맵에 있는 모든 플레이어(자신 제외) 스턴
    [Server]
    private void Svr_GlobalStun()
    {
        foreach (NetworkConnection conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                // 자기 자신은 제외
                if (conn.identity.gameObject == gameObject) continue;

                var targetHandler = conn.identity.GetComponent<ItemEffectHandler>();
                if (targetHandler)
                {
                    // 0.2초 스턴 부여
                    targetHandler.Svr_ApplyStun(0.2f);
                }
            }
        }
    }

    [Server]
    public void Svr_ApplyStun(float time) => StartCoroutine(StunRoutine(time));

    private IEnumerator StunRoutine(float time)
    {
        // PlayerController의 SyncVar 변수를 제어 -> 클라이언트 FixedUpdate 멈춤
        controller.IsStunned = true;

        yield return new WaitForSeconds(time);

        controller.IsStunned = false;
    }

    // [Iron Body] 질량 증가 및 크기 변화
    [Server]
    private IEnumerator IronBodyRoutine()
    {
        rb.mass = defaultMass * 5f;
        RpcSetScale(defaultScale * 1.5f);
        yield return new WaitForSeconds(5f);
        rb.mass = defaultMass;
        RpcSetScale(defaultScale);
    }

    // [Nitro] 이동 속도(Speed) 증가
    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        // PlayerController.Speed를 조작
        controller.Speed = defaultSpeed * 2.5f;
        rb.AddForce(transform.forward * 50f, ForceMode.Impulse); // 순간 가속

        yield return new WaitForSeconds(3f);

        controller.Speed = defaultSpeed; // 원상 복구
    }

    #endregion

    #region 클라이언트 시각/알림 처리

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
