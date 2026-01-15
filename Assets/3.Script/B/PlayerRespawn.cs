using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class PlayerRespawn : NetworkBehaviour
{
    private Rigidbody rb;
    [SyncVar(hook = nameof(OnRespawnStateChanged))]
    public bool isRespawning = false;

    [SyncVar(hook = nameof(OnKinematicChanged))]
    public bool isKinematicSynced = false;

    private GameObject respawn_ob;
    [SerializeField] private GameObject car;
    [SerializeField] private NetworkPlayer netplayer;

    public int playerNumber = -1;

    public override void OnStartLocalPlayer()
    {
        transform.TryGetComponent(out rb);

        List<Transform> startPositions = NetworkManager.startPositions;
        playerNumber = netplayer.playerNumber;

        startPositions.Sort((a, b) => string.Compare(a.name, b.name));

        if (playerNumber != -1 && playerNumber < startPositions.Count)
        {
            Transform targetPos = startPositions[playerNumber];
            transform.position = targetPos.position;
            transform.rotation = targetPos.rotation;
        }

        var respawnList = FindAnyObjectByType<RespawnList>();
        if (respawnList != null && playerNumber < respawnList.spawnList.Count)
        {
            respawn_ob = respawnList.spawnList[playerNumber];
        }
    }

    #region 리스폰 로직

    //서버에서 리스폰을 시작하는 함수 (Collision에서 호출용)
    [Server]
    public void ServerStartRespawn()
    {
        if (isRespawning) return;

        isRespawning = true;
        isKinematicSynced = true;

        TargetRpcRespawn(connectionToClient);
        Invoke(nameof(ResetRespawnFlag), 3.0f);
    }

    [Command]
    public void CmdRequestRespawn()
    {
        ServerStartRespawn(); // 로직 통합
    }

    [Server]
    private void ResetRespawnFlag() => isRespawning = false;

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
        }
    }

    [Command]
    private void CmdSetKinematic(bool state) => isKinematicSynced = state;

    private Coroutine respawnRoutine;
    [TargetRpc]
    void TargetRpcRespawn(NetworkConnection target)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        if (respawnRoutine != null) StopCoroutine(respawnRoutine);
        respawnRoutine = StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(1f);

        if (respawn_ob != null)
        {
            transform.position = respawn_ob.transform.position;
            transform.rotation = respawn_ob.transform.rotation;
        }

        CmdRequestAppearEffect(transform.position);
        respawnRoutine = null;
    }
    #endregion

    #region 부활 Particle
    [SerializeField] private GameObject appearParticlePrefab;

    [Command]
    void CmdRequestAppearEffect(Vector3 pos) => RpcPlayAppearEffect(pos);

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
        if (respawnRoutine != null) StopCoroutine(respawnRoutine);
        isRespawning = false;
    }
}