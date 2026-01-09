using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGameInfo {
	public PlayerInfo info { get; private set; }
	public int hp;

	public PlayerGameInfo(PlayerInfo _info, int _hp = 3) {
		info = _info;
		hp = _hp;
	}
}

/*
 인게임 내, 정보를 관리할 게임 매니저입니다.
 사실 DataManager와 다름이 없긴 한데, 인게임에서 변하는건 여기서 관리할듯 합니다.
 HP 숫자가 생겨서, 서버에서와 인게임 상호작용할때 사용될듯합니다.
 
 
 */

public class GameManager : MonoBehaviour {
	/*
	 클라이언트가 가지고 있을 정보
	1. 닉네임
	2. 목숨
	3. 점수
	 */

	public PlayerGameInfo playerGameInfo;

	private void Start() {
		playerGameInfo = new PlayerGameInfo(DataManager.singletone.playerInfo, 3);
	}
}
