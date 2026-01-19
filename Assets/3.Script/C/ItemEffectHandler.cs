using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    private PlayerController controller;
    private Rigidbody rb;
    private PlayerRespawn myRespawn; // [추가] 내 생존 여부 확인용

    [Header("--- VFX 연결 ---")]
    [Tooltip("[0]:Empty, [1]:Nitro, [2]:EMP_Cast, [3]:Stun_Hit")]
    [SerializeField] private GameObject[] effectRoots;

    [Header("--- 사운드 설정 ---")]
    [Tooltip("아이템 효과음 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1.0f;

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
    private bool _isFeverActive = false;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();

        // [추가] 내 죽음 상태를 확인하기 위해 컴포넌트 가져오기
        myRespawn = GetComponent<PlayerRespawn>();

        if (rb != null) defaultMass = rb.mass;
        if (controller != null) defaultSpeed = controller.Speed;
        defaultScale = transform.localScale;
    }

    [ServerCallback]
    private void Update()
    {
        if (GameManager.Instance == null) return;

        // [해결책 2] 죽은 플레이어(Respawn 불가)라면 피버 로직 차단
        // GameManager가 ProcessPlayerFell에서 canRespawn을 false로 만듭니다.
        if (myRespawn != null && !myRespawn.canRespawn)
        {
            // 만약 피버 효과가 켜진 상태로 죽었다면 즉시 끕니다.
            if (_isFeverActive)
            {
                _isFeverActive = false;
                controller.Speed = defaultSpeed;
                RpcControlEffect(1, false);
            }
            return; // 더 이상 아래 로직(피버 체크)을 실행하지 않음
        }

        // --- 기존 피버 로직 ---
        bool currentFeverState = GameManager.Instance.gameTime < 0;

        if (_isFeverActive != currentFeverState)
        {
            _isFeverActive = currentFeverState;

            if (_isFeverActive)
            {
                controller.Speed = defaultSpeed * nitroSpeedMultiplier;
                RpcControlEffect(1, true);
            }
            else
            {
                controller.Speed = defaultSpeed;
                RpcControlEffect(1, false);
            }
        }
    }

    [Server]
    public void Svr_ApplyItemEffect(int index)
    {
        // 죽은 상태면 아이템 사용 불가
        if (myRespawn != null && !myRespawn.canRespawn) return;

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
        rb.mass = heavyMass;
        RpcSetMass(heavyMass);
        RpcSetScale(defaultScale * ironScaleMultiplier);

        yield return new WaitForSeconds(ironDuration);

        rb.mass = defaultMass;
        RpcSetMass(defaultMass);
        RpcSetScale(defaultScale);
    }

    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        RpcControlEffect(1, true);

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("Burst_ImpactSFX", transform.position, 1.0f, sfxVolume);

        controller.Speed = defaultSpeed * nitroSpeedMultiplier;
        rb.AddForce(transform.forward * nitroImpulseForce, ForceMode.Impulse);

        yield return new WaitForSeconds(nitroDuration);

        // [해결책 1] 시간이 다 됐어도 피버 타임이면 끄지 않는다!
        bool isFever = false;
        if (GameManager.Instance != null)
        {
            isFever = GameManager.Instance.gameTime < 0;
        }

        // 죽은 상태가 아니고, 피버 타임도 아닐 때만 끕니다.
        if (!isFever && (myRespawn == null || myRespawn.canRespawn))
        {
            RpcControlEffect(1, false);
            controller.Speed = defaultSpeed;
        }
        else
        {
            // 피버 타임이거나 죽은 상태면 로직 유지 (Update문에서 처리됨)
            // 피버 중이면 속도를 줄이지 않고 계속 빠름 유지
            controller.Speed = defaultSpeed * nitroSpeedMultiplier;
        }
    }

    [Server]
    private void Svr_UseEMP()
    {
        RpcControlEffect(2, true);
        StartCoroutine(StopParticleDelay(2, empBlastVfxDuration));

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f, sfxVolume);

        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController target in allPlayers)
        {
            if (target.gameObject == gameObject) continue;
            var targetHandler = target.GetComponent<ItemEffectHandler>();

            // 타겟이 살아있을 때만 스턴
            var targetRespawn = target.GetComponent<PlayerRespawn>();
            if (targetHandler != null && (targetRespawn == null || targetRespawn.canRespawn))
            {
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
        if (currentStunCoroutine != null) StopCoroutine(currentStunCoroutine);
        currentStunCoroutine = StartCoroutine(StunRoutine(time));
    }

    private IEnumerator StunRoutine(float time)
    {
        controller.IsStunned = true;
        controller.enabled = false;
        RpcSetControllerState(false);

        float originalDrag = rb.linearDamping;
        rb.linearDamping = 2.0f;
        RpcSetDrag(2.0f);

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f, sfxVolume);

        if (effectRoots.Length > 3) RpcControlEffect(3, true);

        yield return new WaitForSeconds(time);

        if (effectRoots.Length > 3) RpcControlEffect(3, false);

        // 스턴 끝났을 때 죽어있으면 다시 켜지 않음
        if (myRespawn == null || myRespawn.canRespawn)
        {
            controller.IsStunned = false;
            controller.enabled = true;
            RpcSetControllerState(true);
        }

        rb.linearDamping = originalDrag;
        RpcSetDrag(originalDrag);
        currentStunCoroutine = null;
    }

    [ClientRpc]
    private void RpcSetControllerState(bool isEnabled)
    {
        if (controller != null) controller.enabled = isEnabled;
    }

    #endregion

    #region 클라이언트 시각 처리

    [ClientRpc] private void RpcSetScale(Vector3 s) => transform.localScale = s;

    [ClientRpc]
    private void RpcSetMass(float newMass)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.mass = newMass;
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
        if (rb == null && transform.TryGetComponent(out PlayerController pc)) rb = pc.Rb;
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearDamping = newDrag;
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
