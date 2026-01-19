using UnityEngine;
using Mirror;
using System.Collections;

public class UserInfoManager : NetworkBehaviour
{
    [Header("Network Synced Data")]
    [SyncVar(hook = nameof(OnNicknameChange))] public string PlayerNickname = "";
    [SyncVar(hook = nameof(OnRateChange))] public int PlayerRate = 0;
    [SyncVar(hook = nameof(OnNumChange))] public int PlayerNum = 0;
    [SyncVar(hook = nameof(OnReadyChange))] public bool isReady = false;

    private Lobby_UI_Controller lobbyUI;
    private NetworkRoomPlayer roomPlayer;

    public override void OnStartClient()
    {
        base.OnStartClient();
        roomPlayer = GetComponent<NetworkRoomPlayer>();
        if (lobbyUI == null) lobbyUI = FindAnyObjectByType<Lobby_UI_Controller>();

        RefreshUI();
        StartCoroutine(C_SendInitialInfo());
    }

    public void OnReadyStatusChanged(bool oldValue, bool newValue)
    {
        isReady = newValue;
        RefreshUI();
    }

    public override void OnStartServer()
    {
        StartCoroutine(C_StartRegistry());
    }

    [ServerCallback]
    private void Update()
    {
        if (roomPlayer != null && isReady != roomPlayer.readyToBegin)
        {
            isReady = roomPlayer.readyToBegin;
            ServerPlayerRegistry.instance?.TryStartGame();
        }
    }

    public void ToggleReady()
    {
        if (!isLocalPlayer) return; // 내 객체일 때만 서버에 요청 가능

        // 서버에 내 레디 상태를 반전시켜달라고 요청
        CmdSetReadyState(!isReady);
    }
    [Command]
    void CmdSetReadyState(bool state)
    {
        this.isReady = state; // 서버에서 값이 바뀌면 SyncVar에 의해 모든 클라이언트의 OnReadyChange가 실행됨

        // 서버 레지스트리에 알림 (게임 시작 체크용)
        if (ServerPlayerRegistry.instance != null)
            ServerPlayerRegistry.instance.TryStartGame();
    }
    IEnumerator C_StartRegistry()
    {
        yield return new WaitUntil(() => connectionToClient != null);
        ServerPlayerRegistry.instance?.RegisterPlayer(this);
    }

    IEnumerator C_SendInitialInfo()
    {
        while (DataManager.instance == null) yield return null;
        if (DataManager.instance.playerInfo != null)
        {
            // 내 객체일 때만 내 정보를 서버에 보냄
            if (isLocalPlayer)
                CmdRequestSetInfo(DataManager.instance.playerInfo.User_Nic, DataManager.instance.playerInfo.User_Rate);
        }
    }

    [Command]
    void CmdRequestSetInfo(string nic, int rate)
    {
        this.PlayerNickname = nic;
        this.PlayerRate = rate;
    }

    public override void OnStopServer()
    {
        ServerPlayerRegistry.instance?.UnregisterPlayer(this);
    }

    public void AssignPlayerNumber(int number)
    {
        PlayerNum = number;
    }

    [Command]
    public void CmdSendReadyToServer(bool ready)
    {
        if (ServerPlayerRegistry.instance != null)
        {
            ServerPlayerRegistry.instance.TryStartGame();
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
    }

    #region Hooks (UI 갱신 로직)
    void OnNicknameChange(string oldV, string newV) => RefreshUI();
    void OnRateChange(int oldV, int newV) => RefreshUI();
    void OnNumChange(int oldV, int newV)
    {
        if (isLocalPlayer && DataManager.instance != null)
            DataManager.instance.playerInfo.PlayerNum = newV;
        RefreshUI();
    }
    void OnReadyChange(bool oldV, bool newV) 
    {
        Debug.Log($"[SyncVar] {PlayerNickname}의 레디 상태 변경: {newV}");
        RefreshUI();
    }
private void RefreshUI()
    {
        if (lobbyUI == null) lobbyUI = FindAnyObjectByType<Lobby_UI_Controller>();

        // 이제 roomPlayer 대신 SyncVar인 isReady를 직접 사용합니다.
        if (lobbyUI != null && PlayerNum > 0 && PlayerNum <= 4)
        {
            lobbyUI.UpdatePlayerFrameColor(PlayerNum - 1, isReady);
            lobbyUI.UpdateSlotText(PlayerNum - 1, PlayerNickname, PlayerRate);
        }
    }
    #endregion
}