
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SignupController : MonoBehaviour
{
    [SerializeField] private LoginController LoginController;
    [SerializeField] private GameObject Signup_ob;
    [SerializeField] private TMP_InputField Name_input;
    [SerializeField] private TMP_InputField Nic_input;
    [SerializeField] private TMP_InputField Pwd_input;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private Button Signup_btn;
    private void Start()
    {
        //TryGetComponent(out LoginController);
        LogText_viewing(string.Empty);
        Signup_btn.onClick.AddListener(SignupEvent);
    }

    private void LogText_viewing(string text)
    {
        logText.text = text;
    }
    public void SignupEvent()
    {
        if (Name_input.text.Equals(string.Empty) || Pwd_input.text.Equals(string.Empty))
        {
            LogText_viewing("이름 또는 비밀번호를 입력 해주세요");
            LogText_viewing("Name or PassWord check Please");
            return;
        }

        if (DataManager.instance.SignupCheck(Name_input.text))
        {
            LogText_viewing("해당 이름과 비밀번호가 사용하는 사용자가 있습니다.");
            LogText_viewing("THis Name is already uesed");
            return;
        }
        if (DataManager.instance.Signup(Name_input.text, Nic_input.text, Pwd_input.text))
        {
            LoginController.LogText_viewing("생성되었습니다.");
            LoginController.LogText_viewing("Success Sign UP.");
            //GameObject manager = NetworkManager.singleton.gameObject;
            //if (manager.TryGetComponent(out Serverchecker checker))
            //{
            //    checker.Start_Client();
            //}
            //gameObject.SetActive(false);
            Name_input.text = string.Empty;
            Pwd_input.text = string.Empty;
            Signup_ob.SetActive(false);
        }
        else
        {
            LogText_viewing("이름 또는 비밀번호를 확인 해주세요");
            LogText_viewing("Name or PassWord check Please");
        }
    }
}
