using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class PlayerRespawn_rigid : NetworkBehaviour
{
    private Rigidbody rb;
    [SyncVar(hook = nameof(OnRespawnStateChanged))]
    public bool isRespawning = false;

    [SyncVar(hook = nameof(OnKinematicChanged))]
    public bool isKinematicSynced = false;

    private GameObject respawn_ob;
    [SerializeField] private GameObject car;
    [SerializeField] private NetworkPlayer nplayer;

    public int playerNumber = -1;

    public override void OnStartLocalPlayer()
    {
        transform.TryGetComponent(out rb);
        playerNumber = nplayer.playerNumber;

        List<Transform> startPositions = NetworkManager.startPositions;
        startPositions.Sort((a, b) => string.Compare(a.name, b.name));

        if (playerNumber != -1 && playerNumber < startPositions.Count)
        {
            Transform targetPos = startPositions[playerNumber];
            transform.position = targetPos.position;
            transform.rotation = targetPos.rotation;

            // 시작 시 로컬 물리 초기화
            Rigidbody predictedRb = GetComponent<PredictedRigidbody>().predictedRigidbody;
            predictedRb.position = targetPos.position;
            predictedRb.rotation = targetPos.rotation;
        }

        var respawnList = FindAnyObjectByType<RespawnList>();
        if (respawnList != null && playerNumber < respawnList.spawnList.Count)
        {
            respawn_ob = respawnList.spawnList[playerNumber];
        }
    }

    #region 리스폰 로직
    private void OnRespawnStateChanged(bool oldVal, bool newVal)
    {
        if (car == null) return;
        if (newVal == true)
        {
            car.SetActive(false);
        }
        else
        {
            StartCoroutine(BlinkVisuals());
        }
    }

    private void OnKinematicChanged(bool oldVal, bool newVal)
    {
        if (rb == null) transform.TryGetComponent(out rb);
        if (rb != null)
        {
            rb.isKinematic = newVal;
        }
    }

    private IEnumerator BlinkVisuals()
    {
        float duration = 2.0f;
        float interval = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            car.SetActive(!car.activeSelf);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        car.SetActive(true);

        if (isLocalPlayer)
        {
            CmdSetKinematic(false);

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            // [중요] 로컬 플레이어의 예측 RB도 확실히 멈춤
            Rigidbody predictedRb = GetComponent<PredictedRigidbody>().predictedRigidbody;
            if (predictedRb != null)
            {
                predictedRb.linearVelocity = Vector3.zero;
                predictedRb.angularVelocity = Vector3.zero;
            }
        }
    }

    [Command]
    private void CmdSetKinematic(bool state) => isKinematicSynced = state;

    [Command]
    public void CmdRequestRespawn()
    {
        if (isRespawning) return;

        isRespawning = true;
        isKinematicSynced = true;

        TargetRpcRespawn(connectionToClient);
        Invoke(nameof(ResetRespawnFlag), 3.0f);
    }

    [Server]
    private void ResetRespawnFlag() => isRespawning = false;

    private Coroutine respawnRoutine;

    [TargetRpc]
    void TargetRpcRespawn(NetworkConnection target)
    {
        // 로컬에서 물리 초기화 (본체 + 예측용)
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 로컬 플레이어라면 예측용 RB도 초기화
        Rigidbody predictedRb = GetComponent<PredictedRigidbody>().predictedRigidbody;
        if (predictedRb != null)
        {
            predictedRb.linearVelocity = Vector3.zero;
            predictedRb.angularVelocity = Vector3.zero;
            // 주의: predictedRb.isKinematic을 직접 건드리면 예측 엔진과 싸울 수 있으니 속도만 제어
        }

        if (respawnRoutine != null) StopCoroutine(respawnRoutine);
        respawnRoutine = StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(1f);

        // 위치 이동
        transform.position = respawn_ob.transform.position;
        transform.rotation = respawn_ob.transform.rotation;

        // 예측 RB 위치도 강제 동기화 (화면 튐 방지)
        if (isLocalPlayer)
        {
            Rigidbody predictedRb = GetComponent<PredictedRigidbody>().predictedRigidbody;
            predictedRb.position = respawn_ob.transform.position;
            predictedRb.rotation = respawn_ob.transform.rotation;
            predictedRb.linearVelocity = Vector3.zero;
        }

        CmdRequestAppearEffect(transform.position);
        respawnRoutine = null;
    }
    #endregion

    #region 부활 Particle (기존 유지)
    [SerializeField] private GameObject appearParticlePrefab;

    [Command]
    void CmdRequestAppearEffect(Vector3 pos)
    {
        RpcPlayAppearEffect(pos);
    }

    [ClientRpc]
    void RpcPlayAppearEffect(Vector3 pos)
    {
        if (appearParticlePrefab != null)
        {
            GameObject effect = Instantiate(appearParticlePrefab, pos, Quaternion.identity);
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
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
        isRespawning = false;
    }
}