using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoomManager : NetworkRoomManager {
	public int password;
	public int tempPassword;

	public void Set_Password(int _password) {
		password = _password;
	}

	public bool Compare_Password(int _password) {
		if(!password.Equals(_password)) {
			return false;
		}
		return true;
	}

	// 4번 확인용: 호스트가 정상적으로 시작되었는지 확인
	public override void OnStartHost() {
		base.OnStartHost();
		Debug.Log("<color=blue>호스트가 성공적으로 시작되었습니다.</color>");
	}

	// 3번 확인용: 플레이어가 방에 들어왔을 때 실행
	public override void OnRoomServerConnect(NetworkConnectionToClient conn) {
		base.OnRoomServerConnect(conn);
		Debug.Log($"새로운 플레이어 접속 시도: {conn.address}");
	}
}
