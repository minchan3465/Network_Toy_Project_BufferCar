using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour {
    // 서버가 정해주는 순번 (모든 클라이언트에게 동기화)
    public int playerIndex = -1;

    public MeshRenderer meshRenderer;
    public NetworkPlayer networkPlayer;

	private void Awake() {
        TryGetComponent(out networkPlayer);
    }

	private void Start() {
        playerIndex = networkPlayer.playerNumber - 1;
        setCarBodyColor(playerIndex);
        GameManager.Instance.RegisterPlayer(networkPlayer);
    }

	private void setCarBodyColor(int index) {
        Color color;
        switch(index) {
            case 0:
                color = Color.red;
                break;
            case 1:
                color = Color.green;
                break;
            case 2:
                color = Color.blue;
                break;
            case 3:
                color = Color.yellow;
                break;
            default:
                color = Color.white;
                break;
        }
        Debug.Log("자 너의 색 번호는 " + index);
        meshRenderer.materials[0].color = color;
    }

	//-----------추락
	private void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Deadzone")) {
            if (isLocalPlayer) {
                CmdRequestFell();
            }
		}
	}

    [Command]
    void CmdRequestFell() {
        GameManager.Instance.ProcessPlayerFell(playerIndex);
    }
}
