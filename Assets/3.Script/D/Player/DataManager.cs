using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo {
	public string id { get; private set; }
	public string name { get; private set; }
	public int rating { get; private set; }

	public PlayerInfo(string _id, string _name, int _rating) {
		id = _id;
		name = _name;
		rating = _rating;
	}
}

/* 
 플레이어의 데이터를 관리하는 매니저입니다.
 기본적으로 아이디, 닉네임, 랭킹 점수를 가지고 있습니다.
 
 해당 정보들은 로그인 후, DB로부터 받아올겁니다. (SetPlayerInfo 메서드를 통하여) 

 활용될 곳은, 
- Room에 들어갔을때 정보 표시
- 인게임 UI 
- 게임 종료 후, 데이터 변경


윗 기능중, DB와 상호작용하는건 아마 여기서 다 처리할거같습니다.
(데이터 변경 시, DB에 업로드) 이런거
 */


public class DataManager : MonoBehaviour {
	public PlayerInfo playerInfo;

	public static DataManager singletone;

	public DataManager() {
		if (singletone != null) {
			singletone = this;
		} else {
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
	}

	public void SetPlayerInfo(string _id, string _name, int _rating) {
		playerInfo = new PlayerInfo(_id, _name, _rating);
	}

	private void Start() {
		SetPlayerInfo("testid", "테스트", 2000);
	}
}
