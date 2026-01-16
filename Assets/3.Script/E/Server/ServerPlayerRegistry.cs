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
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void OnEnable()
    {
        //Debug.Log($"[Registry] OnEnable | active={gameObject.activeInHierarchy} | server={NetworkServer.active}");
        StartCoroutine(WaitForServer());
    }
    private IEnumerator WaitForServer()
    {
        while (!NetworkServer.active)
            yield return null;

        Debug.Log("[Registry] Server active, subscribe disconnect");
        NetworkServer.OnDisconnectedEvent += OnClientDisconnected;
    }
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
        Debug.Log($"[Registry] 등록 시도: {player.PlayerNickname}"); // 이 로그가 찍히는지 확인

        NetworkConnectionToClient conn = player.connectionToClient;
        connToPlayer[conn] = player;
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
        player.AssignPlayerNumber(assignedNumber);
        Debug.Log($"[Registry] 번호 배정 완료: {assignedNumber}"); // 이 로그가 찍혀야 합니다.
        players.Add(assignedNumber, player);

        Debug.Log($"[Server] Player Registered: {assignedNumber}, Total={players.Count}");
    }

    [Server]
    public void UnregisterPlayer(UserInfoManager player)
    {
        Debug.Log("실행됨3");
        int removeKey = -1;
        foreach (var kv in players)
        {
            if (kv.Value == player)
            {
                removeKey = kv.Key;
                break;
            }
        }
        Debug.Log(removeKey);
        if (removeKey == -1)
        {
            return;
        }
        players.Remove(removeKey);

        // 나간 사람의 연결 정보도 딕셔너리에서 제거
        if (player.connectionToClient != null)
            connToPlayer.Remove(player.connectionToClient);

        availableNumbers.Add(removeKey);
        Debug.Log($"[Server] Player Left: {removeKey}");
    }
    [Server]
    public void TryStartGame()
    {
        if (players.Count == 0)
            return;
        foreach (var p in players)
        {
            if (!p.Value.isReady)
                return;
        }
        Debug.Log("[Server] All players ready. Starting game.");
        GameFlowManager.Instance.StartGame();
    }
    [Server]
    public IReadOnlyDictionary<int, UserInfoManager> GetAllPlayers()
    {
        return players;
    }

}