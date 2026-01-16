using UnityEngine;
using Mirror;

public class ItemBox : NetworkBehaviour
{
    [Header("--- 설정 ---")]
    [Tooltip("아이템이 생성된 후 자동으로 사라지는 시간 (초)")]
    [SerializeField] private float lifeTime = 10f;

    private bool isTriggered = false; // 중복 습득 방지용
    private bool hasLanded = false;   // 땅에 착지했는지 여부

    // 컴포넌트 캐싱
    private Collider myCollider;
    private Rigidbody myRb;

    public override void OnStartServer()
    {
        myCollider = GetComponent<Collider>();
        myRb = GetComponent<Rigidbody>();

        // [초기 상태] 물리 법칙을 따르도록 설정 (떨어지게 함)
        if (myCollider != null) myCollider.isTrigger = false;
        if (myRb != null)
        {
            myRb.useGravity = true;
            myRb.isKinematic = false;
        }

        Invoke(nameof(TimeoutDestroy), lifeTime);
    }

    [Server]
    private void TimeoutDestroy()
    {
        if (gameObject != null) NetworkServer.Destroy(gameObject);
    }

    // 1. 물리 충돌 감지 (땅에 닿거나, 공중에서 차가 들이받을 때)
    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if (isTriggered) return;

        // (A) 플레이어와 부딪힌 경우 (공중 캐치)
        if (collision.gameObject.CompareTag("Player"))
        {
            GetItem(collision.gameObject);
        }
        // (B) 땅(또는 다른 물체)에 닿은 경우 -> 트리거 모드로 변신!
        else if (!hasLanded && !collision.gameObject.CompareTag("ItemBox"))
        {
            // ItemBox끼리 부딪히는 건 무시하고, 바닥에 닿았을 때만 고정
            LandOnGround();
        }
    }

    // 2. 트리거 충돌 감지 (착지 후 차가 지나갈 때)
    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            GetItem(other.gameObject);
        }
    }

    // [핵심] 땅에 닿으면 유령(Trigger) 상태로 전환하여 고정시킴
    private void LandOnGround()
    {
        hasLanded = true;

        if (myRb != null)
        {
            myRb.useGravity = false;

            // 1. 먼저 속도를 0으로 죽이고 (물리 상태일 때)
            myRb.linearVelocity = Vector3.zero;
            myRb.angularVelocity = Vector3.zero; // 회전력도 없애주면 더 깔끔합니다.

            // 2. 그 다음에 "이제부터 움직이지 마(고정)" 선언
            myRb.isKinematic = true;
        }

        if (myCollider != null)
        {
            myCollider.isTrigger = true;
        }
    }

    // 아이템 획득 로직 (중복 코드 통합)
    private void GetItem(GameObject player)
    {
        if (isTriggered) return;

        var handler = player.GetComponent<ItemEffectHandler>();
        if (handler != null)
        {
            isTriggered = true;
            CancelInvoke(nameof(TimeoutDestroy));

            int randomEffect = Random.Range(0, 3);
            handler.Svr_ApplyItemEffect(randomEffect);

            NetworkServer.Destroy(gameObject);
        }
    }
}
