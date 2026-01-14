using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    private PlayerController controller;
    private Rigidbody rb;

    [Header("--- VFX 연결 ---")]
    [Tooltip("[0]:Empty, [1]:Nitro, [2]:EMP_Cast, [3]:Stun_Hit")]
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
    [SerializeField] private float empStunDuration = 2.0f;
    [SerializeField] private float empBlastVfxDuration = 2.0f;

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
            case 0: StartCoroutine(IronBodyRoutine()); break;
            case 1: StartCoroutine(NitroChargeRoutine()); break;
            case 2: Svr_UseEMP(); break;
        }

        TargetShowItemMessage(connectionToClient, index);
    }

    #region 아이템 스킬 구현 (로직)

    [Server]
    private IEnumerator IronBodyRoutine()
    {
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("Power UpSFX", transform.position, 1.0f);

        rb.mass = defaultMass * ironMassMultiplier;
        RpcSetScale(defaultScale * ironScaleMultiplier);

        yield return new WaitForSeconds(ironDuration);

        rb.mass = defaultMass;
        RpcSetScale(defaultScale);
    }

    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        RpcControlEffect(1, true);

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("Burst_ImpactSFX", transform.position, 1.0f);

        controller.Speed = defaultSpeed * nitroSpeedMultiplier;
        rb.AddForce(transform.forward * nitroImpulseForce, ForceMode.Impulse);

        yield return new WaitForSeconds(nitroDuration);

        RpcControlEffect(1, false);

        controller.Speed = defaultSpeed;
    }

    // [핵심 수정] EMP 로직 변경: 모든 PlayerController를 직접 찾습니다.
    [Server]
    private void Svr_UseEMP()
    {
        Debug.Log($"[EMP] {name}가 EMP 발동!"); // 디버그 로그 1

        RpcControlEffect(2, true);
        StartCoroutine(StopParticleDelay(2, empBlastVfxDuration));

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f);

        // 1. 씬에 있는 모든 PlayerController를 찾습니다. (연결 루프보다 확실함)
        // Unity 2023+ (Unity 6) 기준: FindObjectsByType
        // 이전 버전이라면: FindObjectsOfType<PlayerController>()
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController target in allPlayers)
        {
            // 2. 나 자신은 제외
            if (target.gameObject == gameObject) continue;

            // 3. 대상의 핸들러 가져오기
            var targetHandler = target.GetComponent<ItemEffectHandler>();
            if (targetHandler != null)
            {
                Debug.Log($"[EMP] 타겟 발견: {target.name} -> 스턴 적용 시도"); // 디버그 로그 2
                targetHandler.Svr_ApplyStun(empStunDuration);
            }
        }
    }

    private IEnumerator StopParticleDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        RpcControlEffect(index, false);
    }

    private Coroutine currentStunCoroutine;

    [Server]
    public void Svr_ApplyStun(float time)
    {
        // 1. 이미 스턴 타이머가 돌고 있다면? -> 강제 종료! (초기화)
        // 이걸 안 하면 "1초 남은 이전 타이머"가 방금 건 20초짜리 스턴을 1초 뒤에 풀어버립니다.
        if (currentStunCoroutine != null)
        {
            StopCoroutine(currentStunCoroutine);
        }

        // 2. 새로운 타이머 시작
        currentStunCoroutine = StartCoroutine(StunRoutine(time));
    }

    private IEnumerator StunRoutine(float time)
    {
        Debug.Log($"<color=red>[EMP] 스턴 시작! 유지 시간: {time}초</color>");

        // [핵심 변경] IsStunned만 켜는 게 아니라, 컨트롤러 자체를 잠시 끕니다.
        // 이렇게 하면 PlayerController의 FixedUpdate가 안 돌아가서 속도 0 고정이 발생하지 않습니다.
        controller.IsStunned = true; // 상태 표시는 유지 (애니메이션 등을 위해)
        controller.enabled = false;  // <--- 이것이 마법의 코드입니다.

        // [디테일] 조종이 꺼지면 얼음판처럼 미끄러지므로, 저항을 높여서 엔진 꺼진 차처럼 만듭니다.
        float originalDrag = rb.linearDamping; // (Unity 6 기준, 구버전은 drag)
        rb.linearDamping = 2.0f;

        // 피격 효과음 및 이펙트
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f);

        if (effectRoots.Length > 3) RpcControlEffect(3, true);

        // --- 대기 ---
        yield return new WaitForSeconds(time);
        // -----------

        // 해제 로직
        if (effectRoots.Length > 3) RpcControlEffect(3, false);

        // [원상 복구]
        controller.IsStunned = false;
        controller.enabled = true; // 다시 켜서 조종 가능하게 함
        rb.linearDamping = originalDrag; // 저항 복구

        currentStunCoroutine = null;
        Debug.Log($"<color=green>[EMP] 스턴 해제 완료</color>");
    }

    #endregion

    #region 클라이언트 시각 처리

    [ClientRpc] private void RpcSetScale(Vector3 s) => transform.localScale = s;

    [ClientRpc]
    private void RpcControlEffect(int index, bool isPlaying)
    {
        if (effectRoots == null || index < 0 || index >= effectRoots.Length) return;
        GameObject rootObj = effectRoots[index];
        if (rootObj == null) return;

        if (isPlaying)
        {
            rootObj.SetActive(true);
            var childParticles = rootObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in childParticles) ps.Play();
        }
        else
        {
            var childParticles = rootObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in childParticles) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
