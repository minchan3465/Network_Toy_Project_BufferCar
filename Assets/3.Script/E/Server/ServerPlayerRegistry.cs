using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerRegistry : MonoBehaviour
{
    [SerializeField] private NetworkManager manager;
    public static ServerPlayerRegistry instance;
    private readonly Dictionary<NetworkConnectionToClient, NetworkPlayer> connToPlayer = new();
    private readonly Dictionary<int, NetworkPlayer> players = new();
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
    public void RegisterPlayer(NetworkPlayer player)
    {
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
        players.Add(assignedNumber, player);

        Debug.Log($"[Server] Player Registered: {assignedNumber}, Total={players.Count}");
    }

    public void UnregisterPlayer(NetworkPlayer player)
    {
        Debug.Log("½ÇÇàµÊ3");
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
    public IReadOnlyDictionary<int, NetworkPlayer> GetAllPlayers()
    {
        return players;
    }
}