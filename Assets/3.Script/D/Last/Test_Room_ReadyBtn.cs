using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Test_Room_ReadyBtn : MonoBehaviour {
	public void bnt_action() {
		NetworkManager.singleton.ServerChangeScene("D_Main_InGame");
	}
}
