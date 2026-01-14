using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class ItemSpawner : NetworkBehaviour
{
    [Header("--- 밸런스 설정 ---")]
    [Tooltip("아이템 박스 프리팹")]
    [SerializeField] private GameObject itemBoxPrefab;

    [SerializeField] private float spawnInterval = 15f;
    [SerializeField] private float maxSpawnRadius = 20f; // 최대 반경 20
    [SerializeField] private float spawnHeight = 15f;    // 생성 높이

    // [핵심 변경] 스포너의 위치(transform) 대신 사용할 맵의 절대 중심점
    // 맵이 (0,0,0)이 아니라면 이 값을 인스펙터에서 조정하세요.
    [SerializeField] private Vector3 mapCenterPoint = Vector3.zero;

    public override void OnStartServer()
    {
        InvokeRepeating(nameof(SpawnItem), spawnInterval, spawnInterval);
    }

    [Server]
    protected void SpawnItem()
    {
        if (itemBoxPrefab == null) return;

        // 1. 맵 축소 비율 가져오기
        float currentRatio = 1f;
        if (MapShrinker.Instance != null)
        {
            currentRatio = MapShrinker.Instance.CurrentScaleRatio;
        }

        // 2. 동적 반경 계산 (최대 20 * 비율)
        float currentRadius = maxSpawnRadius * currentRatio;

        // 3. 랜덤 좌표 계산
        Vector2 randomCircle = Random.insideUnitCircle * currentRadius;

        // [문제 해결의 핵심]
        // transform.position(스포너 위치)을 아예 쓰지 않습니다.
        // 스포너가 떨어지든 말든, 무조건 설정해둔 mapCenterPoint(0,0,0) 기준으로만 계산합니다.
        Vector3 spawnPos = new Vector3(
            mapCenterPoint.x + randomCircle.x,
            mapCenterPoint.y + spawnHeight, // 높이도 고정값 사용
            mapCenterPoint.z + randomCircle.y
        );

        // 4. 아이템 생성
        GameObject item = Instantiate(itemBoxPrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(item);
    }

    // 기즈모도 스포너 위치가 아니라 맵 중심 기준으로 그립니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        // 스포너가 어디 있든 기즈모는 맵 중앙에 표시됨
        Vector3 center = mapCenterPoint + Vector3.up * spawnHeight;
        Gizmos.DrawWireSphere(center, maxSpawnRadius);

        if (Application.isPlaying && MapShrinker.Instance != null)
        {
            Gizmos.color = Color.red;
            float ratio = MapShrinker.Instance.CurrentScaleRatio;
            Gizmos.DrawWireSphere(center, maxSpawnRadius * ratio);
        }
    }
}
