using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lobby_UI_Controller : MonoBehaviour {
	private bool isReady = false;

	public void	Lobby_Ready() {
		//Ready인지 아닌지, 누를때마다 바뀜.
		isReady = !isReady;

		if(isReady) {
			//서버에 준비되었다는 메시지
			//UI상으로 Ready표시
		} else {
			//서버에 준비 해제되었다는 메시지
			//UI상으로 Ready표시 해제
		}
	}

	public void Lobby_Quit() {
		//NetworkClient.StopClient();
		//네트워크 클라이언트 종료까지 안전하게 하기.
		//메인신으로 돌아가기
	}
}
