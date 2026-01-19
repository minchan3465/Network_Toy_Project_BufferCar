using UnityEngine;
using Mirror;
using System.Collections;

public class UserInfoManager : NetworkRoomPlayer
{
    [Header("Network Synced Data")]
    [SyncVar(hook = nameof(OnNicknameChange))] public string PlayerNickname = "";
    [SyncVar(hook = nameof(OnRateChange))] public int PlayerRate = 0;
    [SyncVar(hook = nameof(OnNumChange))] public int PlayerNum = 0;
    [SyncVar(hook = nameof(OnReadyChange))] public bool isReady = false;

    private Lobby_UI_Controller lobbyUI;

    // SyncVar 데이터가 서버로부터 처음 동기화된 직후 UI를 강제로 업데이트합니다.
    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(C_SendInitialInfo());
        RefreshUI();
    }
    public override void OnStartServer()
    {
        StartCoroutine(C_StartRegistry());
    }

    // 룸 플레이어의 상태가 바뀔 때 호출되는 Mirror 내장 콜백
    public override void OnClientEnterRoom()
    {
        RefreshUI();
    }
    //서버에 접속해서 나의 플레이어 오브젝트(UserInfoManager)가 내 화면에 나타나는 순간 실행됩니다.
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();


        //if (DataManager.instance != null && DataManager.instance.playerInfo != null)
        //{
        //
        //    // 2. 바통 터치 (로컬 DB 데이터를 가져옴)
        //    // DataManager(DB)에서 정보를 가져와 서버로 보고
        //    string nic = DataManager.instance.playerInfo.User_Nic;
        //    int rate = DataManager.instance.playerInfo.User_Rate;
        //
        //    // 3. 서버 보고 (서버에 있는 모든 사람에게 내 정보를 퍼뜨림)
        //    CmdRequestSetInfo(nic, rate);
        //}
    }
    IEnumerator C_StartRegistry()
    {
        // 1. Registry가 준비될 때까지 대기
        while (ServerPlayerRegistry.instance == null)
        {
            yield return null;
        }

        Debug.Log($"[Player] OnStartServer registry={(ServerPlayerRegistry.instance == null ? "NULL" : "OK")}");
        ServerPlayerRegistry.instance.RegisterPlayer(this);
        Debug.Log("Sucessssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssss");
        // 2. DataManager 확인 및 데이터 준비
        if (DataManager.instance != null && DataManager.instance.playerInfo != null)
        {
            string nic = DataManager.instance.playerInfo.User_Nic;
            int rate = DataManager.instance.playerInfo.User_Rate;

            // 3. 서버에 내 정보 등록 요청
            Debug.Log("CmdRequestSetInfo is startttttttttttttttt");
            Debug.Log("nic is "+ nic);
            Debug.Log("rate is "+ rate);
            CmdRequestSetInfo(nic, rate);
        }
    }
    IEnumerator C_SendInitialInfo()
    {
        // 1. Registry가 준비될 때까지 대기
        // 2. DataManager 확인 및 데이터 준비
        while (DataManager.instance == null)
        {
            yield return null;
        }
        string nic;
        int rate;
        if (DataManager.instance.playerInfo != null)
        {
            nic = DataManager.instance.playerInfo.User_Nic;
            rate = DataManager.instance.playerInfo.User_Rate;

            // 3. 서버에 내 정보 등록 요청
            Debug.Log("CmdRequestSetInfo is startttttttttttttttt");
            Debug.Log("nic is " + nic);
            Debug.Log("rate is " + rate);
            CmdRequestSetInfo(nic, rate);
        }
        else
        {
            nic = null;
            rate = -1;
            CmdRequestSetInfo(nic, rate);
        }
    }

    [Command]
    void CmdRequestSetInfo(string nic, int rate)
    {
        // 서버 메모리에 저장 (이후 SyncVar를 통해 모든 클라이언트에게 전파됨)
        this.PlayerNickname = nic;
        this.PlayerRate = rate;

        // 서버 레지스트리에 나를 등록하고 번호 할당 요청
        //if (ServerPlayerRegistry.instance != null)
        //{
        //    ServerPlayerRegistry.instance.RegisterPlayer(this);
        //}
    }

    [Command]
    public void CmdSendReadyToServer(bool ready)
    {
        this.isReady = ready;
        // 레디 상태가 바뀔 때마다 서버 레지스트리에게 "다 준비됐어?"라고 물어봅니다.
        if (ServerPlayerRegistry.instance != null)
        {
            ServerPlayerRegistry.instance.TryStartGame();
        }
    }

    // [보완 추천] 유저가 나갈 때 내 화면에서 해당 슬롯을 즉시 끄기 위해 추가
    public override void OnStopClient()
    {
        if (lobbyUI != null && PlayerNum > 0)
        {
            //lobbyUI.UpdateSlotText(PlayerNum - 1, "", 0);
        }
        base.OnStopClient();
    }
    public override void OnStopServer()
    {
        if (ServerPlayerRegistry.instance != null)
            ServerPlayerRegistry.instance.UnregisterPlayer(this);
    }

    // 서버(Registry)에서 번호를 부여할 때 호출하는 함수
    public void AssignPlayerNumber(int number)
    {
        Debug.Log($"[Client] catch number from server : {number}"); // 이 로그가 찍혀야 성공입니다.
        PlayerNum = number;

        //DataManager.instance.playerInfo.PlayerNum = number;
        // 서버 환경이라면 즉시 UI 갱신 (호스트 유저용)
        if (isServer)
        {
            //RefreshUI();
        }
    }

    #region Hooks (UI 갱신 로직)
    void OnNicknameChange(string oldV, string newV) => RefreshUI();
    void OnRateChange(int oldV, int newV) => RefreshUI();
    void OnNumChange(int oldV, int newV) 
    {
        Debug.Log($"[Client] PlayerNum changed: {newV}");

        if (!isLocalPlayer) return;

        if (DataManager.instance != null)
        {
            Debug.Log($"DataManager.instance.playerInfo.PlayerNum is changed: {newV}");
            DataManager.instance.playerInfo.PlayerNum = newV;
        }
        RefreshUI();
    }
    void OnReadyChange(bool oldV, bool newV) => RefreshUI();

    private void RefreshUI()
    {
        if (lobbyUI == null) lobbyUI = FindAnyObjectByType<Lobby_UI_Controller>();

        if (lobbyUI != null)
        {
            // index와 readyToBegin은 상속받은 NetworkRoomPlayer에 이미 들어있는 변수입니다.
            lobbyUI.UpdatePlayerFrameColor(index, readyToBegin);
            lobbyUI.UpdateSlotText(index, PlayerNickname, PlayerRate);
        }
    }

    #endregion


}