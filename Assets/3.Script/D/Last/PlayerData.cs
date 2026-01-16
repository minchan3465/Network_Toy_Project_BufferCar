using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerData : NetworkBehaviour {
	public int index = -1;
	public string nickname = string.Empty;
	public int rate = -1;

	public PlayerController playerController;
	public PlayerRespawn playerRespawn;

	private void Awake() {
		TryGetComponent(out playerController);
		TryGetComponent(out playerRespawn);
	}
}
