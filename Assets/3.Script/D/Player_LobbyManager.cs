using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct CarColor {
    public Color red;
}

public class Player_LobbyManager : NetworkBehaviour {
    [SyncVar(hook = "asd1")] public int PlayerNumber = 0;
    [SyncVar(hook = "asdf")] public bool isReady = false;

    private bool[] PlayersExist_list;
    private bool[] PlayersReady_list;
    CarColor car_color;

    private int max_player;

    // Update is called once per frame
    public override void OnStartClient() {
        base.OnStartClient();
        max_player = 4;
        PlayersExist_list = new bool[max_player];
        PlayersReady_list = new bool[max_player];
        otherPlayerState();
        PlayerNumSet();

    }

	private void otherPlayerState() {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player_Lobby");
        foreach(GameObject player in players) {
            if (player.Equals(isLocalPlayer)) continue;
            if (player.TryGetComponent(out Player_LobbyManager manager)) 
            {
                PlayersExist_list[manager.PlayerNumber - 1] = true;
                PlayersReady_list[manager.PlayerNumber - 1] = manager.isReady;
			}
		}
	}

    private void PlayerNumSet() {
        for(int i = 0; i<max_player; i++) {
            if (PlayersExist_list[i].Equals(true)) continue;
            PlayerNumber = i+1;
            break;
		}
        //DataManager.instance.playernum = PlayerNumber;
	}

    private void playerSetup() {
        switch(PlayerNumber) {
            case 1:
                //trasnform.postiion = new Vector();
                //material.color = new color();
                break;
            case 2:
                //trasnform.postiion = new Vector();
                //material.color = new color();
                break;
            case 3:
                //trasnform.postiion = new Vector();
                //material.color = new color();
                break;
            case 4:
                //trasnform.postiion = new Vector();
                //material.color = new color();
                break;
        }
	}
    













    //------수신받는 Client 세팅
    [Command]
    public void asdf(bool oldBool, bool newBool) {

	}
    [Command]
    public void asd1(int oldBool, int newBool) {

    }
}
