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

        // 로비에 있는 UI 컨트롤러를 찾아서 카운트다운 연출을 실행시킵니다.
        Lobby_UI_Controller lobbyUI = FindAnyObjectByType<Lobby_UI_Controller>();
        if (lobbyUI != null)
        {
            lobbyUI.StartGameSequence(); // 카운트다운 3, 2, 1 시작
        }
    }
}