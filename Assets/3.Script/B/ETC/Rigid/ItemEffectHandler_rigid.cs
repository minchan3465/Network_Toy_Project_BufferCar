using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController_rigid))]
public class ItemEffectHandler_rigid : NetworkBehaviour
{
    private PlayerController_rigid controller;
    private Rigidbody rb; // 서버용 (실제)
    // 클라이언트 예측용 RB에 접근하기 위한 프로퍼티
    private Rigidbody PredictedRb => GetComponent<PredictedRigidbody>().predictedRigidbody;

    [Header("--- VFX 연결 ---")]
    [Tooltip("[0]:Empty, [1]:Nitro, [2]:EMP_Cast, [3]:Stun_Hit")]
    [SerializeField] private GameObject[] effectRoots;

    [Header("--- 사운드 설정 ---")]
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
    private float defaultDrag;

    void Start()
    {
        controller = GetComponent<PlayerController_rigid>();
        rb = GetComponent<Rigidbody>();

        defaultMass = rb.mass;
        defaultDrag = rb.linearDamping; // Unity 6 (구 drag)
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

    // 1. 아이언 바디 (질량 증가)
    [Server]
    private IEnumerator IronBodyRoutine()
    {

        // 서버 물리 적용
        rb.mass = defaultMass * ironMassMultiplier;

        // 클라이언트 물리 & 시각 적용
        RpcSetIronBodyState(true, defaultMass * ironMassMultiplier, defaultScale * ironScaleMultiplier);

        yield return new WaitForSeconds(ironDuration);

        // 복구
        rb.mass = defaultMass;
        RpcSetIronBodyState(false, defaultMass, defaultScale);
    }

    [ClientRpc]
    private void RpcSetIronBodyState(bool isActive, float mass, Vector3 scale)
    {
        transform.localScale = scale;

        // [핵심] 예측용 RB의 질량도 같이 바꿔줘야 충돌 시 밀리지 않음
        if (isLocalPlayer)
        {
            Rigidbody pRb = PredictedRb;
            if (pRb != null) pRb.mass = mass;
        }
        else
        {
            // 타 클라이언트 입장에서의 물리(보간용)도 적용
            if (rb != null) rb.mass = mass;
        }
    }


    // 2. 니트로 (속도 증가 + 순간 가속)
    [Server]
    private IEnumerator NitroChargeRoutine()
    {
        RpcControlEffect(1, true);


        // 최대 속도 제한 해제
        controller.Speed = defaultSpeed * nitroSpeedMultiplier;

        // [핵심] 순간 가속: 서버와 클라이언트 양쪽에 힘을 가해야 딜레이가 없음
        Vector3 forceVec = transform.forward * nitroImpulseForce;
        rb.AddForce(forceVec, ForceMode.Impulse);
        RpcApplyNitroImpulse(forceVec);

        yield return new WaitForSeconds(nitroDuration);

        RpcControlEffect(1, false);
        controller.Speed = defaultSpeed; // 속도 복구
    }

    [ClientRpc]
    private void RpcApplyNitroImpulse(Vector3 force)
    {
        // 로컬 플레이어라면 예측용 리지드바디에 힘을 가함 (즉시 반응)
        if (isLocalPlayer)
        {
            Rigidbody pRb = PredictedRb;
            if (pRb != null)
            {
                pRb.AddForce(force, ForceMode.Impulse);
            }
        }
    }


    // 3. EMP (광역 스턴)
    [Server]
    private void Svr_UseEMP()
    {
        Debug.Log($"[EMP] {name}가 EMP 발동!");

        RpcControlEffect(2, true);
        StartCoroutine(StopParticleDelay(2, empBlastVfxDuration));


        // UnityEngine.Object.FindObjectsByType은 Unity 2023.1+ 권장 (구버전이면 FindObjectsOfType 사용)
        PlayerController_rigid[] allPlayers = FindObjectsByType<PlayerController_rigid>(FindObjectsSortMode.None);

        foreach (PlayerController_rigid target in allPlayers)
        {
            if (target.gameObject == gameObject) continue;

            var targetHandler = target.GetComponent<ItemEffectHandler_rigid>();
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
        if (currentStunCoroutine != null) StopCoroutine(currentStunCoroutine);
        currentStunCoroutine = StartCoroutine(StunRoutine(time));
    }

    private IEnumerator StunRoutine(float time)
    {
        // 1. 상태 동기화 (PlayerController_rigid의 FixedUpdate에서 이동 로직을 막음)
        controller.IsStunned = true;

        // 2. 물리적 미끄러짐 방지 (Drag 증가)
        float stunDrag = 10.0f; // 꽉 잡히는 느낌
        rb.linearDamping = stunDrag;
        RpcSetStunDrag(stunDrag);

        // [변경] controller.enabled = false를 하지 않습니다.
        // 이유: FixedUpdate가 돌아야 PlayerController 내부의 "if(IsStunned) velocity=0" 로직이 작동하여
        // 예측 엔진과 싸우지 않고 위치를 고정할 수 있습니다.


        if (effectRoots.Length > 3) RpcControlEffect(3, true);

        yield return new WaitForSeconds(time);

        if (effectRoots.Length > 3) RpcControlEffect(3, false);

        // 해제
        controller.IsStunned = false;
        rb.linearDamping = defaultDrag;
        RpcSetStunDrag(defaultDrag);

        currentStunCoroutine = null;
    }

    [ClientRpc]
    private void RpcSetStunDrag(float drag)
    {
        if (isLocalPlayer)
        {
            Rigidbody pRb = PredictedRb;
            if (pRb != null) pRb.linearDamping = drag;
        }
    }

    #endregion

    #region 클라이언트 시각 처리

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