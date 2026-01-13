
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class NicnameController : NetworkBehaviour
{
    [SyncVar(hook = "OnNameChange")]
    public string PlayerNickname = "Empty";

    [SerializeField] private TMP_Text nickname_Card;
    [SerializeField] private GameObject NickNameCard;
    // Update is called once per frame
    public override void OnStartClient()
    {
        base.OnStartClient();
        string nickname = DataManager.instance.playerInfo.User_Nic;
        cmdSendNameToServer(nickname);
    }
    void Update()
    {
        //if (!isLocalPlayer) return;
        if (Camera.main == null) return;
        NickNameCard.transform.LookAt(Camera.main.transform);
    }
    public void set_nickName(string name)
    {
        nickname_Card.text = name;
    }
    //-------Server한테 닉네임 보고
    [Command]
    public void cmdSendNameToServer(string name)
    {
        PlayerNickname = name;
    }

    public void OnNameChange(string oldname, string newname)
    {
        set_nickName(newname);
    }
}
