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

	[SyncVar(hook = nameof(OnMiddleTextChanged))] public string middleText;
	[SyncVar(hook = nameof(OnWinTextChanged))] public string winnerText;

	[SyncVar] public int winnerNumber;

	//모두가 개인적으로 간직하는거
	public List<PlayerUI> playerUIs = new List<PlayerUI>();
	public TMP_Text gameTimer;
	public TMP_Text middleTextUI;
	public TMP_Text winnerTextUI;
	public MeshRenderer winnerCar;
	public GameObject winnerCamera;

	//Server가 관리할거
	public List<PlayerManager> _connectedPlayers = new List<PlayerManager>();


	//---------메서드 파트

	private void Awake() {
		if (Instance == null) { Instance = this; } else { Destroy(Instance); }
	}

	private void Start() {
		Game_Start();
	}

	public override void OnStartClient() {
		base.OnStartClient();
		playersHp.OnChange += OnHpListChanged; // SyncList 값이 변할 때마다 실행될 함수 등록 (최신 Mirror 방식)
		RefreshAllHpUI();
	}

	//게임이 시작 전, 설정 및 초기화.
	[Server]
	public void SetupGame() {
		playersHp.Clear();
		_connectedPlayers.Clear();
		gameTime = 99;
		winnerNumber = -1;
		middleText = string.Empty;
		winnerText = string.Empty;

		// 2. 현재 씬에 있는 모든 PlayerIdentity 객체를 찾음
		// (실제 서비스에서는 NetworkManager에서 생성될 때 리스트에 추가하는 방식이 더 정확합니다)
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

		for (int i = 0; i < players.Length; i++) {
			Debug.Log("게임 시작 시, 인식된 플레이어 수 : " + players.Length);
			if (players[i].TryGetComponent(out PlayerManager manager)) {
				manager.playerNumber = i; // 순번 배정
				_connectedPlayers.Add(manager);
				playersHp.Add(3); // 초기 HP 설정
			}
		}

		//임시 시작용
		Debug.Log($"게임 셋업 완료: {_connectedPlayers.Count}명의 플레이어 준비됨.");
	}

	// 1. 이미 등록된 플레이어인지 확인
	// 2. 리스트에 추가 (들어온 순서대로 순번이 결정됨)
	// 3. 플레이어 객체에 순번 할당 (SyncVar를 통해 클라이언트로 전달됨)
	// 4. 해당 플레이어의 초기 HP 생성 (3으로 설정)
	[Server]
	public void RegisterPlayer(PlayerManager player) {
		if (_connectedPlayers.Contains(player)) return;

		_connectedPlayers.Add(player);

		int newIndex = _connectedPlayers.Count - 1;
		player.playerNumber = newIndex;

		playersHp.Add(3);

		//Debug.Log($"플레이어 등록 완료: Index {newIndex}, 현재 총원: {_connectedPlayers.Count}");
	}

	//------------ 추락시
	[Server]
	public void ProcessPlayerFell(int playerNum) {
		if (playerNum < 0 || playerNum >= playersHp.Count) return;
		playersHp[playerNum] -= 1;
		_connectedPlayers[playerNum].playerHp = playersHp[playerNum];

		//플레이어 목숨 체크
		if (isGameStart) {
			CheckPlayerHps();
		}
	}

	private void CheckPlayerHps() {
		int alivePlayerCnt = 0;
		int winner_check = 0;
		for(int i=0; i<playersHp.Count; i++) {
			if (playersHp[i] > 0) {
				alivePlayerCnt += 1;
				winner_check = i;
			}
		}
		if (alivePlayerCnt > 1) return;
		else Game_Set(winner_check);
	}

	//------------게임 루프 핵심 (시작과 종료)
	private void OnGameStartingCheck(bool oldCheck, bool newCheck) {
		//if (!isGameStart) {

		//} else {

		//}
	}

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
		StopCoroutine("timer_countdown");
		middleText = "GAME SET!";
		StartCoroutine("Game_Result");
	}
	[Server]
	public IEnumerator Game_Result() {
		yield return new WaitForSeconds(3f);
		middleText = string.Empty;

		onWinnerCamera();
		if (winnerNumber.Equals(-1)) {
			winnerText = "NO ONE\nDRAW!";
		} else {
			string color;
			switch (winnerNumber) {
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
					color = string.Empty;
					break;
			}
			winnerText = $"<color={color}>playername</color>\n{winnerNumber + 1}P Win!";
		}

		//임시 재시작
		yield return new WaitForSeconds(5f);
		offWinnerCamera();
		Game_Start();
	}

	//중간 텍스트 변경시
	private void OnMiddleTextChanged(string oldText, string newText) {
		UpdateMiddleTextUI();
	}

	private void UpdateMiddleTextUI() {
		middleTextUI.text = middleText;
	}

	//승리 텍스트 변경시
	private void OnWinTextChanged(string oldText, string newText) {
		UpdateWinTextUI();
	}

	//승리 텍스트 UI에 text값 입력, 화면 끄기
	private void UpdateWinTextUI() {
		winnerTextUI.text = winnerText;
	}

	//승리 시, 카메라 활성화. 시간 후에 종료할거임.
	[ClientRpc]
	private void onWinnerCamera() {
		winnerCar.materials[0].color = setCarBodyColor(winnerNumber);
		winnerCamera.SetActive(true);
	}

	[ClientRpc]
	private void offWinnerCamera() {
		winnerCamera.SetActive(false);
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

	//------------ HP 변경

	private void OnHpListChanged(SyncList<int>.Operation op, int playernumber, int newItem) {
		if (op == SyncList<int>.Operation.OP_CLEAR) return; //지워라~~~
		UpdateSinglePlayerUI(playernumber, playersHp[playernumber]);
	}

	private void RefreshAllHpUI() {
		for (int i = 0; i < playersHp.Count; i++) {
			UpdateSinglePlayerUI(i, playersHp[i]);
		}
	}

	// 해당 플레이어의 버튼들 순차적으로 끄기
	// 예: HP가 2라면 -> 0,1번 버튼은 ON(true), 2번 버튼은 OFF(false)
	// 버튼 인덱스가 현재 체력보다 작으면 활성화, 크거나 같으면 비활성화
	private void UpdateSinglePlayerUI(int playernumber, int currentHp) {
		if (playernumber >= playerUIs.Count) return;
		for (int i = 0; i < playerUIs[playernumber].hpButtons.Length; i++) {
			playerUIs[playernumber].hpButtons[i].interactable = (i < currentHp);
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
		Game_Set(-1);
	}

	private void OnTimerChanged(int oldTime, int newTime) {
		UpdateGameTimer();
	}

	private void UpdateGameTimer() {
		if (gameTime < 0) return;
		gameTimer.text = gameTime.ToString();
	}
}
