using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    // [핵심] 게임이 끝나고 로비로 돌아가는 로직
    [Server]
    public void BackToRoom()
    {
        // 1. 다음 판을 위해 레지스트리 상태 재설정 (중복 방지)
        if (ServerPlayerRegistry.instance != null)
        {
            ServerPlayerRegistry.instance.PrepareForNewGame();
        }

        // 2. [가장 중요] SceneManager.LoadScene이 아니라 이 함수를 써야 함!
        // 그래야 모든 클라이언트가 '접속을 유지한 채' 다 같이 로비로 이동합니다.
        NetworkManager.singleton.ServerChangeScene("Main_Room");
    }
}