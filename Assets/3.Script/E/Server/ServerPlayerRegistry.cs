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
        NetworkConnectionToClient conn = player.connectionToClient;
        // [수정] 이미 등록된 연결(커넥션)이라면 중복 등록하지 않고 리턴
        if (connToPlayer.ContainsKey(conn))
        {
            Debug.LogWarning($"[Server] Player already registered for connection: {conn}");
            return;
        }

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
        players.Add(assignedNumber, player);

        Debug.Log($"[Server] Player Registered: {assignedNumber}, Total={players.Count}");
        Debug.Log($"[Server] Player Registered: {assignedNumber}, Total={players.Count}");
        Debug.Log($"[Server] Player Registered: {assignedNumber}, Total={players.Count}");
        Debug.Log($"[Server] Player Registered: {assignedNumber}, Total={players.Count}");
        Debug.Log($"[Server] Player Registered: {assignedNumber}, Total={players.Count}");
        //DataManager.instance.playerInfo.PlayerNum = assignedNumber;
        //Debug.Log(DataManager.instance.playerInfo.PlayerNum+"Sucessssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssss");
    }

    public void UnregisterPlayer(UserInfoManager player)
    {
        //Debug.Log("실행됨3");
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
            if (!p.Value.readyToBegin)
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