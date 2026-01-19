using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    private PlayerController controller;
    private Rigidbody rb;
    private PlayerRespawn myRespawn;
    private NetworkPlayer networkPlayer;

    [Header("--- VFX 연결 ---")]
    [Tooltip("[0]:Empty, [1]:Nitro, [2]:EMP_Cast, [3]:Stun_Hit")]
    [SerializeField] private GameObject[] effectRoots;

    [Header("--- 사운드 설정 ---")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1.0f;

    [Header("--- 밸런스 설정 ---")]
    [SerializeField] private float ironDuration = 5f;
    [SerializeField] private float ironMassMultiplier = 5f;
    [SerializeField] private float ironScaleMultiplier = 1.5f;

    [SerializeField] private float nitroDuration = 3f;
    [SerializeField] private float nitroSpeedMultiplier = 2.5f;
    [SerializeField] private float nitroImpulseForce = 50f;

    [SerializeField] private float empStunDuration = 2.0f;
    [SerializeField] private float empBlastVfxDuration = 2.0f;

    private float defaultMass;
    private float defaultSpeed;
    private Vector3 defaultScale;

    // [핵심 변경] 상태를 관리하는 변수들
    private bool _isFeverActive = false; // 피버 타임인가?
    private bool _isNitroPlaying = false; // 니트로 아이템 사용 중인가?
    private bool _wasDead = false;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        myRespawn = GetComponent<PlayerRespawn>();
        networkPlayer = GetComponent<NetworkPlayer>();

        if (rb != null) defaultMass = rb.mass;
        if (controller != null) defaultSpeed = controller.Speed;
        defaultScale = transform.localScale;
    }

    [ServerCallback]
    private void Update()
    {
        if (GameManager.Instance == null) return;

        // 1. 죽음 체크
        bool isDead = CheckIsDeadOnServer();
        if (isDead)
        {
            if (!_wasDead)
            {
                ForceStopAllEffects();
                _wasDead = true;
            }
            return;
        }
        _wasDead = false;

        // 2. 피버 타임 상태 감지
        bool currentFeverState = GameManager.Instance.gameTime < 0;

        // 피버 상태가 바뀌었을 때만 로직 실행 (최적화)
        if (_isFeverActive != currentFeverState)
        {
            _isFeverActive = currentFeverState;
            // 피버가 켜지거나 꺼질 때, 니트로 상태와 합쳐서 최종 결정
            UpdateNitroState();
        }
    }

    // [핵심 해결책] 니트로와 피버 상태를 종합해서 켤지 끌지 결정하는 함수
    private void UpdateNitroState()
    {
        // 죽었으면 무조건 끔
        if (CheckIsDeadOnServer())
        {
            controller.Speed = defaultSpeed;
            RpcControlEffect(1, false);
            return;
        }

        // 니트로 아이템 사용 중이거나 OR 피버 타임이면 -> 켠다!
        bool shouldActive = _isNitroPlaying || _isFeverActive;

        if (shouldActive)
        {
            controller.Speed = defaultSpeed * nitroSpeedMultiplier;
            RpcControlEffect(1, true); // 파티클 ON
        }
        else
        {
            controller.Speed = defaultSpeed;
            RpcControlEffect(1, false); // 파티클 OFF
        }
    }

    // 서버 데이터를 통해 죽음 판별
    private bool CheckIsDeadOnServer()
    {
        if (networkPlayer != null && GameManager.Instance != null)
        {
            int myIndex = networkPlayer.playerNumber;
            if (myIndex >= 0 && myIndex < GameManager.Instance.playersHp.Count)
            {
                if (GameManager.Instance.playersHp[myIndex] < 1) return true;
            }
        }
        if (myRespawn != null && !myRespawn.canRespawn) return true;
        return false;
    }

    private void ForceStopAllEffects()
    {
        _isFeverActive = false;
        _isNitroPlaying = false; // 니트로 상태도 강제 초기화

        if (controller != null) controller.Speed = defaultSpeed;
        if (rb != null)
        {
            rb.mass = defaultMass;
            rb.linearDamping = 0;
        }

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
        if (CheckIsDeadOnServer()) return;

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

        if (!CheckIsDeadOnServer())
        {
            rb.mass = defaultMass;
            RpcSetMass(defaultMass);
            RpcSetScale(defaultScale);
        }
    }

    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        // 1. 니트로 사용 중임을 표시
        _isNitroPlaying = true;

        // 2. 상태 업데이트 (여기서 파티클 ON + 속도 증가가 일어남)
        UpdateNitroState();

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("Burst_ImpactSFX", transform.position, 1.0f, sfxVolume);

        rb.AddForce(transform.forward * nitroImpulseForce, ForceMode.Impulse);

        yield return new WaitForSeconds(nitroDuration);

        // 3. 니트로 사용 끝남
        _isNitroPlaying = false;

        // 4. 상태 업데이트 (피버 중이면 안 꺼지고, 피버 아니면 꺼짐)
        UpdateNitroState();
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

            if (targetHandler != null)
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
        if (CheckIsDeadOnServer()) return;

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

        if (!CheckIsDeadOnServer())
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
