using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class RoomEnter : MonoBehaviour {
	public InputField input_ip;
	public InputField input_password;

	public void EnterRoom() {
		if (string.IsNullOrWhiteSpace(input_ip.text)) {
			Debug.Log("ip ÀÔ·Â¾ÈÇÔ.");
			//²¨Áö¼î
			return;
		}

		RoomManager Manager = RoomManager.singleton as RoomManager;
		if (int.TryParse(input_password.text, out int pw)) {
			if (Manager.Compare_Password(pw)) {
				Manager.StartClient();
			}
		}
	}
}
