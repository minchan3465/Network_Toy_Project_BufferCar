using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class PlayerRespawnController : NetworkBehaviour
{
    // 체크하면 서버 없이 로컬에서 즉시 리스폰됩니다.
    public bool isLocalTestMode = false;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 1. 소환되자마자 현재 위치를 리스폰 지점으로 저장
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        //Debug.Log($"<color=green>[Spawn Success]</color> 시작 위치 기억 완료: {initialPosition}");

        // ---------------------------------------------------------
        // [TODO: 임우진] 로그인 정보 확인 및 유저 데이터 매칭
        // DatabaseManager.Instance.GetUserStats(userId);
        // ---------------------------------------------------------
    }

    // 데드존 충돌 시 호출되는 함수
    public void RequestRespawn()
    {
        // 로컬 환경이거나 테스트 모드일 때
        if (!NetworkClient.active || isLocalTestMode)
        {
            ExecuteRespawn();
            return;
        }

        // 서버/클라이언트 환경일 경우 (서버에게 리스폰 처리를 요청)
        if (isServer)
        {
            OnFellInDeadZone();
        }
        else if (isLocalPlayer)
        {
            CmdRequestRespawn();
        }
    }

    // 클라이언트가 서버에 리스폰을 요청하는 통로
    [Command]
    void CmdRequestRespawn()
    {
        OnFellInDeadZone();
    }

    // 서버 사이드에서 점수 처리를 위한 함수 (DeadZone에서 호출)
    [Server]
    public void OnFellInDeadZone()
    {
        // ---------------------------------------------------------
        // [TODO: 임우진] DB 연동: 추락 횟수 증가 및 LP 차감
        // DatabaseAPI.UpdateScore(netId, -10);
        // ---------------------------------------------------------

        RpcRespawn();
    }

    [ClientRpc]
    void RpcRespawn()
    {
        // 내 캐릭터이거나 네트워크가 없는 환경일 때만 실제 위치 이동 수행
        if (isLocalPlayer || !NetworkClient.active)
        {
            ExecuteRespawn();
        }
    }

    private void ExecuteRespawn()
    {
        // 물리 초기화 (Unity 6 기준)
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // [안정화] Rigidbody가 있는 경우 강제 위치 이동 시 발생할 수 있는 물리 충돌 방지
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // ---------------------------------------------------------
        // [TODO: 김민제] 리스폰 폭발/연기 이펙트(VFX) 실행
        // [TODO: 사운드] 리스폰 효과음(SFX) 출력
        // ---------------------------------------------------------

        //Debug.Log("<color=yellow>리스폰 완료</color>");
    }
}