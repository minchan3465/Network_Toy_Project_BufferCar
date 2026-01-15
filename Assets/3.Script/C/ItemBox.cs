using UnityEngine;
using Mirror;

public class ItemBox : NetworkBehaviour
{
    [Header("--- 설정 ---")]
    [Tooltip("아이템이 생성된 후 자동으로 사라지는 시간 (초)")]
    [SerializeField] private float lifeTime = 10f;

    private bool isTriggered = false; // 중복 습득 방지용 플래그

    public override void OnStartServer()
    {
        Invoke(nameof(TimeoutDestroy), lifeTime);
    }

    [Server]
    private void TimeoutDestroy()
    {
        if (gameObject != null)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        // 이미 누군가 건드렸으면 로직 무시
        if (isTriggered) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            var handler = collision.gameObject.GetComponent<ItemEffectHandler>();

            if (handler != null)
            {
                // 깃발을 꽂아서 두 번 다시 못 들어오게 함
                isTriggered = true;

                CancelInvoke(nameof(TimeoutDestroy));

                int randomEffect = Random.Range(0, 3);
                handler.Svr_ApplyItemEffect(randomEffect);

                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
