using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour {
    // 서버가 정해주는 순번 (모든 클라이언트에게 동기화)
    [SyncVar(hook = nameof(OnIndexChanged))]
    public int playerNumber = -1;
	[SyncVar(hook = nameof(OnHpChanged))]
	public int playerHp = -1;

	public PlayerRespawn playerRespawn;
    public MeshRenderer meshRenderer;

	private void Awake() {
        TryGetComponent(out playerRespawn);
    }


	// 서버에서 이 객체가 생성(Spawn)될 때 실행됨
	// GameManager에게 이 플레이어를 등록해달라고 요청
    // 서버 접속했을 경우, 번호 정해주쇼~
	public override void OnStartServer() {
        base.OnStartServer();
        if (GameManager.Instance != null) {
            GameManager.Instance.RegisterPlayer(this);
        }
        //playerRespawn.isAlive = true;
    }
    //번호 바뀌면 생길 일들.
    private void OnIndexChanged(int oldIndex, int newIndex) {
		playerRespawn.playerNumber = playerNumber;
        meshRenderer.materials[0].color = setCarBodyColor(playerNumber);
    }
	private void OnHpChanged(int oldHp, int newHp) {
		Debug.Log("내 체력은 : " + playerHp);
		if (playerHp > 0) return;
		//playerRespawn.isAlive = false;
	}
	//번호 바뀐김에 색도 바꿉시다~
	private Color setCarBodyColor(int index) {
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
        return color;
	}

	//-----------추락
	private void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Deadzone")) {
            OnFall();
		}
	}

	public void OnFall() {
        if (isLocalPlayer) {
            CmdRequestFell();
        }
    }

    [Command]
    void CmdRequestFell() {
        GameManager.Instance.ProcessPlayerFell(playerNumber);
    }
}
