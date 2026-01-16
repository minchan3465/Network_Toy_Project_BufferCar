using UnityEngine;
using Mirror;
using System.Collections;

public class UserInfoManager : NetworkBehaviour
{
    public static UserInfoManager instance;

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
        //RefreshUI();
    }

    //서버에 접속해서 나의 플레이어 오브젝트(UserInfoManager)가 내 화면에 나타나는 순간 실행됩니다.
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        StartCoroutine(C_SendInitialInfo());

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
    IEnumerator C_SendInitialInfo()
    {
        // 1. Registry가 준비될 때까지 대기
        while (ServerPlayerRegistry.instance == null)
        {
            yield return null;
        }

        // 2. DataManager 확인 및 데이터 준비
        if (DataManager.instance != null && DataManager.instance.playerInfo != null)
        {
            string nic = DataManager.instance.playerInfo.User_Nic;
            int rate = DataManager.instance.playerInfo.User_Rate;

            // 3. 서버에 내 정보 등록 요청
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
        if (ServerPlayerRegistry.instance != null)
        {
            ServerPlayerRegistry.instance.RegisterPlayer(this);
        }
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
        if (ServerPlayerRegistry.instance != null)
            ServerPlayerRegistry.instance.UnregisterPlayer(this);
        if (lobbyUI != null && PlayerNum > 0)
        {
            //lobbyUI.UpdateSlotText(PlayerNum - 1, "", 0);
        }
        base.OnStopClient();
    }

    // 서버(Registry)에서 번호를 부여할 때 호출하는 함수
    public void AssignPlayerNumber(int number)
    {
        Debug.Log($"[Client] 서버로부터 번호를 받았습니다: {number}"); // 이 로그가 찍혀야 성공입니다.
        this.PlayerNum = number;
        DataManager.instance.playerInfo.PlayerNum = number;
        // 서버 환경이라면 즉시 UI 갱신 (호스트 유저용)
        if (isServer)
        {
            //RefreshUI();
        }
    }

    #region Hooks (UI 갱신 로직)
    void OnNicknameChange(string oldV, string newV) { }// => RefreshUI();
    void OnRateChange(int oldV, int newV) { }// => RefreshUI();
    void OnNumChange(int oldV, int newV) { }// => RefreshUI();
    void OnReadyChange(bool oldV, bool newV) { }// => RefreshUI();
    /*
    private void RefreshUI()
    {
        if (lobbyUI == null) lobbyUI = FindAnyObjectByType<Lobby_UI_Controller>();

        if (lobbyUI != null && PlayerNum > 0)
        {
            // [중요] 레이팅이나 닉네임이 바뀌었을 때 UI에 즉시 반영하는 로직이 필요합니다.
            lobbyUI.UpdatePlayerFrameColor(PlayerNum - 1, isReady);

            // 만약 UI에 닉네임/레이팅 텍스트 갱신 함수가 있다면 여기서 함께 호출하세요.
            lobbyUI.UpdateSlotText(PlayerNum - 1, PlayerNickname, PlayerRate);
        }
    }
     */
    #endregion


}