using UnityEngine;

public class ScaleFollower : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("맵보다 얼마나 더 빨리 줄어들지 배율 (1.0 = 똑같이, 1.5 = 1.5배 빨리)")]
    [SerializeField] private float shrinkSpeedMultiplier = 1.2f;

    [Tooltip("너무 작아지는 것 방지 (최소 크기 비율)")]
    [SerializeField] private float minScaleRatio = 0.1f;

    private Vector3 initialScale;

    void Start()
    {
        // 시작할 때 자신의 크기를 기억해둡니다.
        initialScale = transform.localScale;
    }

    void Update()
    {
        // 맵 축소 관리자가 없으면 아무것도 안 함
        if (MapShrinker.Instance == null) return;

        // 1. 현재 맵이 얼마나 줄어들었는지 비율을 가져옵니다. (1.0 -> 0.4)
        float mapRatio = MapShrinker.Instance.CurrentScaleRatio; //

        // 2. "줄어든 양"을 계산합니다. (예: 맵이 0.9면 줄어든 양은 0.1)
        float lostAmount = 1.0f - mapRatio;

        // 3. 줄어든 양에 가속도(배수)를 곱합니다. (예: 0.1 * 1.5배 = 0.15만큼 줄어듦)
        float myLostAmount = lostAmount * shrinkSpeedMultiplier;

        // 4. 최종 비율 계산 (1.0 - 내 감소량)
        float myRatio = 1.0f - myLostAmount;

        // 5. 너무 작아져서 뒤집히지 않게 최소값 제한 (Clamp)
        myRatio = Mathf.Max(myRatio, minScaleRatio);

        // 6. 크기 적용
        transform.localScale = initialScale * myRatio;
    }
}
