using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class GameManager : NetworkBehaviour {
	public static GameManager Instance;

	// 플레이어별 HP 버튼 UI를 담는 클래스 (인스펙터에서 할당하기 편하게)
	[System.Serializable]
	public class PlayerUI {
		public Button[] hpButtons; // 3개씩 할당
	}

	//Sync할거
	[SyncVar(hook = nameof(OnGameStartingCheck))] public bool isGameStart;
	[SyncVar(hook = nameof(OnTimerChanged))] public int gameTime;
	public readonly SyncList<int> playersHp = new SyncList<int>();
	public readonly SyncList<string> playersName = new SyncList<string>();

	//모두가 개인적으로 간직하는거
	public List<PlayerUI> playerHpUI = new List<PlayerUI>();
	public List<TMP_Text> playerNameUI = new List<TMP_Text>();
	public TMP_Text gameTimer;
	public TMP_Text middleTextUI;
	public TMP_Text winnerTextUI;
	public MeshRenderer winnerCar;
	public GameObject winnerCamera;
	public int winnerNumber;
	public string winnerName;
	public TMP_Text resultRankTextUI;
	public TMP_Text resultRateTextUI;

	public TMP_Text feverTextUI;
	private bool isFever = false;

	//Server가 관리할거
	public List<NetworkPlayer> _connectedPlayers = new List<NetworkPlayer>();
	public Stack<int> Ranks = new Stack<int>();
	public List<int> playersRating = new List<int>();

	//---------메서드 파트

	private void Awake() {
		if (Instance == null) { Instance = this; } else { Destroy(Instance); }
	}

	private void Start() {
		Game_Start();
	}

	public override void OnStartClient() {
		base.OnStartClient();
		playersHp.OnChange += OnHpListChanged;
		playersName.OnChange += OnNameListChanged;
		RefreshAllHpUI();
		RefreshAllNameUI();
	}

	//게임이 시작 전, 설정 및 초기화.
	[Server]
	public void SetupGame() {
		///////////////////////////////////////초기화
		playersHp.Clear();
		playersName.Clear();
		_connectedPlayers.Clear();
		gameTime = 99;
		winnerNumber = -1;
		UpdateMiddleTextUI(string.Empty);
		UpdateWinnerTextUI(string.Empty);
		UpdateResultRanktTextUI(string.Empty);
		UpdateResultRatetTextUI(string.Empty);
		//////////////////////////////////////////////////////////////////////////플레이어 데이터 세팅
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
		for (int i = 0; i < players.Length; i++) {
			//Debug.Log("게임 시작 시, 인식된 플레이어 수 : " + players.Length);
			if (players[i].TryGetComponent(out NetworkPlayer manager)) {
				_connectedPlayers.Add(manager);
				playersHp.Add(6); // 초기 HP 설정
				playersName.Add(manager.nickname);
			}
		}
		//Debug.Log($"게임 셋업 완료: {_connectedPlayers.Count}명의 플레이어 준비됨.");
		/////////////////////////////////////////////////////////////////////////////
	}

	// 1. 이미 등록된 플레이어인지 확인
	// 2. 리스트에 추가
	// 3. 해당 플레이어의 초기 HP 생성 (3으로 설정)
	[Server]
	public void RegisterPlayer(PlayerManager manager, NetworkPlayer player) {
		if (_connectedPlayers.Contains(player)) return;
		StartCoroutine(delay(manager, player));
	}
	private IEnumerator delay(PlayerManager manager, NetworkPlayer player) {
		yield return new WaitForSeconds(1f);
		_connectedPlayers.Add(player);
		playersHp.Add(6);
		playersName.Add(manager.playerNickname);
		playersRating.Add(manager.playerRating);
	}



	//------------ 추락시
	[Server]
	public void ProcessPlayerFell(int playerNum)
	{
		// [수정] 안전장치 추가: 리스트 범위 밖이면 무시
		if (playerNum < 0 || playerNum >= playersHp.Count)
		{
			Debug.LogWarning($"[ProcessPlayerFell] 잘못된 플레이어 인덱스 감지: {playerNum}. (List Count: {playersHp.Count})");
			return;
		}

		// 기존 로직
		playersHp[playerNum] -= 1;

		if (playersHp[playerNum] < 1)
		{
			Ranks.Push(playerNum);
			Debug.Log("랭카" + Ranks.Count);
		}

		//플레이어 목숨 체크
		if (isGameStart)
		{
			CheckPlayerHps();
		}
	}
	private void CheckPlayerHps() {
		int alivePlayerCnt = 0;
		int winner_check = 0;
		for (int i = 0; i < playersHp.Count; i++) {
			if (playersHp[i] > 0) {
				alivePlayerCnt += 1;
				winner_check = i;
			}
		}
		if (alivePlayerCnt > 1) return;
		else {
			Ranks.Push(winner_check);
			Debug.Log("랭카" + Ranks.Count);
			Game_Set(winner_check);
		}
	}



	//------------게임 루프 핵심 (시작과 종료)
	private void OnGameStartingCheck(bool oldCheck, bool newCheck) { }

	[Server]
	public void Game_Start() {
		SetupGame();
		isGameStart = true;
		StartCoroutine("timer_countdown");
	}
	[Server]
	public void Game_Set(int winnerNumber) {
		isGameStart = false;
		this.winnerNumber = winnerNumber;
		winnerName = playersName[winnerNumber];
		StopCoroutine("timer_countdown");
		OffFeverTime();
		UpdateMiddleTextUI("GAME SET!");
		StartCoroutine("Game_Result");
	}
	[Server]
	public IEnumerator Game_Result() {
		yield return new WaitForSeconds(3f);
		UpdateMiddleTextUI(string.Empty);

		/////////////////////////////////////////////////// 승리한 사람 텍스트
		string str = string.Empty;
		string color = setColor(winnerNumber);
		str = $"<color={color}>{winnerName}</color>\n{winnerNumber + 1}P Win!";
		///////////////////////////////////////////////////
		onWinnerCamera(winnerNumber);
		UpdateWinnerTextUI(str);

		
		yield return new WaitForSeconds(5f);
		//우승 결과
		UpdateWinnerTextUI(string.Empty);
		//플레이어 UI 변경
		ResultTextCal();
		//실제 데이터 변경

		yield return new WaitForSeconds(7f);
		//임시 재시작
		offWinnerCamera();
		Game_Start();
	}
	[ClientRpc] private void UpdateMiddleTextUI(string str) { middleTextUI.text = str; }
	[ClientRpc] private void UpdateWinnerTextUI(string str) { winnerTextUI.text = str; }

	////////////////////////////승리 시, 카메라 활성화. 시간 후에 종료할거임.
	[ClientRpc]
	private void onWinnerCamera(int winnerNumber) {
		winnerCar.materials[0].color = setCarBodyColor(winnerNumber);
		winnerCamera.SetActive(true);
	}
	////////////////////////////
	[ClientRpc] private void offWinnerCamera() { winnerCamera.SetActive(false); }
	////////////////////////////
	[Server]
	private void ResultTextCal() {
		string result_rank = string.Empty;
		string result_rate = string.Empty;
		int rank_count = Ranks.Count;
		for (int i =0; i < rank_count; i++ ) {
			int index = Ranks.Pop();
			string color = setColor(index);
			result_rank += $"{i + 1}\t<color={color}>{playersName[index]}</color>";
			result_rate += $"{playersRating[index]} ";
			switch (i) {
				case 0:
					result_rate += $"<color=orange>+ 200</color>";
					break;
				case 1:
					result_rate += $"<color=orange>+ 100</color>";
					break;
				case 2:
					result_rate += $"<color=blue>- 100</color>";
					break;
				case 3:
					result_rate += $"<color=blue>- 200</color>";
					break;
			}
			result_rank += "\n\n";
			result_rate += "\n\n";
		}
		UpdateResultRanktTextUI(result_rank);
		UpdateResultRatetTextUI(result_rate);
	}
	[ClientRpc] private void UpdateResultRanktTextUI(string str) { resultRankTextUI.text = str; }
	[ClientRpc] private void UpdateResultRatetTextUI(string str) { resultRateTextUI.text = str; }

	//------------ UI 변경
	////////////////////////////////////////////////////////////////////////////////////////// Hp 변경
	private void OnHpListChanged(SyncList<int>.Operation op, int playernumber, int newItem) {
		if (op == SyncList<int>.Operation.OP_CLEAR) return;
		UpdateHpUI(playernumber, playersHp[playernumber]);
	}
	private void RefreshAllHpUI() {
		for (int i = 0; i < playersHp.Count; i++) {
			UpdateHpUI(i, playersHp[i]);
		}
	}
	private void UpdateHpUI(int playernumber, int currentHp) {
		// 해당 플레이어의 버튼들 순차적으로 끄기
		// 예: HP가 2라면 -> 0,1번 버튼은 ON(true), 2번 버튼은 OFF(false)
		// 버튼 인덱스가 현재 체력보다 작으면 활성화, 크거나 같으면 비활성화
		if (playernumber >= playerHpUI.Count) return;
		for (int i = 0; i < playerHpUI[playernumber].hpButtons.Length; i++) {
			playerHpUI[playernumber].hpButtons[i].interactable = (i*2 < currentHp);
		}
	}
	////////////////////////////////////////////////////////////////////////////////////////// Name 변경
	private void OnNameListChanged(SyncList<string>.Operation op, int playernumber, string newItem) {
		if (op == SyncList<string>.Operation.OP_CLEAR) return;
		UpdateNameUI(playernumber, playersName[playernumber]);
	}
	private void RefreshAllNameUI() {
		for (int i = 0; i < playersName.Count; i++) {
			UpdateNameUI(i, playersName[i]);
		}
	}
	private void UpdateNameUI(int playernumber, string name)
	{
		// [기존 코드] 이름 데이터 범위만 체크하고 있음 (부족함)
		if (playernumber >= playersName.Count) return;

		// [★추가할 안전장치] UI 리스트 범위도 체크해야 튕기지 않음!
		if (playerNameUI == null || playernumber >= playerNameUI.Count)
		{
			// UI가 연결 안 됐으면 그냥 무시 (에러 방지)
			return;
		}

		string str = string.Empty;
		string color = setColor(playernumber);
		if ((playernumber % 2).Equals(0))
		{
			str = $"{playernumber + 1}P <color={color}>{name}</color>";
		}
		else
		{
			str = $"<color={color}>{name}</color> {playernumber + 1}P";
		}

		// [★추가] 실제 텍스트 오브젝트가 존재하는지 확인
		if (playerNameUI[playernumber] != null)
		{
			playerNameUI[playernumber].text = str;
		}
	}


	//-------------- Timer 변경
	[Server]
	public IEnumerator timer_countdown() {
		WaitForSeconds wfs = new WaitForSeconds(1f);
		while (gameTime >= 0) {
			yield return wfs;
			gameTime -= 1;
		}
		//피버타임!!!!
		OnFeverTime();
	}
	private void OnTimerChanged(int oldTime, int newTime) {	UpdateGameTimer();}
	private void UpdateGameTimer() {
		if (gameTime < 0) return;
		gameTimer.text = gameTime.ToString();
	}


	[ClientRpc]
	private void OnFeverTime() {
		isFever = true;
		feverTextUI.text = "FEVER!";
		StartCoroutine("feverTextColorChange");
	}
	private IEnumerator feverTextColorChange() {
		WaitForSeconds wfs = new WaitForSeconds(0.01f);
		float color_index = 0;
		bool isMaxColor = false;
		while (true) {
			if (!isMaxColor) {
				color_index += 0.01f;
				if (color_index > 1f) {
					color_index = 1;
					isMaxColor = true;
				}
			} else {
				color_index -= 0.01f;
				if (color_index < 0f) {
					color_index = 0;
					isMaxColor = false;
				}
			}
			feverTextUI.color = new Color(1, color_index, color_index);
			yield return wfs;
		}
	}
	[ClientRpc]
	private void OffFeverTime() {
		if (!isFever) return;
		isFever = false;
		feverTextUI.text = string.Empty;
		StopCoroutine("feverTextColorChange");
	}


	//------------ 색 계산...
	private string setColor(int index) {
		string color;
		switch (index) {
			case 0:
				color = "red";
				break;
			case 1:
				color = "green";
				break;
			case 2:
				color = "blue";
				break;
			case 3:
				color = "yellow";
				break;
			default:
				color = "white";
				break;
		}
		return color;
	}
	private Color setCarBodyColor(int index) {
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

