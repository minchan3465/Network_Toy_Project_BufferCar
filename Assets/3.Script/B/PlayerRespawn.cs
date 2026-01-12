using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class PlayerRespawn : NetworkBehaviour //민섭님 스크립트 참조
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;

    public override void OnStartLocalPlayer()
    {
        transform.TryGetComponent(out rb); //player한테 넣어주세요

        // 1. 소환되자마자 현재 위치를 리스폰 지점으로 저장
        initialPosition = transform.position + Vector3.up * 9f;
        initialRotation = transform.rotation;

        Debug.Log($"Respawn_Position: {initialPosition}");

        // ---------------------------------------------------------
        // player nickname?
        // ---------------------------------------------------------
    }

    [SyncVar] private bool isRespawning = false; // 중복 방지 변수

    [Command]
    public void CmdRequestRespawn()
    {
        if (isRespawning) return;
        // 클라이언트의 요청을 받은 서버가 실행
        OnFellInDeadZone();
    }

    // 서버 사이드에서 점수 처리를 위한 함수 (DeadZone에서 호출)
    [Server]
    public void OnFellInDeadZone()
    {
        if (connectionToClient != null && connectionToClient.isReady)
        {
            // ---------------------------------------------------------
            // 체력이 달게 합니다
            // ---------------------------------------------------------
            TargetRpcRespawn(connectionToClient);
            UIupdateRPC();
        }
        else
        {
            // 연결이 불안정하면 상태 초기화
            isRespawning = false;
        }
    }

    [TargetRpc]//타겟으로 돌리고
    void TargetRpcRespawn(NetworkConnection thisconnection)
    {
        ExecuteRespawn();
        Invoke(nameof(ResetRespawnFlag), 0.5f);
    }

    [Command]
    private void ResetRespawnFlag()
    {
        isRespawning = false;
    }

    //UI전용 ClientRPC사용

    [ClientRpc]
    private void UIupdateRPC()
    {
        Debug.Log("UIupdateRPC");
        //UIUpdate
    }

    private void ExecuteRespawn()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        Debug.Log("Respawn Complete");
    }
}