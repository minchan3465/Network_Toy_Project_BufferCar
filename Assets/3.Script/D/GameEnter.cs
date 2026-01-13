using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameEnter : MonoBehaviour {
	public static string TempID;

	public void GameEnter_Btn() {
		TempID = "Player" + Random.Range(100, 1000);
		Debug.Log($"생성된 ID: {TempID} 로 접속을 시도합니다.");

		NetworkManager.singleton.StartClient();
		//Debug.Log("서버 연결 시도 중...");
	}
}
