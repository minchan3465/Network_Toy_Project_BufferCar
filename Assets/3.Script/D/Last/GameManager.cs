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

	[Space]
	[Header("몇명이 되면 시작할거냐? (0~4)")]
	public int max_player = 1;
	[Space(30f)]

	//Sync할거
	[SyncVar(hook = nameof(OnGameStartingCheck))] public bool isGameStart;
	public readonly SyncList<int> playersHp = new SyncList<int>();
	public readonly SyncList<string> playersName = new SyncList<string>();
	public readonly SyncList<PlayerData> playersData = new SyncList<PlayerData>();

	//모두가 개인적으로 간직하는거
	public List<PlayerUI> playerHpUI = new List<PlayerUI>();
	public List<TMP_Text> playerNameUI = new List<TMP_Text>();
	public TMP_Text startCountdownTimer;
	public TMP_Text gameTimer;
	public TMP_Text middleTextUI;
	public TMP_Text winnerTextUI;
	public MeshRenderer winnerCar;
	public GameObject winnerCamera;
	public TMP_Text[] resultRankTextUI;
	public TMP_Text[] resultRateTextUI;
	public TMP_Text feverTextUI;
	private bool isFever = false;
	public GameObject spectatorCamera;

	public GameObject car;

	//Server가 관리할거
	public Stack<int> Ranks = new Stack<int>();
	public int winnerNumber = -1;
	public string winnerName = string.Empty;
	public int startCountdownTime = 3;
	public int gameTime = 99;


	///////////////////////////////////////////////////////////////////////
	//--------------------------- 메서드 파트 ---------------------------//
	///////////////////////////////////////////////////////////////////////
	private void Awake() {
		if (Instance == null) { Instance = this; } else { Destroy(Instance); }
	}
	private void Start() {
		//NetworkServer.OnDisconnectedEvent += OnClientDisconnected;
		playersData.OnChange += OnPlayersDataChanged;
		playersName.OnChange += OnPlayersNameChanged;
		playersHp.OnChange += OnHpListChanged;
		RefreshNameUI();
	}
	//플레이어 준비됨 파트
	public void ImReady(PlayerData player) {
		//if (!isOwned) return;
		//플레이어 정보 등록, HP 갱신
		playersData.Add(player);
		playersHp.Add(6);
		playersName.Add(player.nickname);
	}
	//플레이어 나가면, 그 번호는 Lost라는 이름을 가지게 하고, hp를 0으로 함.
	//근데 정보가 그대로 남아있을지는 모르겠음;
	public void SetDisconnectPlayerIndexInfo(int index) {
		PlayerData lostPlayer = playersData[index];
		lostPlayer.name = "Lost...";
		playersHp[index] = 0;
		if (!Ranks.Contains(lostPlayer.index)) {
			Ranks.Push(lostPlayer.index);
		}
	}

	//------------ UI 변경
	/////////////////////그리고 4명이 모였다면, (서버기준) 시작!!!!!!!!!!
	private void OnPlayersDataChanged(SyncList<PlayerData>.Operation op, int playernumber, PlayerData newItem) {
		if (isServer) {
			if (isGameStart) return;
			if (playersData.Count.Equals(max_player)) { //서버 시작 인원 설정@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
				StartCoroutine(Game_Start());
			}
		}
	}
	////////////////////////////////////////////////////////////////////////////////////////// Name변경
	private void OnPlayersNameChanged(SyncList<string>.Operation op, int playernumber, string newItem) {
		UpdateNameUI(playernumber, newItem);
	}
	private void RefreshNameUI() {
		for(int i = 0; i < playersData.Count; i++) {
			UpdateNameUI(i, playersData[i].nickname);
		}
	}
	private void UpdateNameUI(int playernumber, string name) {
		string str;
		string color = setColor(playernumber);
		if ((playernumber % 2).Equals(0)) { str = $"{playernumber + 1}P <color={color}>{name}</color>"; } 
		else { str = $"<color={color}>{name}</color> {playernumber + 1}P"; }
		playerNameUI[playernumber].text = str;
	}
	////////////////////////////////////////////////////////////////////////////////////////// Hp 변경
	private void OnHpListChanged(SyncList<int>.Operation op, int playernumber, int newItem) {
		UpdateHpUI(playernumber, playersHp[playernumber]);
	}
	private void UpdateHpUI(int playernumber, int currentHp) {
		// 해당 플레이어의 버튼들 순차적으로 끄기
		// 예: HP가 2라면 -> 0,1번 버튼은 ON(true), 2번 버튼은 OFF(false)
		// 버튼 인덱스가 현재 체력보다 작으면 활성화, 크거나 같으면 비활성화
		if (playernumber >= playerHpUI.Count) return;
		for (int i = 0; i < playerHpUI[playernumber].hpButtons.Length; i++) {
			playerHpUI[playernumber].hpButtons[i].interactable = (i * 2 < currentHp);
		}
	}



	//------------게임 루프 핵심 (시작과 종료)
	private void OnGameStartingCheck(bool oldCheck, bool newCheck) { }

	[Server]
	private void Game_Setup() {
		startCountdownTime = 3;
		gameTime = 99;
	}

	[Server]
	private IEnumerator Game_Start() {
		Game_Setup();
		yield return StartCoroutine("StartCdTimer_co");
		isGameStart = true;
		PlayerCanMoveChange(false);
		StartCoroutine("timer_countdown");
	}
	[ClientRpc]
	private void PlayerCanMoveChange(bool _bool) {
		if(car.TryGetComponent(out PlayerController playerController)) {
			playerController.IsStunned = _bool;
		}
	}

	[Server]
	private void Game_Set(int winnerNumber) {
		isGameStart = false;
		this.winnerNumber = winnerNumber;
		winnerName = playersData[winnerNumber].nickname;
		StopCoroutine("timer_countdown");
		OffFeverTime();
		UpdateMiddleTextUI("GAME SET!");
		StartCoroutine("Game_Result");
	}
	private IEnumerator Game_Result() {
		yield return new WaitForSeconds(3f);
		PlayerCanMoveChange(true);
		UpdateMiddleTextUI(string.Empty);

		/////////////////////////////////////////////////// 승리한 사람 텍스트
		string str = string.Empty;
		string color = setColor(winnerNumber);
		str = $"<color={color}>{winnerName}</color>\n{winnerNumber + 1}P Win!";
		///////////////////////////////////////////////////
		onWinnerCamera(winnerNumber);
		offSpectatorCamera();
		UpdateWinnerTextUI(str);
		
		yield return new WaitForSeconds(5f);
		//우승 결과
		UpdateWinnerTextUI(string.Empty);
		//플레이어 UI, 실제 데이터 변경
		StartCoroutine(ResultCal());

		//yield return new WaitForSeconds(7f);
		//룸으로 돌아가는 세팅
		//그 전에, 플레이어들한테 아바타 권한 다시 돌려놔야함.
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
	/// ///////////////////////////////////
	private IEnumerator ResultCal() {
		string result_rank = string.Empty;
		string result_rate = string.Empty;
		int rank_count = Ranks.Count;
		for (int i =0; i < rank_count; i++ ) {
			int index = Ranks.Pop();
			///////////////////////////////////////////////////////////////////////////////// 순위, 닉네임
			string color = setColor(index);
			result_rank = $"{i + 1}\t<color={color}>{playersData[index].nickname}</color>";
			///////////////////////////////////////////////////////////////////////////////// 순위, 레이팅
			int point = 0;
			bool isHigh = true;
			result_rate = $"{playersData[index].rate} ";
			switch (i) {
				case 0:
					point = 200;
					isHigh = true;
					result_rate += $"<color=orange>+ {point}</color>";
					break;
				case 1:
					point = 100;
					isHigh = true;
					result_rate += $"<color=orange>+ {point}</color>";
					break;
				case 2:
					point = -100;
					isHigh = false;
					result_rate += $"<color=blue>- {point}</color>";
					break;
				case 3:
					point = -200;
					isHigh = false;
					result_rate += $"<color=blue>- {point}</color>";
					break;
			}
			UpdateResultRanktTextUI(i, result_rank);
			UpdateResultRatetTextUI(i, result_rate);
			UpdateResultRatetTextUI(i, playersData[index].rate, point, 1f, isHigh);

			//플레이어 레이트값 조정
			UpdatePlayerRateToDB(point);
		}

		yield return new WaitForSeconds(4f);
		//룸으로 돌아가기
	}
	[ClientRpc] private void UpdateResultRanktTextUI(int index, string text) { resultRankTextUI[index].text = text; }
	[ClientRpc] private void UpdateResultRatetTextUI(int index, string text) { resultRateTextUI[index].text = text; }
	[ClientRpc] private void UpdateResultRatetTextUI(int index, int rate, int point, float duration, bool isHigh) {
		StartCoroutine(RateChangeAnimation_co(index, rate, point, duration, isHigh));
	}
	private IEnumerator RateChangeAnimation_co(int index, int rate, int point, float duration, bool isHigh) {
		yield return new WaitForSeconds(2f);
		float timer = 0f;
		string text = string.Empty;
		while(timer < duration) {
			timer += Time.deltaTime;
			int newRate = Mathf.RoundToInt(Mathf.Lerp(rate, rate + point, timer / duration));
			int newPoint = Mathf.RoundToInt(Mathf.Lerp(point, 0, timer / duration));
			if(isHigh) {
				text = $"{newRate} <color=orange>+ {newPoint}</color>";
			} else {
				text = $"{newRate} <color=blue>- {newPoint}</color>";
			}
			resultRateTextUI[index].text = text;
			yield return null;
		}
	}

	[ClientRpc]
	private void UpdatePlayerRateToDB(int rate) {
		//이게 클라이언트한테 시켜서 번호 업데이트하는거라 그냥 DB에 담긴 데이터를 통해 업데이트하는게 맞을듯.
		string id = DataManager.instance.playerInfo.User_ID;
		if(car.TryGetComponent(out PlayerData playerData)) {
			if(playerData.nickname.Equals(DataManager.instance.playerInfo.User_Nic)) {
				bool result = DataManager.instance.SetRate(id, rate);
				Debug.Log("DB 업데이트 결과 : " + result);
			}
		}
	}


	//------------ 추락시
	[Server]
	public void ProcessPlayerFell(int playerNum, NetworkConnectionToClient target) {
		playersHp[playerNum] -= 1;

		if (playersHp[playerNum] < 1) {
			//죽었음~
			Ranks.Push(playerNum);
			StopPlayerRespawn(target);
			OnSpectatorCamera(target);
		}
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
		else {
			Ranks.Push(winner_check);
			Game_Set(winner_check);
		}
	}
	[TargetRpc]
	private void StopPlayerRespawn(NetworkConnection target) {
		if (car.TryGetComponent(out PlayerRespawn playerRespawn)) {
			playerRespawn.canRespawn = false;
		}
	}

	[TargetRpc]
	private void OnSpectatorCamera(NetworkConnection target) {
		StartCoroutine(OnSpectatorCamera_co());
	}
	private IEnumerator OnSpectatorCamera_co() {
		middleTextUI.text = "game over...";
		yield return new WaitForSeconds(2f);
		middleTextUI.text = string.Empty;
		spectatorCamera.SetActive(true);
	}

	[ClientRpc]
	private void offSpectatorCamera() {
		if (spectatorCamera.activeSelf.Equals(false)) return;
		spectatorCamera.SetActive(false);
	}




	//-------------- Timer 변경
	[Server]
	public IEnumerator StartCdTimer_co() {
		WaitForSeconds wfs = new WaitForSeconds(1f);
		while (startCountdownTime >= 0) {
			if(startCountdownTime.Equals(0)) {
				UpdateStartCdTimer("Go!!!");
			} else {
				UpdateStartCdTimer(startCountdownTime.ToString());
			}
			yield return wfs;
			startCountdownTime -= 1;
		}
	}

	[ClientRpc]
	private void UpdateStartCdTimer(string startCdTimeText) {
		startCountdownTimer.text = startCdTimeText;
		StopCoroutine("FadeOutStartCdTimer");
		StartCoroutine("FadeOutStartCdTimer");
	}
	private IEnumerator FadeOutStartCdTimer() {
		WaitForSeconds wfs = new WaitForSeconds(0.01f);
		float alphaValue = 1f;
		while(alphaValue > 0) {
			alphaValue -= 0.01f;
			startCountdownTimer.color = new Color(1f, 1f, 1f, alphaValue);
			yield return wfs;
		}
	}

	[Server]
	public IEnumerator timer_countdown() {
		WaitForSeconds wfs = new WaitForSeconds(1f);
		while (gameTime >= 0) {
			yield return wfs;
			gameTime -= 1;
			UpdateGameTimer(gameTime);
		}
		//피버타임!!!!
		OnFeverTime();
	}
	[ClientRpc]
	private void UpdateGameTimer(int gameTime) {
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

