
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Mirror;
//using UnityEngine.SceneManagement;
public class LoginController : MonoBehaviour
{
    [SerializeField] private TMP_InputField name_input;
    [SerializeField] private TMP_InputField Pwd_input;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private Button Login_btn;
    [SerializeField] private Button Signup_btn;
    [SerializeField] private SignupController signupController;
    private void Start()
    {
        LogText_viewing(string.Empty);
        Login_btn.onClick.AddListener(LoginEvent);
        Signup_btn.onClick.AddListener(OpenSignupPage);
    }

    public void LogText_viewing(string text)
    {
        logText.text = text;
    }
    public void LoginEvent()
    {
        if (name_input.text.Equals(string.Empty) || Pwd_input.text.Equals(string.Empty))
        {
            LogText_viewing("이름 또는 비밀번호를 입력 해주세요");
            LogText_viewing("Name or PassWord check Please");
            return;
        }
        if (DataManager.instance.Login(name_input.text, Pwd_input.text))
        {
            GameObject manager = NetworkManager.singleton.gameObject;
            if (manager.TryGetComponent(out Serverchecker checker))
            {
                checker.Start_Client();
            }
            //Debug.Log("check1"+ DataManager.instance.playerInfo.User_ID);
            //Debug.Log("check2"+ DataManager.instance.playerInfo.User_Nic);
            //Debug.Log("check3"+ DataManager.instance.playerInfo.User_Rate);
            //gameObject.SetActive(false);
            //SceneManager.LoadScene("Main_InGame!");

        }
        else
        {
            LogText_viewing("이름 또는 비밀번호를 확인 해주세요");
            LogText_viewing("Name or PassWord check Please");
        }
    }
    public void OpenSignupPage()
    {

        signupController.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }
}
