using UnityEngine;
using Mirror;

public class ItemBox : NetworkBehaviour
{
    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 0~4까지 5종류의 효과 랜덤 추첨
            int randomEffect = Random.Range(0, 5);

            ApplyEffect(other.gameObject, randomEffect);

            // 서버에서 박스 파괴 (모든 클라이언트 동기화)
            NetworkServer.Destroy(gameObject);
        }
    }

    [Server]
    private void ApplyEffect(GameObject player, int index)
    {
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            // 플레이어 컨트롤러의 효과 적용 함수 호출
            controller.Svr_ApplyItemEffect(index);
        }
    }
}
