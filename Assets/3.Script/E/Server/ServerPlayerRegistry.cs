using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerRegistry : MonoBehaviour
{
    [SerializeField] private NetworkManager manager;
    public static ServerPlayerRegistry instance;
    private readonly Dictionary<NetworkConnectionToClient, UserInfoManager> connToPlayer = new();
    private readonly Dictionary<int, UserInfoManager> players = new();
    private readonly SortedSet<int> availableNumbers = new();
    private int nextPlayerNumber = 1;
    public int PlayerCount => players.Count;
    private bool isStarting = false; // 중복 시작 방지 플래그

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);

            return;
        }
        //DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        ////Debug.Log($"[Registry] OnEnable | active={gameObject.activeInHierarchy} | server={NetworkServer.active}");
        NetworkServer.OnDisconnectedEvent += OnClientDisconnected;
        //StartCoroutine(WaitForServer());
    }
    //private IEnumerator WaitForServer()
    //{
    //    while (!NetworkServer.active)
    //        yield return null;

    //    //Debug.Log("[Registry] Server active, subscribe disconnect");
    //}
    private void OnDisable()
    {
        NetworkServer.OnDisconnectedEvent -= OnClientDisconnected;
    }
    private void OnClientDisconnected(NetworkConnectionToClient conn)
    {
        //Debug.Log("is starttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttt");
        if (!NetworkServer.active)
            return;
        //if (conn.identity == null)
        //{
        //    //Debug.Log("is starrrrrrrrrrrrrrrrrrrrrrrrrrr");
        //    return;
        //}

        //NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
        if (!connToPlayer.TryGetValue(conn, out var player))
            return;

        //if (player == null)
        //    return;

        UnregisterPlayer(player);
    }
    [Server]
    public void RegisterPlayer(UserInfoManager player)
    {
        if (player.connectionToClient == null) return;
        NetworkConnectionToClient conn = player.connectionToClient;

        // 1. 중복 객체 제거 (기존 로직 유지)
        if (connToPlayer.TryGetValue(conn, out var oldPlayer))
        {
            if (oldPlayer != null && oldPlayer != player)
            {
                int oldKey = -1;
                foreach (var kv in players) { if (kv.Value == oldPlayer) { oldKey = kv.Key; break; } }
                if (oldKey != -1) players.Remove(oldKey);
                NetworkServer.Destroy(oldPlayer.gameObject);
            }
        }

        // 2. [수정] Mirror HUD 인덱스가 할당될 때까지 기다렸다가 등록
        StartCoroutine(C_RegisterByHUDIndex(player, conn));
    }

    private IEnumerator C_RegisterByHUDIndex(UserInfoManager player, NetworkConnectionToClient conn)
    {
        var roomPlayer = player.GetComponent<NetworkRoomPlayer>();
        // Mirror 내부에서 index가 0, 1, 2, 3 중 하나로 할당될 때까지 대기
        yield return new WaitUntil(() => roomPlayer != null && roomPlayer.index != -1);

        int assignedNum = roomPlayer.index + 1; // Mirror HUD 번호를 그대로 사용

        players[assignedNum] = player;
        connToPlayer[conn] = player;
        player.AssignPlayerNumber(assignedNum);

        //Debug.Log($"[Server] HUD {roomPlayer.index}번에 맞춰 UI {assignedNum}번으로 동기화 완료.");
    }

    [Server]
    public void UnregisterPlayer(UserInfoManager player)
    {
        int removeKey = -1;
        foreach (var kv in players)
        {
            if (kv.Value == player)
            {
                removeKey = kv.Key;
                break;
            }
        }

        if (removeKey == -1) return;

        // 1. UI 갱신 명령: 모든 클라이언트에게 해당 슬롯(removeKey - 1)을 비우라고 전달
        // players에 남아있는 아무 플레이어나 사용하여 Rpc를 쏩니다.
        foreach (var p in players.Values)
        {
            if (p != null && p != player)
            {
                p.RpcClearLobbyUI(removeKey - 1);
            }
        }

        // 2. 데이터 삭제 및 번호 회수
        players.Remove(removeKey);
        if (player.connectionToClient != null)
            connToPlayer.Remove(player.connectionToClient);

        availableNumbers.Add(removeKey);

        // 기존 로직 유지
        if (GameManager.Instance != null)
            GameManager.Instance.SetDisconnectPlayerIndexInfo(removeKey - 1);

        //Debug.Log($"[Server] Player Left: {removeKey}");
    }
    [Server]
    public void TryStartGame()
    {
        if (isStarting || players.Count < 4) return; // 이미 시작 중이거나 인원 부족 시 리턴

        foreach (var p in players)
        {
            if (p.Value == null || !p.Value.IsReady) // 한 명이라도 준비 안 됐으면 리턴
                return;
        }

        // 모든 조건 만족 시
        isStarting = true;
        //Debug.Log("[Server] 4명 레디 완료. 1초 후 게임 씬으로 이동합니다.");

        // 지연 후 씬 전환 (코루틴 활용)
        StartCoroutine(C_DelayedStart());
    }
    [Server]
    public IReadOnlyDictionary<int, UserInfoManager> GetAllPlayers()
    {
        return players;
    }
    private IEnumerator C_DelayedStart()
    {
        // AWS 지연을 고려하여 모든 클라이언트에게 레디 상태가 동기화될 시간을 줍니다.
        yield return new WaitForSeconds(1.0f);

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartGame(); // 여기서 ServerChangeScene 호출
        }
    }
    [Server]
    public void PrepareForNewGame()
    {
        // 네트워크 연결을 유지한 채 로비로 돌아왔을 때 사용
        isStarting = false;
        //Debug.Log("[Server] Registry 상태 재설정. 새로운 게임 준비 완료.");
    }
}