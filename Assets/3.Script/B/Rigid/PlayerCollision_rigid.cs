using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem;

public class PlayerCollision_rigid : NetworkBehaviour
{
    private Rigidbody rb;
    private PlayerRespawn_rigid res;
    private Inputsystem input;

    [SerializeField] private float pushForce = 19;
    [SerializeField] private double pushCooldown = 0.2f;
    [SyncVar] private bool isPushing = false;

    [SyncVar] private double lastPushTime;

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

        // [중요] 상대방이 PredictedRigidbody를 사용 중이라면, 
        // 충돌한 collider가 본체가 아니라 'Ghost'일 수 있습니다.
        // 공식 문서 가이드: Ghost인지 확인하고 진짜 주인을 찾아옵니다.
        GameObject targetObj = collision.gameObject;
        if (PredictedRigidbody.IsPredicted(collision.collider, out PredictedRigidbody originalPredicted))
        {
            targetObj = originalPredicted.gameObject;
        }

        if (targetObj.CompareTag("Player"))
        {
            if (targetObj.TryGetComponent(out PlayerRespawn_rigid targetRes))
            {
                if (targetRes.isRespawning) return;
            }

            ContactPoint contact = collision.GetContact(0);
            Vector3 contactPoint = contact.point;
            Vector3 contactNormal = contact.normal;

            if (isPushing) return;

            // 위에서 찾은 targetObj의 NetworkIdentity를 가져옵니다.
            NetworkIdentity targetIdentity = targetObj.GetComponent<NetworkIdentity>();

            if (targetIdentity != null)
            {
                Vector3 dirToTarget = (targetObj.transform.position - transform.position);
                dirToTarget.y = 0;
                Vector3 finalForce = dirToTarget.normalized * pushForce;

                Debug.Log("OnCollisionEnter with Predicted Logic!");

                // Ghost가 아닌 진짜 주인(targetIdentity)을 서버로 보냅니다.
                CmdPushBoth(netIdentity, targetIdentity, finalForce, contactPoint, contactNormal);
            }
        }
    }

    [Command]
    public void CmdPushBoth(NetworkIdentity self, NetworkIdentity target, Vector3 force, Vector3 contactPoint, Vector3 contactNormal)
    {
        if (this.res != null && this.res.isRespawning) return;

        if (target.TryGetComponent(out PlayerRespawn_rigid targetRes))
        {
            if (targetRes.isRespawning) return;
        }

        if (NetworkTime.time < lastPushTime + pushCooldown) return;

        lastPushTime = NetworkTime.time;

        if (this.isPushing) return;

        if (target.TryGetComponent(out PlayerCollision_rigid targetCol))
        {
            if (targetCol.isPushing) return;

            this.isPushing = true;
            targetCol.isPushing = true;

            this.RpcApplyImpulse(-force);
            targetCol.RpcApplyImpulse(force);

            RPCSoundandParticle(contactPoint, contactNormal);

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
        PlayVibration(vpower, duration);
        Debug.Log($"{name} RPC execution. IsLocal: {isLocalPlayer}");

        if (input != null) input.Enter();

        // [중요] 힘을 가할 때도 로컬 플레이어라면 '예측용 리지드바디'에 가해야 즉시 반응함
        Rigidbody targetRb = (isLocalPlayer) ? GetComponent<PredictedRigidbody>().predictedRigidbody : GetComponent<Rigidbody>();

        if (targetRb != null)
        {
            targetRb.linearVelocity = Vector3.zero;
            targetRb.AddForce(force + Vector3.up * 3f, ForceMode.Impulse);
        }
    }
    #endregion

    #region 충돌 Particle (기존 유지)
    [SerializeField] private GameObject collisionParticlePrefab;

    [ClientRpc]
    public void RPCSoundandParticle(Vector3 pos, Vector3 normal)
    {
        if (collisionParticlePrefab != null)
        {
            Vector3 spawnPos = pos + (normal) + (Vector3.up * 1.6f);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            GameObject effect = Instantiate(collisionParticlePrefab, spawnPos, rotation);

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

    #region 카메라, 패드 진동 (기존 유지)
    [Header("설정")]
    [SerializeField] private float vpower = 0.7f;
    [SerializeField] private float duration = 0.2f;

    private Coroutine hapticCoroutine;

    public void PlayVibration(float intensity, float time)
    {
        if (!isLocalPlayer) return;

        if (Camera_manager.instance != null) Camera_manager.instance.ShakeCamera();

        var xboxGamepad = Gamepad.current;
        if (xboxGamepad == null) return;

        if (hapticCoroutine != null) StopCoroutine(hapticCoroutine);
        hapticCoroutine = StartCoroutine(HapticRoutine(xboxGamepad, intensity, time));
    }

    private IEnumerator HapticRoutine(Gamepad gamepad, float intensity, float time)
    {
        gamepad.SetMotorSpeeds(intensity * 0.8f, intensity);
        yield return new WaitForSeconds(time);
        if (gamepad != null) gamepad.SetMotorSpeeds(0f, 0f);
        hapticCoroutine = null;
    }
    #endregion

    #region 충돌로직2 OnTriggerEnter_Deadzone (기존 유지 + 일부 보완)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Deadzone"))
        {
            if (res != null)
            {
                if (res.isLocalPlayer)
                {
                    if (res.isRespawning) return;
                    PlayVibration(vpower, duration);

                    // 낙하 시에도 예측용 RB를 멈춰주는 것이 안전함
                    Rigidbody myRb = GetComponent<PredictedRigidbody>().predictedRigidbody;
                    if (myRb != null)
                    {
                        myRb.linearVelocity = Vector3.zero;
                        myRb.angularVelocity = Vector3.zero;
                        // isKinematic은 PredictedRB가 관리하므로 직접 건드리면 충돌날 수 있으나, 
                        // Respawn 로직에서 강제로 처리하므로 여기선 속도만 0으로
                    }

                    res.CmdRequestRespawn();
                    CmdRpcDeadEffect(transform.position);
                }
            }
        }
    }
    #endregion

    #region Dead_Particle (기존 유지)
    [SerializeField] private GameObject deadparticle;

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
            ParticleSystem[] allParticles = effect.GetComponentsInChildren<ParticleSystem>();
            float maxLifeTime = 0f;
            foreach (ParticleSystem ps in allParticles)
            {
                var main = ps.main;
                float currentLifeTime = main.duration + main.startLifetime.constantMax;
                if (currentLifeTime > maxLifeTime) maxLifeTime = currentLifeTime;
            }
            Destroy(effect, maxLifeTime > 0 ? maxLifeTime : 3.0f);
        }
    }
    #endregion

    private void OnDisable()
    {
        if (hapticCoroutine != null)
        {
            StopCoroutine(hapticCoroutine);
            hapticCoroutine = null;
        }
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
        isPushing = false;
    }
}