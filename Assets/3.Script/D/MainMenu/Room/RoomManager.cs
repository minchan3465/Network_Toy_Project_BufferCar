using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoomManager : NetworkRoomManager {
	public int password;

	public void Set_Password(int _password) {
		password = _password;
	}

	public bool Compare_Password(int _password) {
		if(!password.Equals(_password)) {
			Debug.Log("비밀번호 틀림.");
			return false;
		}
		return true;
	}
}
