using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Server_NetworkManager : NetworkManager {
	public string[] players_id;

	public override void Awake() {
		base.Awake();
		maxConnections = 4;
		players_id = new string[maxConnections];
	}

	public override void OnServerAddPlayer(NetworkConnectionToClient conn) {
		base.OnServerAddPlayer(conn);
		//Debug.Log($"플레이어 접속 / Connection ID : {conn.connectionId}");
	}

	public int AddPlayerID(string id) {
		for(int i = 0; i< maxConnections; i++) {
			if(players_id[i] == null) {
				players_id[i] = id;
				Debug.Log("[Lobby] " + i + "P 유저 입장");
				return i+1;
			}
		}
		Debug.Log("플레이어 정원 초과.");
		return -1;
	}

	public void DelPlayerID(string id) {
		for (int i = 0; i < maxConnections; i++) {
			if (players_id[i].Equals(id)) {
				players_id[i] = null;
				Debug.Log("[Lobby] " + i + "P 유저 퇴장");
				return;
			}
		}
		Debug.Log("플레이어 정보 없음");
	}
}
