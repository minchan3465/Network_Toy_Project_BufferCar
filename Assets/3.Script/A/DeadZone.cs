using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Something hit DeadZone: {other.name}");

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player Tag Detected!");

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

            PlayerRespawn respawnController = other.GetComponent<PlayerRespawn>();

            if (respawnController != null)
            {
                if (respawnController.isLocalPlayer)
                {
                    Debug.Log("My car fell! Requesting Respawn to Server...");
                    respawnController.CmdRequestRespawn();
                }
            }
            else
            {
                Debug.LogWarning("PlayerRespawn component NOT found on this object or its parents!");
            }
        }
    }
}
