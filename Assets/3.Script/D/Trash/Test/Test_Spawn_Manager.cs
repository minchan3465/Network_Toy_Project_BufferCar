using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Test_Spawn_Manager : MonoBehaviour {
    [Header("스폰 설정")]
    public GameObject playerPrefab;    // 범퍼카 프리팹
    public Transform[] spawnPoints;   // 1~4번 스폰 위치 (Transform 드래그)
    public bool isLocalTestMode = true; // 로컬 테스트 시 체크

    void Start() {
        // 로컬 테스트 모드일 때 (네트워크 없이 4명 소환 확인용)
        if (isLocalTestMode) {
            LocalSpawnTest();
        }
    }

    // 로컬에서 4명이 잘 소환되는지 확인하기 위한 함수
    private void LocalSpawnTest() {
        Debug.Log("<color=cyan>[Local Test]</color> 4인 스폰을 시작합니다.");
        for (int i = 0; i < 4; i++) {
            if (i < spawnPoints.Length) {
                GameObject player = Instantiate(playerPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
                player.transform.name = (i+1) + "P";
            }
        }
    }

    // --- 멀티플레이 시 호출될 실제 스폰 로직 (정민찬 CTO 파트) ---
    // Mirror의 NetworkManager에서 유저가 접속할 때 이 함수를 활용하게 됩니다.
    public void NetworkSpawn(NetworkConnectionToClient conn, int playerIndex) {
        if (playerIndex >= spawnPoints.Length) return;

        Transform startPos = spawnPoints[playerIndex];
        GameObject player = Instantiate(playerPrefab, startPos.position, startPos.rotation);

        // 서버에 이 플레이어를 등록
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}
