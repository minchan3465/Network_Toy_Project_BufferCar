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
        // 게임 시작 후 첫 대기
        yield return new WaitForSeconds(spawnInterval);

        while (true)
        {
            // 1. 아이템이 생성될 위치를 '미리' 계산
            Vector3 targetSpawnPos = CalculateSpawnPosition();

            // 2. 경고 단계: 해당 위치에서 소리 재생 (SoundManager 활용)
            // "ItemDrop"이라는 키워드로 SoundManager에 등록되어 있어야 합니다.
            if (SoundManager.instance != null)
            {
                // 소리는 모든 클라이언트에게 들려야 하므로 SoundManager의 ClientRpc 호출
                SoundManager.instance.PlaySFXPoint("ItemDropSFX", targetSpawnPos, 1.0f);
            }

            // [선택 사항] 여기에 "바닥에 빨간 원(Warning Circle)" 같은 시각 효과도 넣을 수 있습니다.

            // 3. 대기 단계: 소리가 들리고 나서 아이템이 떨어질 때까지 기다림
            yield return new WaitForSeconds(warningDelay);

            // 4. 스폰 단계: 아까 계산해둔 위치에 실제 아이템 생성
            SpawnItem(targetSpawnPos);

            // 5. 다음 쿨타임 대기
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
