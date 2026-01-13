using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class serverStarter : MonoBehaviour {
	private NetworkManager manager;

	private void Start() {
		manager = NetworkManager.singleton;
		manager.StartServer();
		Debug.Log("서버 열림");
	}
}
