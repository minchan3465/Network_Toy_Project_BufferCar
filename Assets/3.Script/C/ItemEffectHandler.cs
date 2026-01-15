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

    [Header("--- 사운드 설정 ---")]
    [Tooltip("아이템 효과음 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1.0f; // [추가됨] 인스펙터 제어용

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
        rb = controller.Rb;

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
            SoundManager.instance.PlaySFXPoint("Power UpSFX", transform.position, 1.0f, sfxVolume);

        float heavyMass = defaultMass * ironMassMultiplier;

        // 1. 서버에서의 질량 변경
        rb.mass = heavyMass;

        // 2. [추가] 클라이언트에게도 질량 바꾸라고 명령
        RpcSetMass(heavyMass);

        RpcSetScale(defaultScale * ironScaleMultiplier);

        yield return new WaitForSeconds(ironDuration);

        // 3. 서버 질량 원상복구
        rb.mass = defaultMass;

        // 4. [추가] 클라이언트 질량 원상복구
        RpcSetMass(defaultMass);

        RpcSetScale(defaultScale);
    }


    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        RpcControlEffect(1, true);

        // [수정] 매개변수 4개 (이름, 위치, 더미값, 볼륨배율)
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("Burst_ImpactSFX", transform.position, 1.0f, sfxVolume);

        controller.Speed = defaultSpeed * nitroSpeedMultiplier;
        rb.AddForce(transform.forward * nitroImpulseForce, ForceMode.Impulse);

        yield return new WaitForSeconds(nitroDuration);

        RpcControlEffect(1, false);

        controller.Speed = defaultSpeed;
    }

    [Server]
    private void Svr_UseEMP()
    {
        Debug.Log($"[EMP] {name}가 EMP 발동!");

        RpcControlEffect(2, true);
        StartCoroutine(StopParticleDelay(2, empBlastVfxDuration));

        
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f, sfxVolume);

        
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController target in allPlayers)
        {
            if (target.gameObject == gameObject) continue;

            var targetHandler = target.GetComponent<ItemEffectHandler>();
            if (targetHandler != null)
            {
                Debug.Log($"[EMP] 타겟 발견: {target.name} -> 스턴 적용 시도");
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
        if (currentStunCoroutine != null)
        {
            StopCoroutine(currentStunCoroutine);
        }
        currentStunCoroutine = StartCoroutine(StunRoutine(time));
    }

    private IEnumerator StunRoutine(float time)
    {
        Debug.Log($"<color=red>[EMP] 스턴 시작! 유지 시간: {time}초</color>");

        controller.IsStunned = true;
        controller.enabled = false;
        RpcSetControllerState(false);

        float originalDrag = rb.linearDamping;

        // [수정 1] 서버 마찰력 변경
        rb.linearDamping = 2.0f;

        // [수정 2] 클라이언트에게도 마찰력 2.0으로 바꾸라고 명령!
        RpcSetDrag(2.0f);

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f, sfxVolume);

        if (effectRoots.Length > 3) RpcControlEffect(3, true);

        yield return new WaitForSeconds(time);

        if (effectRoots.Length > 3) RpcControlEffect(3, false);

        controller.IsStunned = false;
        controller.enabled = true;
        RpcSetControllerState(true);

        // [수정 3] 서버 마찰력 복구
        rb.linearDamping = originalDrag;

        // [수정 4] 클라이언트에게도 원래대로 돌려놓으라고 명령!
        RpcSetDrag(originalDrag);

        currentStunCoroutine = null;
        Debug.Log($"<color=green>[EMP] 스턴 해제 완료</color>");
    }

    // [신규 추가] 클라이언트의 컴포넌트를 제어하는 RPC 함수
    [ClientRpc]
    private void RpcSetControllerState(bool isEnabled)
    {
        if (controller != null)
        {
            // 컨트롤러가 꺼지면 FixedUpdate가 멈추므로
            // 속도 0 고정 로직도 실행되지 않아, 밀리는 힘이 정상 적용됩니다.
            controller.enabled = isEnabled;
        }
    }

    #endregion

    #region 클라이언트 시각 처리

    [ClientRpc] private void RpcSetScale(Vector3 s) => transform.localScale = s;

    [ClientRpc]
    private void RpcSetMass(float newMass)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.mass = newMass;
        // 디버깅용 (테스트 후 삭제 가능)
        // Debug.Log($"[IronBody] 질량 변경됨: {rb.mass}");
    }

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

    [ClientRpc]
    private void RpcSetDrag(float newDrag)
    {
        // 혹시 rb가 없으면 찾기
        if (rb == null && transform.TryGetComponent(out PlayerController pc))
            rb = pc.Rb;

        if (rb == null) rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearDamping = newDrag;
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
