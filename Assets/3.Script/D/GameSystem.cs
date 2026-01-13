using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public enum FlowState {
	Wait,
	SetUp,
	Start,
	Playing,
	End,
	Result
}

public class GameSystem : NetworkBehaviour {
	public readonly SyncList<int> playersHp = new SyncList<int>();

	public GameObject[] playerHpUIs;

	public void Setup_PlayersHp() {
		// 서버가 시작될 때 4명의 체력을 3으로 초기화
		for (int i = 0; i < 4; i++) playersHp.Add(3);
	}












	//---------------Player Data
	public GameObject[] players_object;
	private int player_count = 0;
	private int player_number = 0;


	//순위용
	public Stack<GameObject> ranks;
	public GameObject[] ranks_result;
	public int max_player = 4;



	[SyncVar]
	public FlowState state;

	//시간
	[SyncVar] 
	public int timer = 99;
	public TextMeshPro Timer;
	WaitForSeconds wfs = new WaitForSeconds(1f);

	//게임 플레이중 인지
	[SyncVar] 
	public bool isGameStarted = false;

	[Header("UI 오브젝트")]
	public TextMeshPro MiddleTitle;
	[SyncVar]
	public string MiddleTitle_text;

	//-----------------------------------------------------
	//켰을때 세팅
	private void Start() {
		if(isLocalPlayer) {
			Enter_Player(gameObject);
		}
		else if (isServer) {
			players_object = new GameObject[max_player];
			//players_hp = new int[max_player];
	;
			ranks = new Stack<GameObject>();
			ranks_result = new GameObject[max_player];

			Game_Start_Server();
		}
	}

	//--------- Client To Server
	[Command]
	public void Enter_Player(GameObject player) {
		players_object[player_count] = player;
		playersHp[player_count] = 3;
		Set_PlayerNumber(connectionToClient, player_count);
		player_count += 1;
	}

	[TargetRpc]
	void Set_PlayerNumber(NetworkConnection target, int number) {
		player_number = number;
	}

	[Command]
	public void Game_Over_Player(GameObject player) {
		ranks.Push(player);
		Debug.Log($"{5 - ranks.Count}등 : {player.transform.name} 플레이어");
	}

	//---------------------------------------------------------------------------

	//만약 시작 버튼을 눌렀을 때, 흐름 시작
	[Server]
	public void Game_Start_Server() {
		Game_Flow();
	}

	//플레이어 리스트에 추가
	//게임 중이 아니라면, return
	//게임 중이라면 아래 코드

	//등수가 2위까지 정해진다면,마지막 한명의 등수까지 넣어줍니다 (1등).
	//타이머 돌아가는 코루틴 종료
	//게임 종료 flow로.
	private void Update() {
		if (!isGameStarted) return;
		if (ranks.Count.Equals(3)) {
			Debug.Log("끝난 이유 : Last Man Standing");
			//Game_RankSort();								
			StopCoroutine("Game_Playing_Timer");			
			Game_Flow();                       
		}
	}


	// 게임 흐름 제어 플로우 상태머신?
	public void Game_Flow() {
		switch (state) {
			case FlowState.SetUp:
				Debug.Log("현재 플로우 상태 : SetUp");
				Game_SetUp();
				break;
			case FlowState.Start:
				Debug.Log("현재 플로우 상태 : Start");
				Game_Start();
				break;
			case FlowState.Playing:
				Debug.Log("현재 플로우 상태 : Playing");
				Game_Playing();
				break;
			case FlowState.End:
				Debug.Log("현재 플로우 상태 : End");
				Game_End();
				break;
			case FlowState.Result:
				Debug.Log("현재 플로우 상태 : Result");
				Game_Result();
				break;
		}
	}

	//-------------실질적으로 게임 흐름 동작? 할 코드들 

	private void Game_SetUp() {
		//타이머 99초 설정
		timer = 99;
		Timer.text = timer.ToString();

		//각 플레이어의 시작 위치 조정
		for (int i = 0; i<max_player; i++) {
			//players_object[i].transform.position = player_spawn_position[i];
		}

		Game_Flow();
	}

	//--------------Game_Start

	private void Game_Start() {
		//UI적으로 3,2,1 하고
		//모든 플레이어의 움직임을 제한하는걸 풀어야함.

		StartCoroutine("Game_Start_Timer");
		//다음 flow는 'Game_Flow(FlowState.Playing)'
	}

	private IEnumerator Game_Start_Timer() {
		//보니까 Ready 2번 깜빡이고, Go! 하면서 바로 움직일 수 있게 되어있음!
		int start_countdown = 3;
		while(start_countdown >= 0) {
			if(start_countdown > 0) {
				MiddleTitle_text = start_countdown.ToString();
				//UI Ready FadeOut...
			} else {
				MiddleTitle_text = "GO!";
				//UI Go! FadeOut, ZoomIn
			}
			yield return wfs;
			start_countdown += -1;
		}
		MiddleTitle_text = string.Empty;
		Game_Flow();
	}

	//----------------------------------------------------------------------
	//Game_Playing

	private void Game_Playing() {
		//게임 시작
		isGameStarted = true;
		//코루틴으로 99초 이제 하나씩 감소할거 같은데,
		//15초마다 아이템 생성
		//60초지나면 그라운드 축소
		StartCoroutine("Game_Playing_Timer");
		
		//조건 채워치고, 다음 flow는 'Game_Flow(FlowState.End)'
	}
	private IEnumerator Game_Playing_Timer() {
		while(timer >= 0) {
			yield return wfs;
			timer += -1;
			if ((timer % 15).Equals(0)) {
				//만약 15초 단위일 경우 아이템 드랍
				Debug.Log("아이템 소환");
			}
			if (timer.Equals(39)) {
				//만약 60초가 된다면, 범위 축소
				//Debug.Log("맵 범위 축소");
			}
			//타이머 UI 변경
			Timer.text = timer.ToString();
		}
		//0초 될 경우,
		//게임 종료 flow로.
		Game_Flow();
	}

	//----------------------------------------------------------------------
	//Game_END

	private void Game_End() {
		//누군가가 혼자 살아남는다
		//또는 시간초가 다 지났는데도 살아있는다.
		//그러면 여기로 오고
		//일단 바로 게임 종료
		isGameStarted = false;

		//모든 플레이어의 움직임을 제한.
		//게임 종료 메시지 띄우고
		MiddleTitle_text = "GAME SET!";
		//2초 뒤에 결과창.
		StartCoroutine("Game_End_Delay");
		//2초뒤에 나올 때, 다음 Flow 'Game_Flow(FlowState.Result)'
	}

	private IEnumerator Game_End_Delay() {
		yield return wfs;
		yield return wfs;
		Game_Flow();
	}

	//Game_Result

	private void Game_Result() {
		//승리자 or 무승부 메시지 띄우기

		//순위 보여주고
		//데이터 처리
		StartCoroutine("Game_RankResult");
		//방 대기실로 넘어가기

		Game_Flow();
	}

	private IEnumerator Game_RankResult() {
		yield return wfs;
		yield return wfs;
		//일단 1등만 표시하는걸로
		//MiddleTitle_text.text = $"Winner : {ranks_result[0].transform.name} player";

		//1등부터 4등까지 for문으로 하나씩 stack에서 꺼낸다.
		//UI에 등수 표시 및, rating 값 변동.
	}
}
