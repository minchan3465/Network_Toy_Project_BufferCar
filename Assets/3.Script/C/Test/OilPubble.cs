using UnityEngine;
using Mirror;

public class OilPubble : NetworkBehaviour
{
    [SerializeField] private float duration = 2f; // 미끄러지는 시간

    [ServerCallback] // 서버에서만 실행
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var controller = other.GetComponent<PlayerController>();
            if (controller != null)
            {
                // 플레이어에게 미끄러짐 효과 적용
                controller.Svr_StartSlip(duration);
                // 밟은 오일은 바로 사라짐 (선택 사항)
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
