using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using Mirror; // Mirror 추가

public class Lobby_UI_Controller : MonoBehaviour {

	//테스트용 컴포넌트
	//public LobbyManager _LobbyManager;

	private bool isReady = false; // 이건 우진님이 다른데서 받아서 쓰셔두 댈듯합니당

	[Header("버튼 UI")]
	public Button Ready_btn;    // 레디 버튼 오브젝트
	public Text Ready_btn_text; // 레디 버튼 텍스트
	private Image readyBtnImage;// 버튼의 색상을 바꾸기 위해
	public Button Quit_btn;     // 나가기 버튼 오브젝트

	[Header("시작 연출 UI")]
	public Text Notice_text;    // [게임이 곧 시작됩니다.] 가 출력될 텍스트
	public Text Count_text;     // 3.. 2.. 1.. 이 출력될 텍스트

	[Header("플레이어 상태 UI")]
	// 1, 2, 3, 4번 플레이어의 테두리 이미지를 인스펙터에서 넣어주세요.
	public Image[] Player_Frames;

	// 카운트다운 도중에 누군가 나갔을 때, 실행 중인 숫지 세기(코루틴)를 
	// 강제로 멈추기 위해 이 변수가 필요합니다.
	private Coroutine countdownCoroutine;

	// [추가됨] 색상 변수 선언
	private Color unreadyColor;
	private Color readyColor;

	[Header("UI Elements (Size 4)")]
	// 1~4번 자리에 맞는 이미지와 텍스트를 드래그해서 넣어주세요.
	public Image[] playerImages = new Image[4];
	public Text[] playerInfoTexts = new Text[4]; 

	void Awake()
	{
		// 버튼에서 Image 컴포넌트를 찾아 할당합니다.
		if (Ready_btn != null)
		{
			readyBtnImage = Ready_btn.GetComponent<Image>();
		}

		// 게임 시작 시 UI가 켜져있다면 미리 꺼둡니다.
		Notice_text.gameObject.SetActive(false);
		Count_text.gameObject.SetActive(false);

		// 버튼들은 활성화 상태여야 합니다.
		Ready_btn.gameObject.SetActive(true);
		Quit_btn.gameObject.SetActive(true);
		// 버튼들은 처음에 당연히 클릭 가능해야 함
		Ready_btn.interactable = true;
		Quit_btn.interactable = true;

		// 색상 초기화 (Hex 코드를 Color로 변환)
		ColorUtility.TryParseHtmlString("#FFAAAA", out unreadyColor);
		ColorUtility.TryParseHtmlString("#FFFFFF", out readyColor);

		// 모든 플레이어 테두리를 기본 색상(FFAAAA)으로 초기화
		foreach (Image frame in Player_Frames)
		{
			if (frame != null)
			{
				frame.color = unreadyColor;
			}
		}

		// [추가된 로직] 실제 플레이어 정보 UI(이미지/텍스트)를 처음에 모두 끕니다.
		for (int i = 0; i < 4; i++)
		{
			if (playerImages[i] != null) playerImages[i].gameObject.SetActive(false);
			if (playerInfoTexts[i] != null) playerInfoTexts[i].gameObject.SetActive(false);
		}
	}
	// [서버 연동용] 특정 플레이어의 레디 상태에 따라 색상을 바꾸는 메소드
	// LobbyManager나 Player 스크립트에서 이쪽으로 신호를 주면 됩니다.
	public void UpdatePlayerFrameColor(int playerIndex, bool isPlayerReady)
	{
		// 인덱스 범위 체크 (0~3)
		if (playerIndex >= 0 && playerIndex < Player_Frames.Length)
		{
			if (isPlayerReady == true)
			{
				Player_Frames[playerIndex].color = readyColor;
			}
			else
			{
				Player_Frames[playerIndex].color = unreadyColor;
			}
		}
	}

	public void	Lobby_Ready() {
		//Ready인지 아닌지, 누를때마다 바뀜.
		isReady = !isReady;

		if(isReady) {
			//서버에 준비되었다는 메시지
			//UI상으로 Ready표시
			Ready_btn_text.text = "CANCLE";
			//레디 상태일 때 버튼 색상을 흰색(FFFFFF)으로 변경
			readyBtnImage.color = readyColor;
		} 
		else {
			//서버에 준비 해제되었다는 메시지
			//UI상으로 Ready표시 해제
			Ready_btn_text.text = "READY";
			//레디 해제 상태일 때 버튼 색상을 연빨강(FFAAAA)으로 변경
			readyBtnImage.color = unreadyColor;
		}

		// [핵심 추가] 내 UserInfoManager를 찾아서 서버에 레디 상태를 쏩니다.
		// UserInfoManager에 싱글톤을 안 쓰기로 했으므로, NetworkClient.localPlayer를 이용합니다.
		if (NetworkClient.localPlayer != null)
		{
			UserInfoManager myInfo = NetworkClient.localPlayer.GetComponent<UserInfoManager>();
			if (myInfo != null)
			{
				myInfo.CmdSendReadyToServer(isReady);
			}
		}
	}

	//서버에 의해 호출됨
	// 모든 플레이어가 준비되었을 때 서버나 로비매니저가 호출할 메소드
	public void StartGameSequence()
	{
		//모두 레디를 하면 버튼을 사라지게 하지 않고, 클릭만 못하게 막습니다.
		Ready_btn.interactable = false;
		Quit_btn.interactable = false;

		// 텍스트들을 활성화하고 카운트다운 시작
		Notice_text.gameObject.SetActive(true);
		Count_text.gameObject.SetActive(true);

		// 코루틴을 사용하여 시간차 연출을 시작합니다.
		// 서버 신호가 여러 번 오거나 실수가 생겨도 카운트다운이 
		// 두 개씩 돌아가지 않도록 기존 코루틴을 먼저 찾아 멈춰줍니다.
		if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
		countdownCoroutine = StartCoroutine(C_StartCountdown());
	}

	//서버에 의해 호출됨
	// 4명이 다 찼다가 한 명이 나갔을 때 호출하는 용도입니다.
	// 텍스트를 다시 숨기고 버튼을 다시 클릭 가능하게(interactable = true) 만듭니다.
	public void CancelGameSequence()
	{
		if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);

		Notice_text.gameObject.SetActive(false);
		Count_text.gameObject.SetActive(false);

		Ready_btn.interactable = true;
		Quit_btn.interactable = true;
	}

	IEnumerator C_StartCountdown()
	{
		// 1. 안내 문구 표시
		Notice_text.text = "게임이 곧 시작됩니다.";

		// 2. 3초 카운트다운
		int count = 3;
		while (count > 0)
		{
			Count_text.text = count.ToString();
			yield return new WaitForSeconds(1.0f); // 1초 대기
			count--;
		}

		// 3. 마지막 문구 표시
		Count_text.text = "START!";

		// [중요] 씬 이동은 오직 서버(Host)만 명령할 수 있습니다.
		if (NetworkServer.active)
		{
			yield return new WaitForSeconds(0.5f);
			// 모든 클라이언트를 한꺼번에 인게임 씬으로 데려갑니다.
			NetworkManager.singleton.ServerChangeScene("Main_InGame!");
		}
	}
	public void Lobby_Quit() {
		//NetworkClient.StopClient();
		//네트워크 클라이언트 종료까지 안전하게 하기.
		if (NetworkClient.active)
		{
			NetworkManager.singleton.StopClient();
		}
		//메인신으로 돌아가기
		SceneManager.LoadScene("Main_Title!");
	}

	public void UpdateSlotText(int index, string nickname, int rate)
	{
		if (index < 0 || index >= playerImages.Length) return;

		// 닉네임이 있으면 유저가 있는 것으로 간주
		if (!string.IsNullOrEmpty(nickname))
		{
			// 이미지와 텍스트 활성화
			playerImages[index].gameObject.SetActive(true);
			playerInfoTexts[index].gameObject.SetActive(true);

			// 요청하신 형식: 닉네임 + 줄바꿈 + 레이팅RP
			playerInfoTexts[index].text = nickname + "\n" + rate + "RP";
		}
		else
		{
			// 유저가 없으면(나갔으면) 비활성화
			playerImages[index].gameObject.SetActive(false);
			playerInfoTexts[index].gameObject.SetActive(false);
			playerInfoTexts[index].text = "";
		}
	}
}

