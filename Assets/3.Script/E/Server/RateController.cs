using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.UI;

public class RateController : NetworkBehaviour
{
    [SyncVar(hook = "OnRateChange")]
    public int PlayerRate = 0;

    [SerializeField] private int Player_Rate;
    // Update is called once per frame
    public override void OnStartClient()
    {
        base.OnStartClient();
        int rate = DataManager.instance.playerInfo.User_Rate;
        cmdSendRateToServer(rate);
    }
    void Update()
    {
        //if (!isLocalPlayer) return;
        //if (Camera.main == null) return;
        //NickNameCard.transform.LookAt(Camera.main.transform);
    }
    public void set_Rate(int rate)
    {
        Player_Rate = rate;
    }
    //-------Server한테 닉네임 보고
    [Command]
    public void cmdSendRateToServer(int rate)
    {
        PlayerRate = rate;
    }

    public void OnRateChange(int oldrate, int newrate)
    {
        set_Rate(newrate);
    }
    public void getRate()
    {
        //if (!DataManager.instance.Login(Name_input.text, Pwd_input.text))
        //{
        //    LogText_viewing("해당 이름과 비밀번호가 사용하는 사용자가 없습니다.");
        //    LogText_viewing("THis Name is already uesed");
        //    return;
        //}
        //else
        //{
        //    if (DataManager.instance.GetRate(Name_input.text))
        //    {

        //    }
        //}
    }
}
