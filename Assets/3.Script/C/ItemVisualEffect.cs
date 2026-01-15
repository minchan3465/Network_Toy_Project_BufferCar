using UnityEngine;

public class ItemVisualEffect : MonoBehaviour
{
    [Header("--- 회전(Rotation) 설정 ---")]
    [Tooltip("X, Y, Z축별 회전 속도 (초당 각도)")]
    // [핵심 변경] 단일 float 대신 Vector3로 변경하여 모든 축 제어 가능
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 50f, 100f);

    [Header("--- 부유(Floating) 설정 ---")]
    [Tooltip("둥둥 떠다니는 높이 범위")]
    [SerializeField] private float floatAmplitude = 0.5f;

    [Tooltip("둥둥 떠다니는 속도")]
    [SerializeField] private float floatFrequency = 1f;

    private Vector3 startLocalPos;

    void Start()
    {
        // 시작할 때의 부모 기준 로컬 위치를 기억합니다.
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        // 1. 다중 축 회전 (Y축 + Z축 동시 회전 가능)
        // Space.Self를 사용하여 오브젝트가 기울어져도 자기 축 기준으로 돕니다.
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

        // 2. 부유 (Floating)
        float newY = startLocalPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;

        // 위치 적용 (X, Z는 원래 위치 유지, Y만 변경)
        transform.localPosition = new Vector3(startLocalPos.x, newY, startLocalPos.z);
    }
}
