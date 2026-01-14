using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem;

public class PlayerCollision : NetworkBehaviour
{
    private Rigidbody rb;
    private PlayerRespawn res;
    private Inputsystem input;

    [SerializeField] private float pushForce = 19; // 밀어내는 힘의 세기
    [SerializeField] private double pushCooldown = 0.2f; //밀림 쿨타임(중복 호출 방지)
    [SyncVar] private bool isPushing = false;//밀림 상황

    public override void OnStartLocalPlayer()
    {
        input = FindAnyObjectByType<Inputsystem>();
        transform.TryGetComponent(out rb);
        transform.TryGetComponent(out res);
    }

    #region 충돌로직1 OnCollisionEnter_Player
    private void OnCollisionEnter(Collision collision)
    {
        if (!isLocalPlayer) return;

        if (res != null && res.isRespawning) return;

        if (NetworkTime.time < lastPushTime + pushCooldown) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // 상대방의 Respawn 컴포넌트 가져오기
            if (collision.gameObject.TryGetComponent(out PlayerRespawn targetRes))
            {
                // 2. 부딪힌 상대방이 리스폰 중이면 충돌 무시
                if (targetRes.isRespawning) return;
            }
            //닿은 점
            ContactPoint contact = collision.GetContact(0);
            Vector3 contactPoint = contact.point;
            Vector3 contactNormal = contact.normal; // 충돌 표면의 방향 (법선)

            if (isPushing) return;
            NetworkIdentity targetIdentity = collision.gameObject.GetComponent<NetworkIdentity>();

            if (targetIdentity != null)
            {
                // 상대방과의 위치 차이 계산
                Vector3 dirToTarget = (collision.transform.position - transform.position);

                // Y축 값을 0으로 고정(위로 붕뜨거나 아래로 뚫고가려고 하는거 미연 방지)
                dirToTarget.y = 0;

                // 방향벡터에 힘
                Vector3 finalForce = dirToTarget.normalized * pushForce;

                Debug.Log("OnCollisionEnter!");

                // 서버에 계산된 수평 힘을 전달
                CmdPushBoth(netIdentity, targetIdentity, finalForce, contactPoint, contactNormal);
            }
        }
    }

    [SyncVar] private double lastPushTime; // 서버 시간 기록

    [Command]
    public void CmdPushBoth(NetworkIdentity self, NetworkIdentity target, Vector3 force, Vector3 contactPoint, Vector3 contactNormal)
    {
        // 서버측 보안 검
        if (this.res != null && this.res.isRespawning) return;

        if (target.TryGetComponent(out PlayerRespawn targetRes))
        {
            if (targetRes.isRespawning) return;
        }

        if (NetworkTime.time < lastPushTime + pushCooldown) return;

        lastPushTime = NetworkTime.time; // 현재 서버 시간 저장

        // 서버에서 두 플레이어의 상태를 모두 체크
        if (this.isPushing) return;

        if (target.TryGetComponent(out PlayerCollision targetCol))
        {
            if (targetCol.isPushing)
            {
                Debug.Log("Target is already being pushed. Ignore.");
                return;
            }

            this.isPushing = true;
            targetCol.isPushing = true;

            this.RpcApplyImpulse(-force); // 본인은 뒤로 살짝 (작용 반작용)
            targetCol.RpcApplyImpulse(force);    // 상대는 앞으로

            RPCSoundandParticle(contactPoint, contactNormal); // 닿은 위치에서 파티클 넣어주시면 됩니다.
            //사운드의 경우 이번에 3D사운드를 사용하지 않을 것 같습니다.(모노 정도면 OK)
            //(카메라 시점이 멀리 있어서 포인트에서 사운드를 줘도 입체감 살리는게 불가능함) 

            // 일정 시간 후 서버에서 변수만 false로 돌려줌
            Invoke(nameof(ServerResetPushStatus), (float)pushCooldown);
            targetCol.Invoke(nameof(targetCol.ServerResetPushStatus), (float)pushCooldown);
        }
    }

    [Server]
    private void ServerResetPushStatus()
    {
        this.isPushing = false;
    }

    [TargetRpc]
    public void RpcApplyImpulse(Vector3 force)
    {
        PlayVibration(vpower, duration);//진동호출!
        Debug.Log($"{name} RPC execution. IsLocal: {isLocalPlayer}");
        // 각 플레이어의 화면에서 실행
        if (rb == null) rb = GetComponent<Rigidbody>();

        // 조작 일시 정지
        if (input != null) input.Enter();

        // 물리 적용
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force + Vector3.up * 3f, ForceMode.Impulse); // 살짝 띄워줌
        Debug.Log($"Addforce {force} power to {name} player");
    }
    #endregion

    #region 충돌 Particle
    [SerializeField] private GameObject collisionParticlePrefab; // 충돌 파티클 프리팹

    [ClientRpc]
    public void RPCSoundandParticle(Vector3 pos, Vector3 normal)
    {
        if (collisionParticlePrefab != null)
        {
            // 1. 위치 보정 (가장 중요)
            // normal 방향(벽 밖으로) 0.2m + Vector3.up(하늘 위로) 0.2m 
            // 이렇게 하면 벽과 바닥 양쪽 모두에서 파묻히지 않고 공중에 확실히 뜹니다.
            Vector3 spawnPos = pos + (normal) + (Vector3.up * 1.6f);

            // 2. 회전 보정 (ㅗ 모양 유지)
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

            GameObject effect = Instantiate(collisionParticlePrefab, spawnPos, rotation);

            // 3. 자식 파티클 수명 계산 (기존 로직)
            float maxLifeTime = 0f;
            ParticleSystem[] allParticles = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                var main = ps.main;
                float currentLifeTime = main.duration + main.startLifetime.constantMax;
                if (currentLifeTime > maxLifeTime) maxLifeTime = currentLifeTime;
            }

            Destroy(effect, maxLifeTime > 0 ? maxLifeTime : 1.5f);
        }
    }
    

    #endregion

    #region 카메라, 패드 진동
    [Header("설정")]
    [SerializeField] private float vpower = 0.7f;  // 진동 강도
    [SerializeField] private float duration = 0.2f;  // 진동 지속 시간

    private Coroutine hapticCoroutine;

    public void PlayVibration(float intensity, float time)
    {
        if (!isLocalPlayer) return;

        if (Camera_manager.instance != null) 
        { 
            Camera_manager.instance.ShakeCamera();
            Debug.Log("Camera shake+PlayVibration");
        }

        var xboxGamepad = Gamepad.current;
        if (xboxGamepad == null) return;

        // 이미 진동 중이라면 멈추고 새로 시작
        if (hapticCoroutine != null) StopCoroutine(hapticCoroutine);
        hapticCoroutine = StartCoroutine(HapticRoutine(xboxGamepad, intensity, time));
    }

    private IEnumerator HapticRoutine(Gamepad gamepad, float intensity, float time)
    {
        // 낮은 주파수(왼쪽, 큰 모터라서 진동이 큽니다. 폭발등)와
        // 높은 주파수(오른쪽, 작고 가벼운 모터라서 징~ 거리는 진동)에 강도 적용
        gamepad.SetMotorSpeeds(intensity * 0.8f, intensity);

        yield return new WaitForSeconds(time);

        // 진동 종료
        if (gamepad != null) gamepad.SetMotorSpeeds(0f, 0f);
        hapticCoroutine = null;
    }
    #endregion

    #region 충돌로직2 OnTriggerEnter_Deadzone
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Deadzone"))
        {
            Debug.Log("Deadzone Tag Detected!");

            if (res != null)
            {
                if (res.isLocalPlayer)
                {
                    if (res.isRespawning) { return; }
                    PlayVibration(vpower, duration);//진동호출!
                    //여기 사운드나 파티클 넣어주세요?

                    //체력이 남아있다면 리스폰 호출, 그렇지 않으면 실격처리 들어가야됩니다.

                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic = true;
                    }
                    res.CmdRequestRespawn();
                    CmdRpcDeadEffect(transform.position);
                    Debug.Log("My car fell! Requesting Respawn to Server...");
                }
            }
        }
    }
    #endregion

    #region Dead_Particle
    [SerializeField] private GameObject deadparticle; // 충돌 파티클 프리팹
    
    [Command]
    void CmdRpcDeadEffect(Vector3 pos)
    {
        RpcPlayDeadEffect(pos);
    }

    [ClientRpc]
    void RpcPlayDeadEffect(Vector3 pos)
    {
        if (deadparticle != null)
        {
            GameObject effect = Instantiate(deadparticle, pos, Quaternion.identity);

            // 모든 자식 파티클 시스템을 가져옴
            ParticleSystem[] allParticles = effect.GetComponentsInChildren<ParticleSystem>();

            float maxLifeTime = 0f;

            foreach (ParticleSystem ps in allParticles)
            {
                var main = ps.main;
                // 각 파티클마다 (지속시간 + 생존시간)을 계산해서 가장 긴 시간을 찾음
                float currentLifeTime = main.duration + main.startLifetime.constantMax;
                if (currentLifeTime > maxLifeTime)
                {
                    maxLifeTime = currentLifeTime;
                }
            }

            // 가장 긴 파티클이 끝나는 시점에 부모 오브젝트 삭제 (없으면 기본 3초)
            Destroy(effect, maxLifeTime > 0 ? maxLifeTime : 3.0f);
        }
        //사운드도?
    }
    #endregion

    private void OnDisable()
    {
        if (hapticCoroutine != null)
        {
            StopCoroutine(hapticCoroutine);
            hapticCoroutine = null;
        }
        // 오브젝트가 비활성화될 때 진동 강제 종료
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
        // 오브젝트가 꺼질 때 변수를 초기화하여 다음 활성화 때 버그 방지
        isPushing = false;
    }
}
