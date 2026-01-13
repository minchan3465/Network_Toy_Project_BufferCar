using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class RoomEnter : MonoBehaviour {
	public InputField input_ip;
	public InputField input_password;

	public void EnterRoom() {
		if (string.IsNullOrWhiteSpace(input_ip.text)) {
			Debug.Log("ip 입력안함.");
			//꺼지쇼
			return;
		}

		RoomManager manager = RoomManager.singleton as RoomManager;
		if(NetworkClient.active) {
			manager.StopClient();
		}

		if (manager != null) {
			// 1. 서버 주소 설정
			manager.networkAddress = input_ip.text;

			// 2. 입력한 비밀번호를 '잠시' 보관 (접속 성공 후 서버에 보내기 위해)
			if (int.TryParse(input_password.text, out int pw)) {
				//내가 입력한 비밀번호를 저장하고
				manager.tempPassword = pw;
				//접속 시도.
				//접속 시도하면서, RoomPasswordChecker가 내부 Authenticator로 서버와 수신하여, 비밀번호 테스트를 할거임.
				//접속 성공 유무에 따라 메시지 결과창 뜨는건 그 인증기에서 알아서 하라 하고.
				manager.StartClient();
			} else {
				Debug.Log("비밀번호는 숫자만 입력 가능합니다.");
			}
		}
	}
}
