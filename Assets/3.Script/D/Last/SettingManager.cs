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
	public GameObject spawnPos;

	private MeshRenderer meshRenderer;

	public override void OnStartLocalPlayer() {
		Debug.Log("실행됨1");
		Debug.Log("실행됨2");
		GameObject playerCar = Instantiate(this.playerCar, spawnPos.transform.position, Quaternion.identity);
		Debug.Log("실행됨3");
		if (playerCar.TryGetComponent(out PlayerData playerData)) {
			Debug.Log("실행됨4");
			GameObject[] players = GameObject.FindGameObjectsWithTag("Player_Lobby");
			Debug.Log("실행됨5");
			foreach (GameObject player in players) {
				Debug.Log("실행됨 반복문on");
				if (player.TryGetComponent(out UserInfoManager manager)) {
					if (manager.isLocalPlayer) continue;
					Debug.Log("내가 받을 데이터 찾았쇼~");
					playerData.index = manager.PlayerNum;
					playerData.nickname = manager.PlayerNickname;
					playerData.rate = manager.PlayerRate;
				}
				break;
			}

			if(playerCar.TryGetComponent(out meshRenderer)) {
				meshRenderer.materials[0].color = Setting_CarBodyColor(playerData.index);
			}
			playerData.playerRespawn.InitializePlayer(playerData.index);
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
