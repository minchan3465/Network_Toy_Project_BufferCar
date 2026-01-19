using UnityEngine;
using Mirror;
using System.Collections;

public class UserInfoManager : NetworkBehaviour
{
    [Header("Network Synced Data")]
    [SyncVar(hook = nameof(OnNicknameChange))] public string PlayerNickname = "";
    [SyncVar(hook = nameof(OnRateChange))] public int PlayerRate = 0;
    [SyncVar(hook = nameof(OnNumChange))] public int PlayerNum = 0;

    private Lobby_UI_Controller lobbyUI;
    private NetworkRoomPlayer roomPlayer;

    private bool lastReadyState; // 이전 레디 상태 기억
    // 현재 레디 상태를 외부(Registry 등)에서 확인할 수 있는 프로퍼티
    public bool IsReady => roomPlayer != null && roomPlayer.readyToBegin;

    public override void OnStartClient()
    {
        base.OnStartClient();
        roomPlayer = GetComponent<NetworkRoomPlayer>();
        if (lobbyUI == null) lobbyUI = FindAnyObjectByType<Lobby_UI_Controller>();

        RefreshUI();
        StartCoroutine(C_SendInitialInfo());
    }

    private void Update()
    {
        if (roomPlayer == null) roomPlayer = GetComponent<NetworkRoomPlayer>();

        // 상태가 실제로 변했을 때만 UI 갱신 (매 프레임 UI 연산 방지)
        if (roomPlayer != null && roomPlayer.readyToBegin != lastReadyState)
        {
            lastReadyState = roomPlayer.readyToBegin;
            RefreshUI();

            // 서버라면 레디 상태가 변할 때마다 게임 시작 가능 여부 체크
            if (isServer)
            {
                ServerPlayerRegistry.instance?.TryStartGame();
            }
        }
    }

    public void ToggleReady()
    {
        if (!isLocalPlayer || roomPlayer == null) return;

        // [핵심] Mirror 내부 명령어를 직접 호출하여 상태를 바꿉니다.
        roomPlayer.CmdChangeReadyState(!roomPlayer.readyToBegin);
    }

    public void RefreshUI()
    {
        if (lobbyUI == null) lobbyUI = FindAnyObjectByType<Lobby_UI_Controller>();
        if (roomPlayer == null) roomPlayer = GetComponent<NetworkRoomPlayer>();

        if (lobbyUI != null && PlayerNum > 0 && PlayerNum <= 4)
        {
            // [기준] roomPlayer.readyToBegin 값을 UI 색상 기준으로 사용
            lobbyUI.UpdatePlayerFrameColor(PlayerNum - 1, roomPlayer.readyToBegin);
            lobbyUI.UpdateSlotText(PlayerNum - 1, PlayerNickname, PlayerRate);
        }
    }

    // --- 데이터 변경 시 리프레시 ---
    void OnNicknameChange(string oldV, string newV) => RefreshUI();
    void OnRateChange(int oldV, int newV) => RefreshUI();
    void OnNumChange(int oldV, int newV)
    {
        if (isLocalPlayer && DataManager.instance != null)
            DataManager.instance.playerInfo.PlayerNum = newV;
        RefreshUI();
    }

    // --- 서버 관련 로직 (기존 유지) ---
    public override void OnStartServer() => StartCoroutine(C_StartRegistry());
    IEnumerator C_StartRegistry()
    {
        yield return new WaitUntil(() => connectionToClient != null);
        ServerPlayerRegistry.instance?.RegisterPlayer(this);
    }
    IEnumerator C_SendInitialInfo()
    {
        while (DataManager.instance == null) yield return null;
        if (isLocalPlayer) CmdRequestSetInfo(DataManager.instance.playerInfo.User_Nic, DataManager.instance.playerInfo.User_Rate);
    }
    [Command] void CmdRequestSetInfo(string nic, int rate) { this.PlayerNickname = nic; this.PlayerRate = rate; }
    public override void OnStopServer() => ServerPlayerRegistry.instance?.UnregisterPlayer(this);
    public void AssignPlayerNumber(int number) => PlayerNum = number;
}