using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class InfoPacket {
    public int _index;
    public string _id;
    public string _name;
    public int _rate;
}

public class PlayerManager : NetworkBehaviour {
    // 서버가 정해주는 순번 (모든 클라이언트에게 동기화)
    public MeshRenderer meshRenderer;
    //public PlayerInfo playerInfo;
    public NetworkPlayer networkPlayer;

    public InfoPacket packet;
    /*
        User_ID = _id;
        User_Nic = _nic;
        User_Rate = _rate;
     
     */
    private void Awake() {
        TryGetComponent(out networkPlayer);
	}

	private void Start() {
        //playerIndex = int.Parse(playerInfo.User_ID) - 1;
        //playerNickname = playerInfo.User_Nic;
        //playerRating = playerInfo.User_Rate;
        //playerInfo = new PlayerInfo(playerID, playerNickname, playerRating);;
        //playerInfo = new PlayerInfo("id", "Car" + Random.Range(100, 1000), 2000);
        packet = new InfoPacket();
        packet._index = networkPlayer.playerNumber;
        packet._id = "id";
        packet._name = "Car" + Random.Range(100, 1000);
        packet._rate = 2000;

        setCarBodyColor(packet._index);
        GameManager.Instance.RegisterPlayer(packet);
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
		if (other.CompareTag("Deadzone")) {
            if (isLocalPlayer) {
                CmdRequestFell();
            }
		}
	}

    [Command]
    void CmdRequestFell() {
        GameManager.Instance.ProcessPlayerFell(packet._index);
    }
}
