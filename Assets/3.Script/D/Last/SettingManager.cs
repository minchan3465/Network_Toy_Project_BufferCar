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

	public GameObject playerCarPrefab;
	public GameObject spawnPos;

	[Client]
	private void OnEnable() {
		//Debug.Log("SettingManager 활성화됨");
		StartCoroutine(WaitForLocalPlayerAndSetup());
	}

	private IEnumerator WaitForLocalPlayerAndSetup() {
		// 로컬 플레이어 오브젝트가 생성될 때까지 대기
		// 씬 전환 직후에는 내 캐릭터가 아직 스폰 안 됐을 수 있음
		while (NetworkClient.localPlayer == null) {
			yield return null;
		}

		int targetIndex = -1;
		string targetName = "Unknown";
		int targetRate = -1;

		if(NetworkClient.localPlayer.TryGetComponent(out UserInfoManager manager)) {
			while (string.IsNullOrEmpty(manager.PlayerNickname) || manager.PlayerRate == -1) {
				yield return null;
			}
			targetIndex = manager.PlayerNum - 1;
			targetName = manager.PlayerNickname;
			targetRate = manager.PlayerRate;
		}
		CmdRequestSpawnAndReady(targetIndex, targetName, targetRate);
		enabled = false;
	}
	[Command(requiresAuthority = false)]
	private void CmdRequestSpawnAndReady(int index, string nickname, int rate, NetworkConnectionToClient senderConnection = null) {
		GameObject car = Instantiate(playerCarPrefab, spawnPos.transform.position, Quaternion.identity);

		if (car.TryGetComponent(out PlayerData playerData)) {
			playerData.index = index;
			playerData.nickname = nickname;
			playerData.rate = rate;

			//네트워크 스폰, 다른 사람들한테도 연동시켜주기 위한거...
			NetworkServer.Spawn(car, senderConnection);
			//어쨌든, 해당 차량에 플레이어 아바타 권한 양도.

			//NetworkServer.ReplacePlayerForConnection(senderConnection, car, true);
			NetworkServer.AddPlayerForConnection(senderConnection, car);

			//플레이어 위치 설정
			//playerData.playerRespawn.InitializePlayer(playerData.index);
		}
	}
}
