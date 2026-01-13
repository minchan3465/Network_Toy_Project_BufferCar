using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

public class LobbyManager : MonoBehaviour {
	//[SyncVar] public string Lobby_Start_Text_text = string.Empty;
	//[SyncVar] public string Lobby_Start_Timer_text = string.Empty;




	public Text Lobby_Start_Text;
	public Text Lobby_Start_Timer;

	[Server]
	public void changeText(Text text_field, string text) {
		text_field.tag = text;
	}

	//public void Lobby_Start() {
	//	Lobby_Start_Text_text.text = "곧 게임이 시작됩니다.";
	//	StartCoroutine("Lobby_Start_Timer");
	//}

	//public void Lobby_Stop() {
	//	Lobby_Start_Text_text.text = string.Empty;
	//	Lobby_Start_Timer_text.text = string.Empty;
	//	StopCoroutine("Lobby_Start_Timer");
	//}

	//private IEnumerator Lobby_Start_Timer() {
	//	WaitForSeconds wfs = new WaitForSeconds(1f);
	//	int Lobby_Countdown = 3;
	//	while(Lobby_Countdown >= 0) {
	//		Lobby_Start_Timer_text.text = Lobby_Countdown.ToString();
	//		yield return wfs;
	//		Lobby_Countdown += -1;
	//	}
	//	SceneManager.LoadScene("D_Test_Game");
	//}
}
