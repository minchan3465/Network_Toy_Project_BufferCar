using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player_LobbySetup : NetworkBehaviour {
	//플레이어 번호에 따른 차량 색 변경
	private int player_number;

	public override void OnStartLocalPlayer() {
		string id = "Player" + Random.Range(100,1000);	

		Cmd_RegisterID(id);
	}

	[Command]
	private void  Cmd_RegisterID(string id) {
		if(NetworkManager.singleton is Server_NetworkManager serverMgr) {
			player_number = serverMgr.AddPlayerID(id);
		}
	}
}
