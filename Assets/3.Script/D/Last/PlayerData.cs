using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerData : NetworkBehaviour {
	[SyncVar(hook = nameof(UpdatePlayerIndex))] public int index = -1;
	[SyncVar] public string nickname = string.Empty;
	[SyncVar] public int rate = -1;

	public PlayerController playerController;
	public PlayerRespawn playerRespawn;
	public GameObject carMeshRenderer;

	private void Awake() {
		TryGetComponent(out playerController);
		TryGetComponent(out playerRespawn);
	}

	public override void OnStartAuthority() {
		base.OnStartAuthority();
		GameManager.Instance.car = gameObject;
		playerRespawn.InitializePlayer(index);
		playerController.IsStunned = true;
		CmdImReady();
	}
	public override void OnStopServer() {
		base.OnStopServer();
		CmdImOut(index);
	}

	[Command]
	public void CmdImReady() {	GameManager.Instance.ImReady(this); }
	[Command]
	public void CmdImOut(int index) { GameManager.Instance.SetDisconnectPlayerIndexInfo(index); }

	public void PlayerStunChange(bool _bool) {	playerController.IsStunned = _bool; }
	public void UpdatePlayerIndex(int oldIndex, int newIndex) { SetCarBodyColor(Setting_CarBodyColor(index)); }
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
