using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    private PlayerController controller;
    private Rigidbody rb;

    [Header("--- VFX 연결 ---")]
    [Tooltip("[0]:Empty(IronBody), [1]:Nitro_Root, [2]:EMP_Blast(시전), [3]:Stun_Lightning(피격)")]
    // [개선] 스턴 당했을 때 머리 위 번개 효과를 위해 배열 크기를 4로 늘리는 것을 추천합니다.
    [SerializeField] private GameObject[] effectRoots;

    [Header("--- 밸런스 설정 (Iron Body) ---")]
    [SerializeField] private float ironDuration = 5f;
    [SerializeField] private float ironMassMultiplier = 5f;
    [SerializeField] private float ironScaleMultiplier = 1.5f;

    [Header("--- 밸런스 설정 (Nitro) ---")]
    [SerializeField] private float nitroDuration = 3f;
    [SerializeField] private float nitroSpeedMultiplier = 2.5f;
    [SerializeField] private float nitroImpulseForce = 50f;

    [Header("--- 밸런스 설정 (EMP) ---")]
    [SerializeField] private float empStunDuration = 2.0f;     // 스턴 지속 시간
    [SerializeField] private float empBlastVfxDuration = 2.0f; // 시전자 이펙트 잔존 시간

    // 복구용 기본 수치 저장
    private float defaultMass;
    private float defaultSpeed;
    private Vector3 defaultScale;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        rb = controller.Rb; //

        defaultMass = rb.mass;
        defaultSpeed = controller.Speed;
        defaultScale = transform.localScale;
    }

    [Server]
    public void Svr_ApplyItemEffect(int index)
    {
        switch (index)
        {
            case 0: StartCoroutine(IronBodyRoutine()); break; // Iron Body
            case 1: StartCoroutine(NitroChargeRoutine()); break; // Nitro
            case 2: Svr_UseEMP(); break; // EMP
        }

        TargetShowItemMessage(connectionToClient, index);
    }

    #region 아이템 스킬 구현 (로직)

    // [0: Iron Body]
    [Server]
    private IEnumerator IronBodyRoutine()
    {
        rb.mass = defaultMass * ironMassMultiplier;
        RpcSetScale(defaultScale * ironScaleMultiplier);

        yield return new WaitForSeconds(ironDuration);

        rb.mass = defaultMass;
        RpcSetScale(defaultScale);
    }

    // [1: Nitro]
    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        RpcControlEffect(1, true); // 부스터 이펙트 ON

        controller.Speed = defaultSpeed * nitroSpeedMultiplier;
        rb.AddForce(transform.forward * nitroImpulseForce, ForceMode.Impulse);

        yield return new WaitForSeconds(nitroDuration);

        RpcControlEffect(1, false); // 부스터 이펙트 OFF

        controller.Speed = defaultSpeed;
    }

    // [2: EMP]
    [Server]
    private void Svr_UseEMP()
    {
        // 1. 시전자: EMP 폭발 이펙트 (Index 2)
        RpcControlEffect(2, true);
        StartCoroutine(StopParticleDelay(2, empBlastVfxDuration));

        // 2. 피격자 검색 및 적용
        foreach (NetworkConnection conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                if (conn.identity.gameObject == gameObject) continue; // 나 자신은 제외

                var targetHandler = conn.identity.GetComponent<ItemEffectHandler>();
                if (targetHandler)
                {
                    // 상대방에게 스턴 및 번개 효과 적용
                    targetHandler.Svr_ApplyStun(empStunDuration);
                }
            }
        }
    }

    // 파티클 자동 꺼짐 예약
    private IEnumerator StopParticleDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        RpcControlEffect(index, false);
    }

    // [피격] 스턴 처리
    [Server]
    public void Svr_ApplyStun(float time) => StartCoroutine(StunRoutine(time));

    private IEnumerator StunRoutine(float time)
    {
        // 1. 상태 동기화 (이동 불가)
        controller.IsStunned = true; //

        // 2. 시각 효과 (만약 Index 3에 번개 이펙트가 있다면 켜줌)
        // 기존 배열 크기가 3이었다면 에러 방지를 위해 체크
        if (effectRoots.Length > 3) RpcControlEffect(3, true);

        yield return new WaitForSeconds(time);

        // 3. 해제
        if (effectRoots.Length > 3) RpcControlEffect(3, false);
        controller.IsStunned = false;
    }

    #endregion

    #region 클라이언트 시각/알림 처리

    [ClientRpc] private void RpcSetScale(Vector3 s) => transform.localScale = s;

    [ClientRpc]
    private void RpcControlEffect(int index, bool isPlaying)
    {
        if (effectRoots == null || index < 0 || index >= effectRoots.Length) return;

        GameObject rootObj = effectRoots[index];
        if (rootObj == null) return;

        if (isPlaying)
        {
            // [버그 수정의 핵심]
            // 부모를 먼저 켜야 GetComponentsInChildren이 자식 파티클을 찾을 수 있습니다.
            rootObj.SetActive(true);

            var childParticles = rootObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in childParticles)
            {
                ps.Play();
            }
        }
        else
        {
            // 꺼질 때는 파티클을 먼저 멈추고(잔상 유지)
            var childParticles = rootObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in childParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            // 상황에 따라 오브젝트 자체를 끌지, 파티클만 멈출지 결정
            // 여기서는 파티클 멈춤 명령만 내리고 오브젝트는 켜둡니다 (잔상 이슈 방지)
            // 만약 깔끔하게 사라져야 한다면 코루틴으로 지연 후 SetActive(false) 처리가 필요할 수 있음
        }
    }

    [TargetRpc]
    private void TargetShowItemMessage(NetworkConnection target, int index)
    {
        string[] itemNames = { "Iron Body", "Nitro", "EMP" };
        string msg = (index >= 0 && index < itemNames.Length) ? itemNames[index] : "Unknown";
        Debug.Log($"[아이템 획득] {msg} 사용!");
    }

    #endregion


}
