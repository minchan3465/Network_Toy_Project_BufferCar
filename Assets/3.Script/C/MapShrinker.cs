using UnityEngine;
using Mirror;

public class MapShrinker : NetworkBehaviour
{
    public static MapShrinker Instance;

    [Header("줄어들기 설정")]
    [Tooltip("게임 시작 후 몇 초가 남았을 때 줄어들기 시작할까요?")]
    public int shrinkStartTime = 60; // [추가됨] 여기서 시간 관리!

    [SerializeField] private float shrinkDuration = 30f;
    [SerializeField] private float targetRatio = 0.4f;

    public float CurrentScaleRatio { get; private set; } = 1f;

    private Vector3 initialScale;
    private Vector3 targetScale;
    private float elapsedTime = 0f;

    [SyncVar]
    private bool isShrinking = false;

    private void Awake()
    {
        Instance = this;
        initialScale = transform.localScale;
        targetScale = initialScale * targetRatio;
    }

    [Server]
    public void StartShrinking()
    {
        if (isShrinking) return;

        isShrinking = true;
        elapsedTime = 0f;
        this.enabled = true;

        //[추가] 누가 줄어드는지 이름 출력
        Debug.Log($"[범인 색출] 현재 줄어들고 있는 오브젝트 이름: {gameObject.name}");
    }

    [Server]
    public void ResetMap()
    {
        isShrinking = false;
        elapsedTime = 0f;
        CurrentScaleRatio = 1f;

        // 서버 크기 복구
        transform.localScale = initialScale;
        this.enabled = false;

        // 클라이언트 크기 복구 명령
        RpcResetMap(initialScale);
        Debug.Log("[MapShrinker] 맵 크기 초기화 완료.");
    }

    [ClientRpc]
    private void RpcResetMap(Vector3 originScale)
    {
        transform.localScale = originScale;
    }

    [ServerCallback]
    private void Update()
    {
        if (!isShrinking) return;

        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / shrinkDuration;

        CurrentScaleRatio = Mathf.Lerp(1f, targetRatio, progress);
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
        transform.localScale = targetScale;
        RpcStopShrinking(targetScale);
    }

    [ClientRpc]
    private void RpcStopShrinking(Vector3 finalScale)
    {
        transform.localScale = finalScale;
    }
}
