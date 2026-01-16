using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameEnter : MonoBehaviour {
	public void GameEnter_Btn() {
		NetworkManager.singleton.StartClient();
	}
}
