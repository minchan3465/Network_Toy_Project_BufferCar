using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class ItemEffectHandler : NetworkBehaviour
{
    private PlayerController controller;
    private Rigidbody rb;
    // [변경] AudioSource 변수 삭제

    [Header("--- VFX 연결 ---")]
    [Tooltip("[0]:Empty, [1]:Nitro, [2]:EMP_Cast, [3]:Stun_Hit")]
    [SerializeField] private GameObject[] effectRoots;

    // [변경] AudioClip 배열 삭제 -> SoundManager의 Key값(String)을 사용

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
        // [SoundManager 호출] 
        // "Iron"이라는 이름은 SoundManager 인스펙터에 등록된 이름과 같아야 합니다.
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("PowerUpSFX", transform.position, 1.0f);

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

        // [SoundManager 호출]
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("Burst_ImpactSFX", transform.position, 1.0f);

        controller.Speed = defaultSpeed * nitroSpeedMultiplier;
        rb.AddForce(transform.forward * nitroImpulseForce, ForceMode.Impulse);

        yield return new WaitForSeconds(nitroDuration);

        RpcControlEffect(1, false);

        controller.Speed = defaultSpeed;
    }

    [Server]
    private void Svr_UseEMP()
    {
        RpcControlEffect(2, true);

        StartCoroutine(StopParticleDelay(2, empBlastVfxDuration));

        foreach (NetworkConnection conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                if (conn.identity.gameObject == gameObject) continue;

                var targetHandler = conn.identity.GetComponent<ItemEffectHandler>();
                if (targetHandler)
                {
                    targetHandler.Svr_ApplyStun(empStunDuration);
                }
            }
        }
    }

    private IEnumerator StopParticleDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        RpcControlEffect(index, false);
    }

    [Server]
    public void Svr_ApplyStun(float time) => StartCoroutine(StunRoutine(time));

    private IEnumerator StunRoutine(float time)
    {
        controller.IsStunned = true;

        // [SoundManager 호출] 피격자 위치에서 소리 재생
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFXPoint("EmpSFX", transform.position, 1.0f);

        if (effectRoots.Length > 3) RpcControlEffect(3, true);

        yield return new WaitForSeconds(time);

        if (effectRoots.Length > 3) RpcControlEffect(3, false);
        controller.IsStunned = false;
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
