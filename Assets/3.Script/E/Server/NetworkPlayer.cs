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
        if (DataManager.instance == null)
            return;
        DataManager.instance.playerInfo.PlayerNum = newValue;
    }
    public override void OnStartServer()
    {
        Debug.Log($"[Player] OnStartServer registry={(ServerPlayerRegistry.instance == null ? "NULL" : "OK")}");
        ServerPlayerRegistry.instance.RegisterPlayer(this);
    }
    public override void OnStopServer()
    {
        if (ServerPlayerRegistry.instance != null) 
            ServerPlayerRegistry.instance.UnregisterPlayer(this);
    }
    #endregion
    #region Client Side
    public override void OnStartLocalPlayer()
    {
        string nick = PlayerPrefs.GetString("NICKNAME", "Player");
        CmdSetNickname(nick);
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
    #endregion
}