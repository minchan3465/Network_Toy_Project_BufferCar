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

	public TMP_Text feverTextUI;
	private bool isFever = false;

	//Server가 관리할거
	public List<NetworkPlayer> _connectedPlayers = new List<NetworkPlayer>();

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
		_connectedPlayers.Clear();
		gameTime = 99;
		winnerNumber = -1;
		UpdateMiddleTextUI(string.Empty);
		UpdateWinTextUI(string.Empty);
		//////////////////////////////////////////////////////////////////////////플레이어 데이터 세팅
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
		for (int i = 0; i < players.Length; i++) {
			//Debug.Log("게임 시작 시, 인식된 플레이어 수 : " + players.Length);
			if (players[i].TryGetComponent(out NetworkPlayer manager)) {
				_connectedPlayers.Add(manager);
				playersHp.Add(3); // 초기 HP 설정
				playersName.Add(manager.nickname);
			}
		}
		Debug.Log($"게임 셋업 완료: {_connectedPlayers.Count}명의 플레이어 준비됨.");
		/////////////////////////////////////////////////////////////////////////////
	}

	// 1. 이미 등록된 플레이어인지 확인
	// 2. 리스트에 추가
	// 3. 해당 플레이어의 초기 HP 생성 (3으로 설정)
	[Server]
	public void RegisterPlayer(NetworkPlayer manager) {
		if (_connectedPlayers.Contains(manager)) return;
		StartCoroutine(delay(manager));
	}

	private IEnumerator delay(NetworkPlayer manager) {
		yield return new WaitForSeconds(0.2f);
		_connectedPlayers.Add(manager);
		playersHp.Add(3);
		playersName.Add(manager.nickname);
	}



	//------------ 추락시
	[Server]
	public void ProcessPlayerFell(int playerNum) {
		playersHp[playerNum] -= 1;
		Debug.Log(playerNum + "떨어짐! 남은 HP : " + playersHp[playerNum]);

		//플레이어 목숨 체크
		if (isGameStart) {
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
		else Game_Set(winner_check);
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

		onWinnerCamera();
		/////////////////////////////////////////////////// 승리한 사람 텍스트
		string str = string.Empty;
		string color = setColor(winnerNumber);
		str = $"<color={color}>{winnerName}</color>\n{winnerNumber + 1}P Win!";
		///////////////////////////////////////////////////
		UpdateWinTextUI(str);

		//임시 재시작
		yield return new WaitForSeconds(5f);
		offWinnerCamera();
		Game_Start();
	}


	[ClientRpc] private void UpdateMiddleTextUI(string str) { middleTextUI.text = str; } //중간 텍스트 변경시
	[ClientRpc] private void UpdateWinTextUI(string str) { winnerTextUI.text = str; }//승리 텍스트 UI에 text값 입력, 화면 끄기
	////////////////////////////승리 시, 카메라 활성화. 시간 후에 종료할거임.
	[ClientRpc]
	private void onWinnerCamera() {
		winnerCar.materials[0].color = setCarBodyColor(winnerNumber);
		winnerCamera.SetActive(true);
	}
	////////////////////////////
	[ClientRpc] private void offWinnerCamera() { winnerCamera.SetActive(false); }



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
			playerHpUI[playernumber].hpButtons[i].interactable = (i < currentHp);
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
	private void UpdateNameUI(int playernumber, string name) {
		if (playernumber >= playersName.Count) return;
		string str = string.Empty;
		string color = setColor(playernumber);
		if((playernumber % 2).Equals(0)) { 
			str = $"{playernumber + 1}P <color={color}>{name}</color>";
		} else { 
			str = $"<color={color}>{name}</color> {playernumber + 1}P"; 
		}
		Debug.Log(str);
		playerNameUI[playernumber].text = str;
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

