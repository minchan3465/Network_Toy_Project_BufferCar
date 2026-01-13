using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class PlayerNumController : NetworkBehaviour
{
    [SyncVar(hook = "OnPlayerNumChange")]
    public int PlayerNum = 0;

    [SerializeField] private int Player_Num;
    [SerializeField] private int[] otherPlayerNum = new int[4];
    // Update is called once per frame
    public override void OnStartClient()
    {
        base.OnStartClient();
        findOtherPlayer();
        myPlayerNumSet();
        throwPlayerNum();
        int playerNum = DataManager.instance.playerInfo.PlayerNum;
        cmdSendNameToServer(playerNum);
    }
    void Update()
    {
    }
    public void set_PlayerNum(int num)
    {
        Player_Num = num;
    }
    //-------Server한테 닉네임 보고
    [Command]
    public void cmdSendNameToServer(int num)
    {
        PlayerNum = num;
    }

    public void OnPlayerNumChange(int oldnum, int newnum)
    {
        set_PlayerNum(newnum);
    }
    [ClientRpc]
    public void throwPlayerNum()
    {
        int myNum = DataManager.instance.playerInfo.PlayerNum;
        for (int i = 0; i < otherPlayerNum.Length; i++)
        {
            if ((i + 1).Equals(myNum))
            {
                otherPlayerNum[i] = 1;
            }
        }
    }
    public void findOtherPlayer()
    {
        GameObject[] playerObject;
        playerObject = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject pO in playerObject)
        {
            if (TryGetComponent(out PlayerNumController Num))
            {
                otherPlayerNum[Num.PlayerNum -1] = 1;
            }
        }
    }
    public void myPlayerNumSet()
    {
        for (int i = 0; i < otherPlayerNum.Length; i++)
        {
            if (otherPlayerNum[i + 1].Equals(0))
            {
                otherPlayerNum[i+1] = 1;
                DataManager.instance.playerInfo.PlayerNum = i + 1;
                return;
            }
        }
    }
}