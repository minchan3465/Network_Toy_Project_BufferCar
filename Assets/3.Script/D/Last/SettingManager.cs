using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SettingManager : NetworkBehaviour {
	/*
	 얘가 해줄거.
	 1. 이 오브젝트가 활성화 된다면 (즉, Scene로딩이 다 된다면)
	 2. 차량 생성 (LocalPlayer로 작동될 차량) 단, 안보이는곳에
	 3. 인게임에서 작동할 데이터 세팅 (아마 로비에서 생성된 오브젝트에서 불러올듯.)
	 4. 데이터 가지고 추가적 세팅 (색, 위치 등)
	 5. 다 세팅되었으면, 게임 매니저에게 '나 준비 됐어요' 설정
	 6. 그리고 스스로 Disable (알수없는 오류를 막기 위해서.
	 */

	public GameObject playerCar;
	public Vector3 spawnPos;
	public PlayerData playerData;

	private MeshRenderer meshRenderer;

	private void OnEnable() {
		if (!isLocalPlayer) return;
		GameObject playerCar = Instantiate(this.playerCar, spawnPos, Quaternion.identity);
		if(playerCar.TryGetComponent(out playerData)) {
			GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerDDOL");
			foreach (GameObject player in players) {
				if (!isLocalPlayer) continue;
				//if (player.TryGetComponent(out UserInfoManager manager) {
				//	playerData.index = manager.PlayerNum;
				//	playerData.nickname = manager.PlayerNickName;
				//	playerData.rate = manager.PlayerRate;
				//}
				break;
			}

			if(playerCar.TryGetComponent(out meshRenderer)) {
				meshRenderer.materials[0].color = Setting_CarBodyColor(playerData.index);
			}
			//playerData.playerRespawn.위치 지정 메서드
			//세팅 끝.
			//난 실행될 준비 됐어요~~~~
			GameManager.Instance.ImReady(playerData);
		}
	}

	private Color Setting_CarBodyColor(int index) {
		Color color;
		switch (index) {
			case 0:
				color = Color.red;
				break;
			case 1:
				color = Color.green;
				break;
			case 2:
				color = Color.blue;
				break;
			case 3:
				color = Color.yellow;
				break;
			default:
				color = Color.white;
				break;
		}
		return color;
	}
}
