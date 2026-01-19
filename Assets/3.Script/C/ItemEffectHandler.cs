using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    private PlayerController controller;
    private Rigidbody rb;
    private PlayerRespawn myRespawn;

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
    private bool _wasDead = false; // [추가] 죽음 처리를 한 번만 실행하기 위한 플래그

    void Start()
    {
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        myRespawn = GetComponent<PlayerRespawn>();

        if (rb != null) defaultMass = rb.mass;
        if (controller != null) defaultSpeed = controller.Speed;
        defaultScale = transform.localScale;
    }

    [ServerCallback]
    private void Update()
    {
        if (GameManager.Instance == null) return;

        // 1. 내 생존 여부 확인
        bool isDead = (myRespawn != null && !myRespawn.canRespawn);

        if (isDead)
        {
            // 죽었는데 아직 처리를 안 했다면? (죽은 직후 1회 실행)
            if (!_wasDead)
            {
                ForceStopAllEffects(); // [핵심] 모든 이펙트 강제 종료
                _wasDead = true;       // 처리 완료 표시
            }
            return; // 죽어있는 동안은 아래 로직 실행 X
        }

        // 다시 살아났다면 플래그 초기화 (혹시 모를 리스폰 대비)
        _wasDead = false;

        // 2. 피버 타임 로직 (살아있을 때만)
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

    // [추가] 모든 효과를 즉시 끄는 함수
    private void ForceStopAllEffects()
    {
        _isFeverActive = false;
        if (controller != null) controller.Speed = defaultSpeed;
        if (rb != null)
        {
            rb.mass = defaultMass;
            rb.linearDamping = 0; // 혹시 스턴 중이었다면 해제
        }

        // 등록된 모든 파티클 끄기
        if (effectRoots != null)
        {
            for (int i = 0; i < effectRoots.Length; i++)
            {
                RpcControlEffect(i, false);
            }
        }
    }

    [Server]
    public void Svr_ApplyItemEffect(int index)
    {
        if (myRespawn != null && !myRespawn.canRespawn) return;

        switch (index)
        {
            case 0: StartCoroutine(IronBodyRoutine()); break;
            case 1: StartCoroutine(NitroChargeRoutine()); break;
            case 2: Svr_UseEMP(); break;
        }

        TargetShowItemMessage(connectionToClient, index);
    }

    #region 아이템 스킬 구현

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

        // 끝나고 나서 죽어있으면 굳이 복구할 필요 없음 (ForceStop에서 이미 처리됨)
        if (myRespawn == null || myRespawn.canRespawn)
        {
            rb.mass = defaultMass;
            RpcSetMass(defaultMass);
            RpcSetScale(defaultScale);
        }
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

        // [수정된 로직] 코루틴이 끝난 시점의 상태 체크
        bool isFever = (GameManager.Instance != null && GameManager.Instance.gameTime < 0);
        bool isDead = (myRespawn != null && !myRespawn.canRespawn);

        // 1. 죽었으면? -> 무조건 끈다.
        if (isDead)
        {
            RpcControlEffect(1, false);
            controller.Speed = defaultSpeed;
        }
        // 2. 피버 타임이 아니면? -> 끈다.
        else if (!isFever)
        {
            RpcControlEffect(1, false);
            controller.Speed = defaultSpeed;
        }
        // 3. 피버 타임이면? -> 끄지 않고 유지한다. (Speed도 유지)
        else
        {
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
        // 혹시 그 사이에 죽었더라도 끄는 건 문제없음
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
