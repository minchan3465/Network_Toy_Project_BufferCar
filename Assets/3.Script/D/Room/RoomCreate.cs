using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoomCreate : MonoBehaviour {




	public void CreateRoom() {
		NetworkManager Manager = RoomManager.singleton;
		//방 설정 처리
		//
		//
		Manager.StartHost();
	}
}
