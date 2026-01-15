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

        // [복구됨] 1. 게임 시작/종료 상태 변화 감지 로직
        // "어? 방금 전까진 게임이 안 시작됐었는데(false), 지금 보니 시작됐네(true)?" -> 이게 '시작되는 순간'입니다.
        if (GameManager.Instance.isGameStart != wasGameStarted)
        {
            if (GameManager.Instance.isGameStart)
            {
                // [게임 시작 순간] -> 초기화 실행!
                Debug.Log("[GameDirector] 게임 시작 감지! 맵 초기화 및 청소 실행");
                InitializeRound();
            }
            else
            {
                // [게임 종료 순간]
                Debug.Log("[GameDirector] 게임 종료 감지.");
            }

            // 상태 갱신 (현재 상태를 기억해둠)
            wasGameStarted = GameManager.Instance.isGameStart;
        }

        // 2. 시간 감지 로직 (맵 축소 타이밍)
        if (GameManager.Instance.isGameStart)
        {
            // 설정된 시간(shrinkStartTime)이 되었고, 아직 축소 명령을 안 내렸다면
            int triggerTime = 60; // 기본값
            if (MapShrinker.Instance != null) triggerTime = MapShrinker.Instance.shrinkStartTime;

            if (GameManager.Instance.gameTime == triggerTime && !hasTriggeredShrink)
            {
                Debug.Log($"[GameDirector] {triggerTime}초 감지! 맵 축소 명령 실행.");

                if (MapShrinker.Instance != null)
                {
                    MapShrinker.Instance.StartShrinking();
                    hasTriggeredShrink = true; // 중복 실행 방지
                }
            }
        }
    }

    [Server]
    private void InitializeRound()
    {
        Debug.Log("[GameDirector] 라운드 초기화 시작...");

        // 1. 맵 크기 원상 복구
        if (MapShrinker.Instance != null)
        {
            MapShrinker.Instance.ResetMap(); //
        }

        // 2. 바닥에 떨어진 아이템 삭제
        ClearAllItems();

        // 3. 축소 트리거 초기화 (다음 판을 위해)
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
