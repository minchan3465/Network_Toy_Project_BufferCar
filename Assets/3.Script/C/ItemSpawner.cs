using UnityEngine;
using Mirror;

public class ItemSpawner : NetworkBehaviour
{
    public GameObject itemPrefab;
    public float spawnInterval = 15f;

    // 서버 전용 시작 함수
    public override void OnStartServer()
    {
        InvokeRepeating(nameof(SpawnItem), spawnInterval, spawnInterval);
    }

    // 3. [Server] 특성은 오직 '메서드' 위에만 위치해야 합니다.
    // 4. Mirror 90+ 버전에서는 protected나 public 권장 (일부 컴파일러 이슈 방지)
    [Server]
    protected void SpawnItem()
    {
        if (itemPrefab == null) return;

        Vector3 spawnPos = new Vector3(Random.Range(-5, 5), 10, Random.Range(-5, 5));
        GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        // 5. 서버에서 생성 후 모든 클라이언트에 전파
        NetworkServer.Spawn(item);
    }
}
