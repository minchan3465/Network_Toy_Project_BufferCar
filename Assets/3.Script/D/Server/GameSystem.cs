using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
	public Stack<GameObject> ranks;
	public GameObject[] ranks_result;
	public int max_player = 4;

	//테스트용
	public GameObject[] players_object;
	private int player_count = 0;

	//시간
	public int timer = 99;
	WaitForSeconds wfs = new WaitForSeconds(1f);
	//게임 플레이중 인지
	public bool isGameStarted = false;


	[Header("UI 오브젝트")]
	public Text MiddleTitle_text;
	public Text Playing_Timer_text;

	[Header("플레이어 스폰 위치")]
	public GameObject set_spawn_position;
	public float spawn_radius = 1f;
	public Vector3 spawn_position;
	public Vector3[] player_spawn_position;

	//-----------------------------------------------------

	public static GameSystem singletone = null;

	private void Awake() {
		if (singletone == null) {
			singletone = this;
		} else {
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
	}

	//켰을때 세팅
	private void Start() {
		players_object = new GameObject[max_player];

		players_info = new PlayerInfo[max_player];
		ranks = new Stack<GameObject>();
		ranks_result = new GameObject[max_player];

		player_spawn_position = new Vector3[max_player];
		player_spawn_position[0] = spawn_position + new Vector3(0, 0, -spawn_radius);
		player_spawn_position[1] = spawn_position + new Vector3(spawn_radius, 0, 0);
		player_spawn_position[2] = spawn_position + new Vector3(0, 0, spawn_radius);
		player_spawn_position[3] = spawn_position + new Vector3(-spawn_radius, 0, 0);

		Game_Start_Btn();
	}

	public void Test_Add_Player(GameObject player) {
		players_object[player_count] = player;
		player_count += 1;
	}

	//---------------------------------------------------------------------------

	//만약 시작 버튼을 눌렀을 때, 흐름 시작
	public void Game_Start_Btn() {
		Game_Flow(FlowState.SetUp);
	}

	//플레이어 리스트에 추가


	private void Update() {
		//게임 중이 아니라면, return
		if (!isGameStarted) return;
		//게임 중이라면
		//플레이하면서 Last Man Standing 하게 된다면
		//게임 즉시 종료!
		if (ranks.Count.Equals(3)) {
			Debug.Log("끝난 이유 : Last Man Standing");
			//등수가 2위까지 정해진다면,
			//마지막 한명의 등수까지 넣어줍니다 (1등).
			Game_RankSort();


			//타이머 돌아가는 코루틴 종료
			StopCoroutine("Game_Playing_Timer");
			
			//게임 종료 flow로.
			Game_Flow(FlowState.End);
		}
	}

	public void Game_Over_Player(GameObject player) {
		ranks.Push(player);
		Debug.Log($"{5 - ranks.Count}등 : {player.transform.name} 플레이어");
	}

	private void Game_RankSort() {
		//일단 1등 넣어주고
		foreach(GameObject player in players_object) {
			if(player.TryGetComponent(out Test_Player player_script)) {
				if(player_script.hp > 0) {
					Game_Over_Player(player);
				}
			}
		}
		//1등부터 4등까지 순차적으로 배열로 넣어주기.
		for(int i = 0; i<max_player; i++) {
			ranks_result[i] = ranks.Pop();
		}
	}

	// 게임 흐름 제어 플로우 상태머신?
	public void Game_Flow(FlowState state) {
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

	//-----------------------------------------------------------------------
	//실질적으로 게임 흐름 동작? 할 코드들 

	private void Game_SetUp() {
		//타이머 99초 설정
		timer = 99;
		Playing_Timer_text.text = timer.ToString();

		//각 플레이어의 시작 위치 조정
		for (int i = 0; i<max_player; i++) {
			//players_object[i].transform.position = player_spawn_position[i];
		}

		Game_Flow(FlowState.Start);
	}

	//----------------------------------------------------------------------
	//Game_Start

	private void Game_Start() {
		//UI적으로 3,2,1 하고
		//모든 플레이어의 움직임을 제한하는걸 풀어야함.

		StartCoroutine("Game_Start_Timer");
		//다음 flow는 'Game_Flow(FlowState.Playing)'
	}

	private IEnumerator Game_Start_Timer() {
		//보니까 Ready 2번 깜빡이고, Go! 하면서 바로 움직일 수 있게 되어있음!
		int start_countdown = 2;
		while(start_countdown >= 0) {
			if(start_countdown > 0) {
				MiddleTitle_text.text = start_countdown.ToString();
				//UI Ready FadeOut...
			} else {
				MiddleTitle_text.text = "GO!";
				//UI Go! FadeOut, ZoomIn
			}
			yield return wfs;
			start_countdown += -1;
		}
		MiddleTitle_text.text = string.Empty;
		Game_Flow(FlowState.Playing);
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
			Playing_Timer_text.text = timer.ToString();
		}
		//0초 될 경우,
		//게임 종료 flow로.
		Game_Flow(FlowState.End);
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
		MiddleTitle_text.text = "G A M E  S E T !";
		//2초 뒤에 결과창.
		StartCoroutine("Game_End_Delay");
		//2초뒤에 나올 때, 다음 Flow 'Game_Flow(FlowState.Result)'
	}

	private IEnumerator Game_End_Delay() {
		yield return wfs;
		yield return wfs;
		Game_Flow(FlowState.Result);
	}

	//----------------------------------------------------------------------
	//Game_Result

	private void Game_Result() {
		//승리자 or 무승부 메시지 띄우기

		//순위 보여주고
		//데이터 처리
		StartCoroutine("Game_RankResult");
		//방 대기실로 넘어가기

		Game_Flow(FlowState.Wait);
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
