using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class ItemSpawner : NetworkBehaviour
{
    [Header("--- 밸런스 설정 ---")]
    [Tooltip("아이템 박스 프리팹")]
    [SerializeField] private GameObject itemBoxPrefab;

    [Tooltip("아이템 생성 주기 (쿨타임)")]
    [SerializeField] private float spawnInterval = 15f;

    [Tooltip("소리 재생 후 아이템이 나올 때까지의 대기 시간")]
    [SerializeField] private float warningDelay = 2.0f; // [추가됨] 경고 시간

    [SerializeField] private float maxSpawnRadius = 20f;
    [SerializeField] private float spawnHeight = 15f;

    [Header("--- 위치 보정 ---")]
    [SerializeField] private Vector3 mapCenterPoint = Vector3.zero;

    public override void OnStartServer()
    {
        // InvokeRepeating 대신 코루틴 루프 시작
        StartCoroutine(SpawnLoop());
    }

    [Server]
    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(spawnInterval);

        while (true)
        {
            // [추가] GameManager가 존재하고, 게임 시작 상태일 때만 스폰
            if (GameManager.Instance != null && GameManager.Instance.isGameStart)
            {
                Vector3 targetSpawnPos = CalculateSpawnPosition();

                if (SoundManager.instance != null)
                    SoundManager.instance.PlaySFXPoint("ItemDropSFX", targetSpawnPos, 1.0f);

                yield return new WaitForSeconds(warningDelay);

                // 대기하는 동안 게임이 끝났을 수도 있으니 한 번 더 체크
                if (GameManager.Instance.isGameStart)
                {
                    SpawnItem(targetSpawnPos);
                }
            }
            else
            {
                // 게임 중이 아니면 잠시 대기 (1초)
                yield return new WaitForSeconds(1f);
                continue; // 아래 spawnInterval 대기 건너뛰고 다시 루프 시작
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // 위치 계산 로직 분리
    private Vector3 CalculateSpawnPosition()
    {
        float currentRatio = 1f;
        if (MapShrinker.Instance != null)
        {
            currentRatio = MapShrinker.Instance.CurrentScaleRatio;
        }

        float currentRadius = maxSpawnRadius * currentRatio;
        Vector2 randomCircle = Random.insideUnitCircle * currentRadius;

        return new Vector3(
            mapCenterPoint.x + randomCircle.x,
            mapCenterPoint.y + spawnHeight,
            mapCenterPoint.z + randomCircle.y
        );
    }

    [Server]
    private void SpawnItem(Vector3 pos)
    {
        if (itemBoxPrefab == null) return;

        GameObject item = Instantiate(itemBoxPrefab, pos, Quaternion.identity);
        NetworkServer.Spawn(item);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
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
