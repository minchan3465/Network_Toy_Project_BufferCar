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
        // [수정] 매개변수 4개 (이름, 위치, 더미값, 볼륨배율)
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("Power UpSFX", transform.position, 1.0f, sfxVolume);

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

        // [수정] 매개변수 4개 (이름, 위치, 더미값, 볼륨배율)
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f, sfxVolume);

        // Unity 2023+ (Unity 6) 기준: FindObjectsByType
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

        float originalDrag = rb.linearDamping;
        rb.linearDamping = 2.0f;

        // [수정] 매개변수 4개 (이름, 위치, 더미값, 볼륨배율)
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f, sfxVolume);

        if (effectRoots.Length > 3) RpcControlEffect(3, true);

        yield return new WaitForSeconds(time);

        if (effectRoots.Length > 3) RpcControlEffect(3, false);

        controller.IsStunned = false;
        controller.enabled = true;
        rb.linearDamping = originalDrag;

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
