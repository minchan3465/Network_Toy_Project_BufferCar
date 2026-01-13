using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Lobby_UI_Controller : MonoBehaviour {

	//테스트용 컴포넌트
	public LobbyManager _LobbyManager;

	private bool isReady = false;
	public Text Ready_btn_text;

	public void	Lobby_Ready() {
		//Ready인지 아닌지, 누를때마다 바뀜.
		isReady = !isReady;

		if(isReady) {
			//서버에 준비되었다는 메시지
			//UI상으로 Ready표시
			Ready_btn_text.text = "준비해제";
			_LobbyManager.Lobby_Start();
		} else {
			//서버에 준비 해제되었다는 메시지
			//UI상으로 Ready표시 해제
			Ready_btn_text.text = "준비하기";
			_LobbyManager.Lobby_Stop();
		}
	}

	public void Lobby_Quit() {
		//NetworkClient.StopClient();
		//네트워크 클라이언트 종료까지 안전하게 하기.
		//메인신으로 돌아가기
		SceneManager.LoadScene("D_Main");
	}


}
