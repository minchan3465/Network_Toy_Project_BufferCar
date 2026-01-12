using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    // ▼ [신규] 밸런스 조절용 데이터 구조체 (인스펙터 노출)
    [System.Serializable]
    public struct IronBodyStats
    {
        [Tooltip("효과 지속 시간 (초)")] public float duration;
        [Tooltip("무거워지는 배율 (기본값의 n배)")] public float massMultiplier;
        [Tooltip("커지는 배율 (기본값의 n배)")] public float scaleMultiplier;
    }

    [System.Serializable]
    public struct NitroStats
    {
        [Tooltip("효과 지속 시간 (초)")] public float duration;
        [Tooltip("속도 증가 배율 (기본값의 n배)")] public float speedMultiplier;
        [Tooltip("순간 가속 힘 (Impulse)")] public float impulseForce;
    }

    [System.Serializable]
    public struct EmpStats
    {
        [Tooltip("스턴 지속 시간 (초)")] public float stunDuration;
    }

    [Header("--- 밸런스 설정 (Balance Settings) ---")]
    [SerializeField] public IronBodyStats ironBody = new IronBodyStats { duration = 5f, massMultiplier = 5f, scaleMultiplier = 1.5f };
    [SerializeField] public NitroStats nitro = new NitroStats { duration = 3f, speedMultiplier = 2.5f, impulseForce = 50f };
    [SerializeField] public EmpStats emp = new EmpStats { stunDuration = 0.2f };

    // 내부 참조 변수
    private PlayerController controller;
    private Rigidbody rb;

    // 복구용 기본 수치 저장
    private float defaultMass;
    private float defaultSpeed;
    private Vector3 defaultScale;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        rb = controller.Rb;

        // 게임 시작 시점의 기본값 저장
        defaultMass = rb.mass;
        defaultSpeed = controller.Speed;
        defaultScale = transform.localScale;
    }

    // ★ 아이템 박스가 호출하는 진입점
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

    #region 아이템 스킬 구현 (Serialize 적용)

    // [0번] Iron Body
    [Server]
    private IEnumerator IronBodyRoutine()
    {
        // 주인 클라이언트에게 변신 명령
        TargetApplyIronBody(connectionToClient, true);

        // 직렬화된 duration 사용
        yield return new WaitForSeconds(ironBody.duration);

        // 복구 명령
        TargetApplyIronBody(connectionToClient, false);
    }

    [TargetRpc]
    private void TargetApplyIronBody(NetworkConnection target, bool active)
    {
        if (active)
        {
            // 직렬화된 수치 적용
            rb.mass = defaultMass * ironBody.massMultiplier;
            transform.localScale = defaultScale * ironBody.scaleMultiplier;
        }
        else
        {
            rb.mass = defaultMass;
            transform.localScale = defaultScale;
        }
    }

    // [1번] Nitro
    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        TargetApplyNitro(connectionToClient, true);

        // 직렬화된 duration 사용
        yield return new WaitForSeconds(nitro.duration);

        TargetApplyNitro(connectionToClient, false);
    }

    [TargetRpc]
    private void TargetApplyNitro(NetworkConnection target, bool active)
    {
        if (active)
        {
            // 직렬화된 수치 적용
            controller.Speed = defaultSpeed * nitro.speedMultiplier;
            rb.AddForce(transform.forward * nitro.impulseForce, ForceMode.Impulse);
        }
        else
        {
            controller.Speed = defaultSpeed;
        }
    }

    // [2번] EMP
    [Server]
    private void Svr_GlobalStun()
    {
        foreach (NetworkConnection conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                if (conn.identity.gameObject == gameObject) continue;

                var targetHandler = conn.identity.GetComponent<ItemEffectHandler>();
                if (targetHandler)
                {
                    // 직렬화된 stunDuration 사용
                    targetHandler.Svr_ApplyStun(emp.stunDuration);
                }
            }
        }
    }

    [Server]
    public void Svr_ApplyStun(float time) => StartCoroutine(StunRoutine(time));

    private IEnumerator StunRoutine(float time)
    {
        controller.IsStunned = true;
        yield return new WaitForSeconds(time);
        controller.IsStunned = false;
    }

    #endregion

    #region UI 및 알림

    [TargetRpc]
    private void TargetShowItemMessage(NetworkConnection target, int index)
    {
        string[] itemNames = { "Iron Body", "Nitro", "EMP" };
        string msg = (index >= 0 && index < itemNames.Length) ? itemNames[index] : "Unknown";
        Debug.Log($"[아이템 획득] {msg} 사용!");
    }

    #endregion
}
