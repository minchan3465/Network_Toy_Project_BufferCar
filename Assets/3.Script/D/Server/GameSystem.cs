using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FlowState {
	Wait,
	SetUp,
	Start,
	Playing,
	End,
	Result
}

public class GameSystem : MonoBehaviour {
	//서버용 스크립트.
	//즉, Client에서 신호를 받아서 처리가 된다면, 다음 게임 진행이 되어야함.

	/*
	 서버가 가지고 있을 정보
	 1. 유저 순서 ( 1 ~ 4 P )
	 2. 유저 순위 Stack형
	 */

	//플레이어 정보
	public PlayerInfo[] players_info;
	public Stack<PlayerInfo> ranks;
	public int max_player = 4;

	//테스트용
	public GameObject[] players_object;


	//시간
	public int timer = 99;
	WaitForSeconds wfs = new WaitForSeconds(1f);
	//게임 플레이중 인지
	public bool isGameStarted = false;

	[Header("플레이어 스폰 위치")]
	public GameObject set_spawn_position;
	public float spawn_radius = 1f;
	public Vector3 spawn_position;
	public Vector3[] player_spawn_position;

	//-----------------------------------------------------

	//켰을때 세팅
	private void Start() {
		players_object = new GameObject[max_player];

		players_info = new PlayerInfo[max_player];
		ranks = new Stack<PlayerInfo>();

		player_spawn_position = new Vector3[max_player];
		player_spawn_position[0] = spawn_position + new Vector3(0, 0, -spawn_radius);
		player_spawn_position[1] = spawn_position + new Vector3(spawn_radius, 0, 0);
		player_spawn_position[2] = spawn_position + new Vector3(0, 0, spawn_radius);
		player_spawn_position[3] = spawn_position + new Vector3(-spawn_radius, 0, 0);
	}

	//---------------------------------------------------------------------------

	//만약 시작 버튼을 눌렀을 때, 흐름 시작
	public void Game_Start_Btn() {
		Game_Flow(FlowState.SetUp);
	}

	private void Update() {
		//게임 중이 아니라면, return
		if (!isGameStarted) return;
		//게임 중이라면
		//플레이하면서 Last Man Standing 하게 된다면
		//게임 즉시 종료!
		if (ranks.Count.Equals(4)) {
			//등수가 정해진다면,
			//타이머 돌아가는 코루틴 종료
			StopCoroutine("Game_Playing_Timer");
			//게임 종료 flow로.
			Game_Flow(FlowState.End);
		}
	}

	public void Game_Drop_Player(PlayerInfo player) {
		ranks.Push(player);
	}

	// 게임 흐름 제어 플로우 상태머신?
	public void Game_Flow(FlowState state) {
		switch (state) {
			case FlowState.SetUp:
				Game_SetUp();
				break;
			case FlowState.Start:
				Game_Start();
				break;
			case FlowState.Playing:
				Game_Playing();
				break;
			case FlowState.End:
				Game_End();
				break;
			case FlowState.Result:
				Game_Result();
				break;
		}
	}

	//-----------------------------------------------------------------------
	//실질적으로 게임 흐름 동작? 할 코드들 

	private void Game_SetUp() {
		//타이머 99초 설정
		timer = 99;

		//각 플레이어의 시작 위치 조정
		for (int i = 0; i<max_player; i++) {
			players_object[i].transform.position = player_spawn_position[i];
		}

		Game_Flow(FlowState.Start);
	}

	private void Game_Start() {
		//UI적으로 3,2,1 하고
		//모든 플레이어의 움직임을 제한하는걸 풀어야함.

		StartCoroutine("Game_Start_Timer");
		//다음 flow는 'Game_Flow(FlowState.Playing)'
	}

	private IEnumerator Game_Start_Timer() {
		//보니까 Ready 2번 깜빡이고, Go! 하면서 바로 움직일 수 있게 되어있음!
		int start_countdown = 2;
		while(start_countdown < 0) {
			if(start_countdown > 0) {
				//UI Ready FadeOut...
			} else {
				//UI Go! FadeOut, ZoomIn
			}
			yield return wfs;
		}
		Game_Flow(FlowState.Playing);
	}

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
		while(timer < 0) {
			yield return wfs;
			timer += -1;
			//만약 15초 단위일 경우 아이템 드랍
			//만약 60초가 된다면, 범위 축소
			//타이머 UI 변경
		}
		//0초 될 경우,
		//게임 종료 flow로.
		Game_Flow(FlowState.End);
	}

	private void Game_End() {
		//누군가가 혼자 살아남는다
		//또는 시간초가 다 지났는데도 살아있는다.
		//그러면 여기로 오고
		//일단 바로 게임 종료
		isGameStarted = false;

		//모든 플레이어의 움직임을 제한.
		//게임 종료 메시지 띄우고
		//2초 뒤에 결과창.
		Game_Flow(FlowState.Result);
	}

	private void Game_Result() {
		//승리자 or 무승부 메시지 띄우기
		//순위 보여주고
		//데이터 처리
		Game_RankResult();
		//방 대기실로 넘어가기

		Game_Flow(FlowState.Wait);
	}

	private void Game_RankResult() {
		//1등부터 4등까지 for문으로 하나씩 stack에서 꺼낸다.
		//UI에 등수 표시 및, rating 값 변동.
	}
}
