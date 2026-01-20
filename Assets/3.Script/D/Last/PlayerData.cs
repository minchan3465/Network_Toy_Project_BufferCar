using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerData : NetworkBehaviour {
	[SyncVar(hook = nameof(SetPlayerDataIndex))] public int index = -1;
	[SyncVar(hook = nameof(SetPlayerDataID))] public string id = string.Empty;
	[SyncVar(hook = nameof(SetPlayerDataNickname))] public string nickname = string.Empty;
	[SyncVar(hook = nameof(SetPlayerDataRate))] public int rate = -1;

	public PlayerController playerController;
	public PlayerRespawn playerRespawn;
	public GameObject carMeshRenderer;

	private void Awake() {
		TryGetComponent(out playerController);
		TryGetComponent(out playerRespawn);
	}

	private void Start() {
		if (!isLocalPlayer) return;
		int index = DataManager.instance.playerInfo.PlayerNum - 1;
		string id = DataManager.instance.playerInfo.User_ID;
		string nickname = DataManager.instance.playerInfo.User_Nic;
		int rate = DataManager.instance.playerInfo.User_Rate;

		playerRespawn.InitializePlayer(index);
		playerController.IsStunned = true;
		GameManager.Instance.car = gameObject;

		CmdSetPlayerData(index, id, nickname, rate);
		CmdImReady();
	}

	public void SetPlayerDataIndex(int oldIndex, int newIndex) { SetCarBodyColor(Setting_CarBodyColor(newIndex)); }
	public void SetPlayerDataID(string oldid, string newid) { }
	public void SetPlayerDataNickname(string oldName, string newName) { }
	public void SetPlayerDataRate(int oldRate, int newRate) { }

	[Command]
	private void CmdSetPlayerData(int index, string id, string nickname, int rate) {
		this.index = index;
		this.id = id;
		this.nickname = nickname;
		this.rate = rate;
	}


	[Command]
	public void CmdImReady() { GameManager.Instance.ImReady(this); }
	[Command]
	public void CmdImOut(int index) { GameManager.Instance.SetDisconnectPlayerIndexInfo(index); }


	public void PlayerStunChange(bool _bool) {	playerController.IsStunned = _bool; }

	private void SetCarBodyColor(Color color) {
		if(carMeshRenderer.TryGetComponent(out MeshRenderer meshRenderer)) {
			meshRenderer.materials[0].color = color;
		}
	}
	private Color Setting_CarBodyColor(int index) {
		switch (index) {
			case 0: return Color.red;
			case 1: return Color.green;
			case 2: return Color.blue;
			case 3: return Color.yellow;
			default: return Color.white;
		}
	}

	private void OnTriggerEnter(Collider other) {
		if (!isOwned) return;
		if (other.CompareTag("Deadzone")) {
			Onfall();
		}
	}
	[Command]
	private void Onfall() {
		GameManager.Instance.ProcessPlayerFell(index, connectionToClient);
	}
}
