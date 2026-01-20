using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    // --- 컴포넌트 캐싱 ---
    private PlayerController controller;
    private Rigidbody rb;
    private PlayerRespawn myRespawn;
    private NetworkPlayer networkPlayer;

    // --- 설정 변수들 ---
    [Header("--- VFX & Sound ---")]
    [SerializeField] private GameObject[] effectRoots; // [0]:Empty, [1]:Nitro, [2]:EMP, [3]:Stun
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;

    [Header("--- 아이템 밸런스 ---")]
    [SerializeField] private float ironDuration = 5f;
    [SerializeField] private float ironMassMultiplier = 5f;
    [SerializeField] private float ironScaleMultiplier = 1.5f;

    [SerializeField] private float nitroDuration = 3f;
    [SerializeField] private float nitroSpeedMultiplier = 2.5f;
    [SerializeField] private float nitroImpulseForce = 50f;

    [SerializeField] private float empStunDuration = 2.0f;
    [SerializeField] private float empBlastDuration = 2.0f;

    // --- 내부 상태 변수 ---
    private float defaultMass;
    private float defaultSpeed;
    private Vector3 defaultScale;

    private bool _isFeverActive = false;  // 피버 타임
    private bool _isNitroPlaying = false; // 니트로 사용 중
    private bool _wasDead = false;        // 죽음 상태 플래그 (중복 처리 방지)
    private bool _wasRespawning = false;

    // [리팩토링 1] 복잡한 죽음 확인 로직을 프로퍼티 하나로 압축
    private bool IsDead
    {
        get
        {
            // 1. 서버 데이터(GameManager HP) 확인
            if (networkPlayer != null && GameManager.Instance != null)
            {
                int id = networkPlayer.playerNumber;
                if (id >= 0 && id < GameManager.Instance.playersHp.Count)
                    if (GameManager.Instance.playersHp[id] < 1) return true;
            }
            // 2. 로컬 데이터 확인 (보조)
            return (myRespawn != null && !myRespawn.canRespawn);
        }
    }

    // [리팩토링 2] 부스트 상태인지 확인 (니트로 OR 피버)
    private bool IsBoostActive => (_isNitroPlaying || _isFeverActive) && !IsDead;

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

        // 1. 죽음 처리 (기존 코드 유지)
        if (IsDead)
        {
            if (!_wasDead) ResetAllState();
            _wasDead = true;
            return;
        }
        _wasDead = false;

        // --- [여기부터 추가/수정] ---
        // 1.5 리스폰 완료 감지 로직
        // PlayerRespawn의 isRespawning이 true -> false로 바뀌는 순간(서버 대기 끝)을 포착
        if (myRespawn != null)
        {
            bool isRespawning = myRespawn.isRespawning;
            if (_wasRespawning && !isRespawning)
            {
                // 서버 대기(2초)는 끝났지만, 클라이언트에서는 깜빡임(2초)이 시작됨.
                // 깜빡임이 다 끝난 뒤에 파티클을 켜주기 위해 코루틴 실행
                StartCoroutine(RestoreEffectAfterBlink());
            }
            _wasRespawning = isRespawning;
        }
        // ---------------------------

        // 2. 피버 타임 감지 (기존 코드 유지)
        bool nowFever = GameManager.Instance.gameTime < 0;
        if (_isFeverActive != nowFever)
        {
            _isFeverActive = nowFever;
            RefreshState();
        }
    }

    // [리팩토링 3] 모든 상태(속도, 파티클)를 결정하는 중앙 관제소
    // 니트로가 끝나든, 피버가 오든, 죽든 그냥 이거 한번 부르면 알아서 정리됨.
    private void RefreshState()
    {
        if (IsDead)
        {
            ResetAllState();
            return;
        }

        // 부스트 상태(니트로 or 피버)에 따라 속도와 파티클 결정
        bool active = IsBoostActive;
        controller.Speed = active ? defaultSpeed * nitroSpeedMultiplier : defaultSpeed;
        RpcControlEffect(1, active);
    }

    private void ResetAllState()
    {
        _isFeverActive = false;
        _isNitroPlaying = false;

        if (controller != null) controller.Speed = defaultSpeed;
        if (rb != null) { rb.mass = defaultMass; rb.linearDamping = 0; }

        // 모든 파티클 끄기
        for (int i = 0; i < effectRoots.Length; i++) RpcControlEffect(i, false);
    }

    // --- 아이템 사용 진입점 ---
    [Server]
    public void Svr_ApplyItemEffect(int index)
    {
        if (IsDead) return;

        switch (index)
        {
            case 0: StartCoroutine(IronBodyRoutine()); break;
            case 1: StartCoroutine(NitroRoutine()); break;
            case 2: Svr_UseEMP(); break;
        }
        TargetShowItemMessage(connectionToClient, index);
    }

    // --- 루틴 (아이템 로직) ---
    [Server]
    private IEnumerator IronBodyRoutine()
    {
        PlaySound("Power_UpSFX");

        // 적용
        rb.mass = defaultMass * ironMassMultiplier;
        RpcSetMass(rb.mass);
        RpcSetScale(defaultScale * ironScaleMultiplier);

        yield return new WaitForSeconds(ironDuration);

        // 해제 (살아있을 때만)
        if (!IsDead)
        {
            rb.mass = defaultMass;
            RpcSetMass(defaultMass);
            RpcSetScale(defaultScale);
        }
    }

    [Server]
    private IEnumerator NitroRoutine()
    {
        _isNitroPlaying = true;
        RefreshState(); // 상태 갱신 (켜기)

        PlaySound("Burst_ImpactSFX");
        rb.AddForce(transform.forward * nitroImpulseForce, ForceMode.Impulse);

        yield return new WaitForSeconds(nitroDuration);

        _isNitroPlaying = false;
        RefreshState(); // 상태 갱신 (끄거나 피버면 유지)
    }

    [Server]
    private void Svr_UseEMP()
    {
        RpcControlEffect(2, true); // 캐스팅 이펙트
        StartCoroutine(DelayEffectOff(2, empBlastDuration)); // n초 뒤 끄기
        PlaySound("EmpSFX");

        // 다른 플레이어 찾아서 스턴
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.gameObject == gameObject) continue;
            var handler = p.GetComponent<ItemEffectHandler>();
            if (handler != null) handler.Svr_ApplyStun(empStunDuration);
        }
    }

    [Server]
    public void Svr_ApplyStun(float time)
    {
        if (IsDead) return;
        StartCoroutine(StunRoutine(time));
    }

    private IEnumerator StunRoutine(float time)
    {
        // 스턴 시작
        SetControl(false);
        PlaySound("EmpSFX");
        RpcControlEffect(3, true); // 스턴 이펙트 ON

        float orgDrag = rb.linearDamping;
        SetDrag(2.0f);

        yield return new WaitForSeconds(time);

        // 스턴 종료
        RpcControlEffect(3, false); // 스턴 이펙트 OFF
        SetDrag(orgDrag);

        if (!IsDead) SetControl(true);
    }

    // --- 도우미 함수들 (Helpers) ---

    private void SetControl(bool enable)
    {
        controller.IsStunned = !enable;
        controller.enabled = enable;
        RpcSetControllerState(enable);
    }

    private void SetDrag(float drag)
    {
        rb.linearDamping = drag;
        RpcSetDrag(drag);
    }

    private void PlaySound(string name)
    {
        // SoundManager의 RPC를 부르는 게 아니라, 내 RPC를 부른다!
        RpcPlaySound(name);
    }

    private IEnumerator DelayEffectOff(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        RpcControlEffect(index, false);
    }

    // --- RPC (네트워크 전송) ---
    [ClientRpc] private void RpcSetScale(Vector3 s) => transform.localScale = s;
    [ClientRpc] private void RpcSetMass(float m) { if (TryGetComponent(out Rigidbody r)) r.mass = m; }
    [ClientRpc] private void RpcSetDrag(float d) { if (TryGetComponent(out Rigidbody r)) r.linearDamping = d; }
    [ClientRpc] private void RpcSetControllerState(bool e) { if (controller != null) controller.enabled = e; }

    [ClientRpc]
    private void RpcControlEffect(int index, bool play)
    {
        if (effectRoots == null || index >= effectRoots.Length) return;
        var root = effectRoots[index];
        if (root == null) return;

        root.SetActive(play);
        var parts = root.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in parts)
            if (play) ps.Play(); else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    [TargetRpc]
    private void TargetShowItemMessage(NetworkConnection target, int index)
    {
        string[] names = { "Iron Body", "Nitro", "EMP" };
        string msg = (index >= 0 && index < names.Length) ? names[index] : "Unknown";
    }
    [ClientRpc]
    private void RpcPlaySound(string name)
    {
        if (AudioManager.instance != null)
            // 3D 위치 무시하고 일단 소리부터 나게 PlaySFXInternal 사용
            AudioManager.instance.PlaySFX(name);
    }

    [Server]
    private IEnumerator RestoreEffectAfterBlink()
    {
        // PlayerRespawn.cs의 BlinkVisualsDuration이 2.0초이므로
        // 확실하게 깜빡임이 끝난 뒤(2.2초 후)에 명령을 보내야 함
        yield return new WaitForSeconds(2.2f);

        // 현재 상태(피버인지, 니트로인지 등)를 다시 확인해서 RPC 전송
        RefreshState();
    }
}
