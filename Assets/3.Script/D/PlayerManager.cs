using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour {
    public int _index;
    public string _id;
    public string _name;
    public int _rate;

    // 서버가 정해주는 순번 (모든 클라이언트에게 동기화)
    public MeshRenderer meshRenderer;
    //public PlayerInfo playerInfo;
    //public NetworkPlayer networkPlayer;
    public PlayerController playerController;
    public PlayerRespawn playerRespawn;

    private void Awake() {
        //TryGetComponent(out networkPlayer);
        TryGetComponent(out playerController);
        TryGetComponent(out playerRespawn);
	}

	private void Start() {
        //playerIndex = int.Parse(playerInfo.User_ID) - 1;
        //playerNickname = playerInfo.User_Nic;
        //playerRating = playerInfo.User_Rate;
        //playerInfo = new PlayerInfo(playerID, playerNickname, playerRating);;
        //playerInfo = new PlayerInfo("id", "Car" + Random.Range(100, 1000), 2000);
        //packet._index = networkPlayer.playerNumber -1;
        _index = 0;
        _id = "id";
        _name = "Car" + Random.Range(100, 1000);
        _rate = 2000;
    }

    public override void OnStartServer() {
        base.OnStartServer();
        //GameManager.Instance.RegisterPlayer(this);
        setCarBodyColor(_index);
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
        meshRenderer.materials[0].color = color;
    }

	//-----------추락
	private void OnTriggerEnter(Collider other) {
        if (!isLocalPlayer) return;
		if (other.CompareTag("Deadzone")) {
            CmdRequestFell();
		}
	}

    [Command]
    void CmdRequestFell() {
        GameManager.Instance.ProcessPlayerFell(_index);
    }
}
