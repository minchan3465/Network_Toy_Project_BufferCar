using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerCollision : NetworkBehaviour
{
    private Rigidbody rb;
    private PlayerRespawn res;
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

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent(out PlayerRespawn targetRes))
            {
                if (targetRes.isRespawning) return;
            }

            if (isPushing) return;
            NetworkIdentity targetIdentity = collision.gameObject.GetComponent<NetworkIdentity>();

            if (targetIdentity != null)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 dirToTarget = (collision.transform.position - transform.position);
                dirToTarget.y = 0;
                Vector3 finalForce = dirToTarget.normalized * pushForce;

                CmdPushBoth(netIdentity, targetIdentity, finalForce, contact.point, contact.normal);
            }
        }
    }

    [Command]
    public void CmdPushBoth(NetworkIdentity self, NetworkIdentity target, Vector3 force, Vector3 contactPoint, Vector3 contactNormal)
    {
        if (this.res != null && this.res.isRespawning) return;
        if (target.TryGetComponent(out PlayerRespawn targetRes) && targetRes.isRespawning) return;
        if (NetworkTime.time < lastPushTime + pushCooldown || this.isPushing) return;

        lastPushTime = NetworkTime.time;

        if (target.TryGetComponent(out PlayerCollision targetCol))
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
    private void ServerResetPushStatus() => this.isPushing = false;

    [TargetRpc]
    public void RpcApplyImpulse(Vector3 force)
    {
        PlayVibration(vpower, duration);
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (input != null) input.Enter();

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force + Vector3.up * 3f, ForceMode.Impulse);
    }
    #endregion

    #region 충돌로직2 OnTriggerEnter_Deadzone+Particle
    [SerializeField] private GameObject deadparticle;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Deadzone"))
        {
            // [최소 수정] res.isRespawning = true; 직접 대입을 지우고 ServerStartRespawn 호출
            if (res == null || res.isRespawning) return;

            Debug.Log("Deadzone Detected on Server");

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // 이 함수 안에서 isRespawning = true와 TargetRpc가 모두 실행됨
            res.ServerStartRespawn();

            RpcPlayDeadEffects(transform.position);
        }
    }

    [ClientRpc]
    private void RpcPlayDeadEffects(Vector3 pos)
    {
        if (isLocalPlayer)
        {
            PlayVibration(vpower, duration);
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        if (deadparticle != null)
        {
            GameObject effect = Instantiate(deadparticle, pos, Quaternion.identity);
            Destroy(effect, 3.0f);
        }
    }
    #endregion

    #region 유틸리티 (진동, 파티클 등)
    [Header("진동 설정")]
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

    [SerializeField] private GameObject collisionParticlePrefab;
    [ClientRpc]
    public void RPCSoundandParticle(Vector3 pos, Vector3 normal)
    {
        if (collisionParticlePrefab != null)
        {
            Vector3 spawnPos = pos + (normal) + (Vector3.up * 1.6f);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            GameObject effect = Instantiate(collisionParticlePrefab, spawnPos, rotation);
            Destroy(effect, 1.5f);
        }
    }

    private void OnDisable()
    {
        if (hapticCoroutine != null) StopCoroutine(hapticCoroutine);
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
        isPushing = false;
    }
    #endregion
}