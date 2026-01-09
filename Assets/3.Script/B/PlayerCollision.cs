using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float pushForce = 100; // 밀어내는 힘의 세기
    private Inputsystem input;
    private void OnEnable()
    {
        transform.TryGetComponent(out rb);
        input = FindAnyObjectByType<Inputsystem>();
    }
   
    private void OnTriggerStay(Collider other)//나만 밀립니다
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody otherRb = other.attachedRigidbody;
            if (otherRb == null || otherRb == rb) return;

            input.Enter();

            // 2. 방향 계산
            Vector3 pushDir = (other.transform.position - transform.position);
            pushDir.y = Random.Range(0f, 1f);
            pushDir.Normalize();

            // 3. 내 캐릭터는 반대로 밀림 (로컬에서 즉시 반응)
            rb.AddForce(-pushDir * pushForce, ForceMode.Acceleration);

            // 4. 상대방을 밀어내는 것은 상대방의 클라이언트가 처리하거나 
            // NetworkTransform이 내 위치를 밀어내는 결과를 동기화하게 됩니다.
        }
    }
}
