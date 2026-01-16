using Mirror;
using UnityEngine;
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPlayerNumberChanged))] public int playerNumber;
    [SyncVar] public string nickname;
    [SyncVar(hook = nameof(OnReadyChanged))] public bool isReady;

    #region Server Side
    [Server]
    public void AssignPlayerNumber(int number)
    {
        playerNumber = number;
    }

    private void OnPlayerNumberChanged(int _, int newValue)
    {
        if (!isLocalPlayer) return;
        if (DataManager.instance == null) return;

        DataManager.instance.playerInfo.PlayerNum = newValue;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // [수정 완료] 여기에 있던 에러 유발 코드(GameManager.RegisterPlayer)를 모두 삭제했습니다.
        // 등록 요청은 아래 'OnStartLocalPlayer'에서 'CmdRequestRegister'를 통해 안전하게 처리됩니다.
    }

    //[ClientCallback]
    //public void OnDestroy()
    //{
    //    if (ServerPlayerRegistry.instance != null)
    //    {
    //        ServerPlayerRegistry.instance.UnregisterPlayer(this);
    //    }
    //}
    #endregion

    #region Client Side
    public override void OnStartLocalPlayer()
    {
        // 1. 닉네임 설정
        string nick = PlayerPrefs.GetString("NICKNAME", "Player");
        CmdSetNickname(nick);

        // 2. 서버에게 "나 등록해줘"라고 요청 보내기 (이게 정석입니다)
        CmdRequestRegister();
    }

    [Command]
    private void CmdSetNickname(string name)
    {
        nickname = name;
    }

    [Command]
    public void CmdSetReady(bool ready)
    {
        isReady = ready;
        ServerPlayerRegistry.instance.TryStartGame();
    }

    private void OnReadyChanged(bool _, bool newValue)
    {
        Debug.Log($"Player {playerNumber} Ready: {newValue}");
    }

    [Command]
    public void CmdRequestRegister()
    {
        // 이 함수는 '서버'에서 실행됩니다.
        if (GameManager.Instance != null)
        {
            // 1. 내 게임오브젝트에 붙어있는 PlayerManager를 찾습니다.
            PlayerManager pm = GetComponent<PlayerManager>();

            if (pm != null)
            {
                // 2. 매개변수 2개(PlayerManager, NetworkPlayer)를 정확히 맞춰서 호출합니다.
                GameManager.Instance.RegisterPlayer(pm, this);
            }
            else
            {
                Debug.LogError("[NetworkPlayer] PlayerManager 컴포넌트를 찾을 수 없습니다!");
            }
        }
    }
    #endregion
}