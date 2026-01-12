using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class ItemSpawner : NetworkBehaviour
{
    [Header("--- 밸런스 설정 ---")]
    [Tooltip("아이템 박스 하나만 넣어주세요")]
    [SerializeField] private GameObject itemBoxPrefab;

    [SerializeField] private float spawnInterval = 15f;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float spawnHeight = 15f;

    public override void OnStartServer()
    {
        // 서버 시작 시 15초마다 박스 생성 시작
        InvokeRepeating(nameof(SpawnItem), spawnInterval, spawnInterval);
    }

    [Server]
    protected void SpawnItem()
    {
        if (itemBoxPrefab == null)
        {
            Debug.LogError("ItemBox 프리팹이 할당되지 않았습니다!");
            return;
        }

        // 1. 랜덤 위치 계산
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

        // 2. 서버에서 단일 박스 생성
        GameObject item = Instantiate(itemBoxPrefab, spawnPos, Quaternion.identity);

        // 3. 모든 클라이언트에게 전파
        NetworkServer.Spawn(item);

        Debug.Log($"[ItemSpawner] 박스 투하 완료: {spawnPos}");
    }

    // 기획자용 시각 가이드
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + Vector3.up * spawnHeight;
        Gizmos.DrawWireSphere(center, spawnRadius);
        Gizmos.DrawLine(center, transform.position);
    }
}
