using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Mirror;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 태그가 달린 물체만 감지
        if (other.CompareTag("Player"))
        {
            // 2. 닿은 오브젝트(other)로부터 위(Parent)로 올라가며 스크립트 탐색
            var respawnController = other.GetComponentInParent<PlayerRespawnController>();

            if (respawnController != null)
            {
                // 로컬 테스트 모드라면 즉시 리스폰
                if (respawnController.isLocalTestMode)
                {
                    respawnController.RequestRespawn();
                }
                // 멀티플레이라면 서버인 경우에만 처리 (중복 방지)
#if UNITY_SERVER || UNITY_EDITOR
                else if (NetworkServer.active)
                {
                    // 서버 사이드 로직 실행 (DB 갱신 등)
                    respawnController.OnFellInDeadZone();
                }
#endif
            }
        }
    }
}
