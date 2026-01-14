
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ReadyController : NetworkBehaviour
{
    [SyncVar(hook = "OnReadyChange")]
    public bool PlayerReady = false;

    [SerializeField] private bool Player_ready;
    [SerializeField] private bool[] otherPlayerNum = new bool[4];
    //[SerializeField] private GameObject NickNameCard;
    // Update is called once per frame
    public override void OnStartClient()
    {
        base.OnStartClient();
        Player_ready = PlayerReady;
        //string nickname = DataManager.instance.playerInfo.User_Nic;
        //bool nickname = DataManager.instance.playerInfo.User_Nic;
        cmdSendReadyToServer(Player_ready);
    }
    void Update()
    {
        //if (!isLocalPlayer) return;
        //if (Camera.main == null) return;
        //NickNameCard.transform.LookAt(Camera.main.transform);
    }
    public void set_Ready(bool ready)
    {
        Player_ready = ready;
    }
    //-------Server한테 닉네임 보고
    [Command]
    public void cmdSendReadyToServer(bool ready)
    {
        PlayerReady = ready;
    }

    public void OnReadyChange(bool oldready, bool newready)
    {
        set_Ready(newready);
        throwPlayerReady();
    }
    [ClientRpc]
    public void throwPlayerReady()
    {
        int myNum = DataManager.instance.playerInfo.PlayerNum;
        for (int i = 0; i < otherPlayerNum.Length; i++)
        {
            if ((i + 1).Equals(myNum))
            {
                otherPlayerNum[i] = true;
            }
        }
    }
    public void myPlayerReadySet()
    {
        otherPlayerNum[DataManager.instance.playerInfo.PlayerNum - 1] = true;
    }
}
