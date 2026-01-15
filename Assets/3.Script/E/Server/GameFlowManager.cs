using Mirror;
using UnityEngine;
public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance;
    [SyncVar] private bool gameStarted;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
    [Server]
    public void StartGame()
    {
        if (gameStarted)
            return;
        gameStarted = true;
        Debug.Log("[Server] Game Started");
        RpcStartGame();
    }
    [ClientRpc]
    private void RpcStartGame()
    {
        // 여기서 씬 전환 or 게임 시작 UI 처리
        Debug.Log("[Client] Game Start Signal Received");
    }
}