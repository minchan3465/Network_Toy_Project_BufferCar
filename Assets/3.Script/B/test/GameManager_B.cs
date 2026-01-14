using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager_B : NetworkBehaviour//수정중
{
    public static GameManager_B instance = null;

    //체력 감소 로직. server호출시 GameManager.instance 등 값 변경과 동시에 서버에 할당(씬전환시 유지)
    //게임 종료후 점수 Scene에서 GameManager.instance.StartGame(); 등으로 속도 체력초기화
    //GameManager가 변수를 직접 바꿀 수 없고 [SyncVar]사용하여 서버 연동
    //체력이 다 달았으면 순위 결정

    //GameManager가 가질 값
    //속도? 아이템값
    //체력
    //등수?
    //player number
    //제한시간

    //인게임이던 로비던 튕길때 로직 수정

    private void Awake()
    {
        if(instance == null) { instance = this; }
        else { Destroy(gameObject); }
        DontDestroyOnLoad(gameObject);
    }

    [SyncVar] public int playerNum = -1;
    [SyncVar] public int PlayerHP = 3;
    [SyncVar] public float Timeset = 99;
    [SerializeField] private GameObject DisconnectServerUI;//자식에 넣어두기 게임 종료전용?

    public void exit()//게임 종료용 버튼용 메서드
    {
        StartCoroutine(exitwait());
    }

    private IEnumerator exitwait()
    {
        //AudioManager.instance.PlaySFX("SFX1");
        yield return new WaitForSeconds(1f);
        if (NetworkManager.singleton != null)
        {

            if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient(); // 클라이언트 종료
            }
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
