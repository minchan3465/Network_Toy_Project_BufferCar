using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class RoomCreate : MonoBehaviour {
	public InputField input_password;

	public void CreateRoom() {
		if (string.IsNullOrWhiteSpace(input_password.text)) {
			Debug.Log("비밀번호 입력 안함.");
			//꺼지쇼
			return;
		}
		//RoomManager로 형변환 (어짜피 RoomManager는 NetworkManager를 상속받는 NetworkRoomManager를 상속받기 때문.)
		RoomManager manager = RoomManager.singleton as RoomManager;
		//비밀번호 저장
		if (int.TryParse(input_password.text, out int pw)) {
			manager.Set_Password(pw);
			manager.StartHost();
		} else {
			Debug.Log("비밀번호는 숫자만 입력 가능합니다.");
		}
	}
}
