using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class PlayerRespawn : NetworkBehaviour
{
    private Rigidbody rb;
    [SyncVar] private bool isRespawning = false; // 중복 방지 변수

    public int playerNumber = 2;//-1

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
    }

    [Command]
    public void CmdRequestRespawn()
    {
        if (isRespawning) return;

        isRespawning = true;
        // 클라이언트의 요청을 받은 서버가 실행
        UIupdateRPC();
        TargetRpcRespawn(connectionToClient);
        // 1초 뒤 리스폰 잠금 해제
        Invoke(nameof(ResetRespawnFlag), 1.0f);
    }

    [TargetRpc]
    void TargetRpcRespawn(NetworkConnection target)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        var spawnmanager = FindAnyObjectByType<RespawnManager>();
        GameObject spawnob = spawnmanager.spawnList[playerNumber];

        transform.position = spawnob.transform.position;
        transform.rotation = spawnob.transform.rotation;

        //사운드

        StartCoroutine(ReleasePhysics(2.0f));
    }

    private IEnumerator ReleasePhysics(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null)
        {
            // 다시 중력과 충돌 영향을 받음
            rb.isKinematic = false;
            // 위치 이동 직후이므로 다시 한번 속도 제로 확인
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    [Server]
    private void ResetRespawnFlag() => isRespawning = false;

    [ClientRpc]
    private void UIupdateRPC() { /* 체력 UI 업데이트 로직 */ }
}