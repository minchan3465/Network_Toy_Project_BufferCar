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
        //Debug.Log($"[Registry] OnEnable | active={gameObject.activeInHierarchy} | server={NetworkServer.active}");
        NetworkServer.OnDisconnectedEvent += OnClientDisconnected;
        //StartCoroutine(WaitForServer());
    }
    //private IEnumerator WaitForServer()
    //{
    //    while (!NetworkServer.active)
    //        yield return null;

    //    Debug.Log("[Registry] Server active, subscribe disconnect");
    //}
    private void OnDisable()
    {
        NetworkServer.OnDisconnectedEvent -= OnClientDisconnected;
    }
    private void OnClientDisconnected(NetworkConnectionToClient conn)
    {
        Debug.Log("is starttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttt");
        if (!NetworkServer.active)
            return;
        //if (conn.identity == null)
        //{
        //    Debug.Log("is starrrrrrrrrrrrrrrrrrrrrrrrrrr");
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

        // 이미 등록된 커넥션이면 무시 (중복 생성 방지 핵심)
        if (connToPlayer.ContainsKey(player.connectionToClient)) return;

        int assignedNumber;
        if (availableNumbers.Count > 0)
        {
            assignedNumber = availableNumbers.Min;
            availableNumbers.Remove(assignedNumber);
        }
        else
        {
            assignedNumber = nextPlayerNumber++;
        }

        players[assignedNumber] = player;
        connToPlayer[player.connectionToClient] = player;

        player.AssignPlayerNumber(assignedNumber);
        Debug.Log($"[Server] {player.PlayerNickname} 등록 완료. 슬롯: {assignedNumber}");
        //Debug.Log(DataManager.instance.playerInfo.PlayerNum+"Sucessssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssss");
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

        Debug.Log($"[Server] Player Left: {removeKey}");
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
        Debug.Log("[Server] 4명 레디 완료. 1초 후 게임 씬으로 이동합니다.");

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
}