using UnityEngine;
using Mirror;

public class MapShrinker : NetworkBehaviour
{
    public static MapShrinker Instance;

    [Header("UI 연결")]
    [Tooltip("줄어들 때 띄울 경고 텍스트 오브젝트 (통째로 껐다 켭니다)")]
    [SerializeField] private GameObject warningUI; // [추가 1] UI 오브젝트 연결 슬롯

    [Header("줄어들기 설정")]
    [Tooltip("게임 시작 후 몇 초가 남았을 때 줄어들기 시작할까요?")]
    public int shrinkStartTime = 60; // [추가됨] 여기서 시간 관리!

    [SerializeField] private float shrinkDuration = 30f;
    [SerializeField] private Vector3 targetRatio = new Vector3(0.6f, 0.6f, 1.0f);

    public float CurrentScaleRatio { get; private set; } = 1f;

    private Vector3 initialScale;
    private Vector3 targetScale;
    private float elapsedTime = 0f;

    [SyncVar(hook = nameof(OnShrinkStateChange))]
    private bool isShrinking = false;

    private void Awake()
    {
        Instance = this;
        initialScale = transform.localScale;
        // Vector3.Scale을 쓰면 한 줄로 끝나서 훨씬 간결해!
        targetScale = Vector3.Scale(initialScale, targetRatio);
    }

    private void OnShrinkStateChange(bool oldState, bool newState)
    {
        // newState가 true면(줄어드는 중) UI 켜기
        // newState가 false면(끝남/리셋) UI 끄기
        if (warningUI != null)
        {
            warningUI.SetActive(newState);
        }
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
        float progress = Mathf.Min(elapsedTime / shrinkDuration, 1f);

        // 전체적인 진행률 파악을 위해 x축 비율을 대표로 저장
        CurrentScaleRatio = Mathf.Lerp(1f, targetRatio.x, progress);

        // 부드럽게 크기 변경
        transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);

        if (progress >= 1f) FinishShrinking();
    }

    [Server]
    private void FinishShrinking()
    {
        isShrinking = false;
        // targetRatio.x를 대입해서 float 타입을 맞춰줌
        CurrentScaleRatio = targetRatio.x;
        transform.localScale = targetScale;
        RpcStopShrinking(targetScale);
    }

    [ClientRpc]
    private void RpcStopShrinking(Vector3 finalScale)
    {
        transform.localScale = finalScale;
    }
}
