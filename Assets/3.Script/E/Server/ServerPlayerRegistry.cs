using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerRegistry : MonoBehaviour
{
    public static ServerPlayerRegistry instance;
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
    [Server]
    public void RegisterPlayer(NetworkPlayer player)
    {
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
    [Server]
    public void UnregisterPlayer(NetworkPlayer player)
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
        if (removeKey != -1)
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