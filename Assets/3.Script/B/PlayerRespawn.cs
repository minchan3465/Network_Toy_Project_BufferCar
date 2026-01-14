using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class PlayerRespawn : NetworkBehaviour
{
    private Rigidbody rb;
    [SyncVar] public bool isRespawning = false; // 중복 방지 변수
    private GameObject respawn_ob;

    public int playerNumber = -1;//값 쏴주면 받아주세요

    public override void OnStartLocalPlayer()
    {
        transform.TryGetComponent(out rb); //player한테 넣어주세요

        List<Transform> startPositions = NetworkManager.startPositions;

        // 이름순 정렬 (순서 꼬임 방지) 0123
        startPositions.Sort((a, b) => string.Compare(a.name, b.name));

        // 부여받은 번호가 있고, 리스트 범위 내에 있다면 해당 위치 사용
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
    [Command]
    public void CmdRequestRespawn()//PlaterCollsion 에서 Trigger에서 불러옵니다.
    {
        if (isRespawning) return;
        isRespawning = true;
        TargetRpcRespawn(connectionToClient);// 클라이언트의 요청을 받은 서버가 실행
        Invoke(nameof(ResetRespawnFlag), 1.0f);// 1초 뒤 리스폰 잠금 해제
    }
    [Server]
    private void ResetRespawnFlag() => isRespawning = false;

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
        // 1초 대기 (추락한 곳에서 잠시 멈춤), 파티클 시간에 따라 시간 바꿔야됌
        yield return new WaitForSeconds(1f);
        // 리스폰 위치로 이동
        transform.position = respawn_ob.transform.position;
        transform.rotation = respawn_ob.transform.rotation;

        CmdRequestAppearEffect(transform.position);
        //공중에서 잠시 대기 여기서도 부활 파티클 같은거 있으면 좋을 것 같습니다.(공중에 그냥 가만히 있음)
        yield return new WaitForSeconds(2f);

        // 물리 해제
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        respawnRoutine = null;
    }
    #endregion

    #region 부활 Particle
    [SerializeField] private GameObject appearParticlePrefab;

    [Command]
    void CmdRequestAppearEffect(Vector3 pos)
    {
        RpcPlayAppearEffect(pos); // 서버가 모든 클라이언트에게 실행 명령을 내림
    }

    [ClientRpc]
    void RpcPlayAppearEffect(Vector3 pos)
    {
        // 모든 플레이어의 화면에서 실행됨
        if (appearParticlePrefab != null)
        {
            Instantiate(appearParticlePrefab, pos, Quaternion.identity);
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
        // 리스폰 도중 오브젝트가 꺼지면 상태를 초기화
        isRespawning = false;
    }
}