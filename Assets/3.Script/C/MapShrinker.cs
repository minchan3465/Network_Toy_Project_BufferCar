using UnityEngine;
using Mirror;

public class MapShrinker : NetworkBehaviour
{
    public static MapShrinker Instance; // 외부에서 접근하기 위한 싱글톤

    [Header("줄어들기 설정")]
    [SerializeField] private float startDelay = 5f;
    [SerializeField] private float shrinkDuration = 30f;
    [SerializeField] private float targetRatio = 0.4f; //

    // 현재 맵이 몇 % 크기인지 반환 (1.0 ~ 0.1)
    public float CurrentScaleRatio { get; private set; } = 1f;

    private Vector3 initialScale;
    private Vector3 targetScale;
    private float elapsedTime = 0f;
    private bool isShrinking = false;

    private void Awake()
    {
        Instance = this; // 싱글톤 할당
    }

    public override void OnStartServer()
    {
        initialScale = transform.localScale;
        targetScale = initialScale * targetRatio; //

        // 시작 시 비율은 1.0
        CurrentScaleRatio = 1f;

        Invoke(nameof(StartShrinking), startDelay);
    }

    [Server]
    private void StartShrinking()
    {
        isShrinking = true;
        Debug.LogWarning($"[MapShrinker] 맵 축소 시작!");
    }

    [ServerCallback]
    private void Update()
    {
        if (!isShrinking) return;

        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / shrinkDuration;

        // 진행률에 따라 현재 비율 갱신 (Lerp 사용)
        CurrentScaleRatio = Mathf.Lerp(1f, targetRatio, progress);

        // 실제 크기 적용
        transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);

        if (progress >= 1f)
        {
            FinishShrinking();
        }
    }

    [Server]
    private void FinishShrinking()
    {
        isShrinking = false;
        CurrentScaleRatio = targetRatio;
        transform.localScale = targetScale; // 최종 크기 확정

        // [핵심] 모든 클라이언트에게 "이제 그만! 이 크기로 고정해!" 라고 명령
        RpcStopShrinking(targetScale);

        Debug.Log("[MapShrinker] 축소 완료 및 고정.");
        this.enabled = false; // 서버 업데이트 중지
    }

    [ClientRpc]
    private void RpcStopShrinking(Vector3 finalScale)
    {
        // 1. NetworkTransform의 보간(Interpolation)을 끄기 위해 컴포넌트 비활성화
        var netTransform = GetComponent<NetworkTransformReliable>(); //
        if (netTransform != null) netTransform.enabled = false;

        // 2. 위치 강제 고정 (떨림 방지)
        transform.localScale = finalScale;

        // 3. 클라이언트 스크립트도 비활성화
        this.enabled = false;
    }
}
