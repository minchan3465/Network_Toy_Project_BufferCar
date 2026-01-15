using UnityEngine;
using Mirror;

public class ItemBox : NetworkBehaviour
{
    [Header("--- 설정 ---")]
    [Tooltip("아이템이 생성된 후 자동으로 사라지는 시간 (초)")]
    [SerializeField] private float lifeTime = 10f; // 기본 10초

    // 서버에서 아이템이 스폰될 때 딱 한 번 실행됩니다.
    public override void OnStartServer()
    {
        // lifeTime(초) 뒤에 DestroySelf 함수를 실행하도록 예약합니다.
        Invoke(nameof(TimeoutDestroy), lifeTime);
    }

    // 시간이 다 되었을 때 실행되는 함수
    [Server]
    private void TimeoutDestroy()
    {
        // 이미 누가 먹어서 사라진 상태라면 무시
        if (gameObject != null)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var handler = collision.gameObject.GetComponent<ItemEffectHandler>();

            if (handler != null)
            {
                // 충돌 시 예약된 자동 삭제(Invoke) 취소 (혹시 모를 중복 방지)
                CancelInvoke(nameof(TimeoutDestroy));

                int randomEffect = Random.Range(0, 3);
                handler.Svr_ApplyItemEffect(randomEffect);

                // 플레이어가 먹었으므로 즉시 삭제
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
