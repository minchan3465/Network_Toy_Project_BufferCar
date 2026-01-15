using UnityEngine;
using Mirror;

public class GameDirector : MonoBehaviour
{
    // 이전 프레임의 게임 상태를 기억하기 위한 변수
    private bool wasGameStarted = false;
    private bool hasTriggeredShrink = false;

    [ServerCallback]
    private void Update()
    {
        if (GameManager.Instance == null) return;

        // [디버깅용 로그] 시간이 잘 흐르는지, 60초가 되었을 때 감지가 되는지 확인
        // (너무 많이 뜨면 60초 근처일 때만 뜨게 조건 걸기)
        if (GameManager.Instance.gameTime == 60)
        {
            Debug.Log($"[GameDirector] 60초 감지됨! (현재 hasTriggeredShrink 상태: {hasTriggeredShrink})");
        }

        // ... (기존 게임 시작/종료 감지 로직) ...

        // 2. 시간 감지 로직
        if (GameManager.Instance.isGameStart)
        {
            if (GameManager.Instance.gameTime == 60 && !hasTriggeredShrink)
            {
                Debug.Log("[GameDirector] 축소 명령 실행 시도..."); // 로그 추가

                if (MapShrinker.Instance != null)
                {
                    MapShrinker.Instance.StartShrinking();
                    hasTriggeredShrink = true;
                    Debug.Log("[GameDirector] MapShrinker에게 명령 전달 성공!"); // 로그 추가
                }
                else
                {
                    Debug.LogError("[GameDirector] 비상! MapShrinker가 없습니다 (Instance is null)"); // 로그 추가
                }
            }
        }
    }

    [Server]
    private void InitializeRound()
    {
        // 1. 맵 크기 원상 복구
        if (MapShrinker.Instance != null)
        {
            MapShrinker.Instance.ResetMap();
        }

        // 2. 바닥에 떨어진 아이템 삭제
        ClearAllItems();

        // 3. 축소 트리거 초기화 (다음 60초 때 다시 발동하도록)
        hasTriggeredShrink = false;
    }

    [Server]
    private void ClearAllItems()
    {
        GameObject[] items = GameObject.FindGameObjectsWithTag("ItemBox");
        foreach (var item in items)
        {
            NetworkServer.Destroy(item);
        }
        Debug.Log($"[GameDirector] 아이템 {items.Length}개 청소 완료.");
    }
}
