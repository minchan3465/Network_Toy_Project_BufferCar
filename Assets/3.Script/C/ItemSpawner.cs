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
    [SerializeField] private float warningDelay = 2.0f;

    [SerializeField] private float maxSpawnRadius = 20f;
    [SerializeField] private float spawnHeight = 15f;

    [Header("--- 사운드 설정 ---")]
    [Tooltip("아이템 스폰 경고음 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1.0f; // [추가됨] 볼륨 조절 변수

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
            // [복구] GameManager가 존재하고, 게임 시작 상태일 때만 스폰 (게임 끝나면 멈춤)
            if (GameManager.Instance != null && !GameManager.Instance.isGameStart)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // 1. 아이템이 생성될 위치를 '미리' 계산
            Vector3 targetSpawnPos = CalculateSpawnPosition();

            // 2. 경고 단계: 해당 위치에서 소리 재생
            RpcPlaySpawnSound("GearDropSFX");
            // (대기하는 동안 게임이 끝났을 수도 있으니 한 번 더 체크)
            if (GameManager.Instance == null || GameManager.Instance.isGameStart)
            {
                SpawnItem(targetSpawnPos);
            }

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
    [ClientRpc]
    private void RpcPlaySpawnSound(string name)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFX(name);
    }
}
