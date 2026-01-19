using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror; // Mirror 필수

public class Lobby_UI_Controller : MonoBehaviour
{

    [Header("버튼 UI")]
    public Button Ready_btn;        // 레디 버튼
    public Text Ready_btn_text;     // 레디 버튼 텍스트
    private Image readyBtnImage;    // 버튼 색상 변경용
    public Button Quit_btn;         // 나가기 버튼

    [Header("시작 연출 UI")]
    public Text Notice_text;        // "게임이 곧 시작됩니다"
    public Text Count_text;         // "3, 2, 1..."

    [Header("플레이어 슬롯 UI (인스펙터 연결 필요)")]
    // 1~4번 플레이어의 테두리 이미지
    public Image[] Player_Frames;
    // [추가] 1~4번 플레이어의 닉네임 텍스트 (UI에 닉네임 텍스트들을 만들어서 넣어주세요)
    public Text[] Player_Nicknames;
    // [추가] 1~4번 플레이어의 레이팅 텍스트 (UI에 점수 텍스트들을 만들어서 넣어주세요)
    public Text[] Player_Rates;

    // 카운트다운 제어용 코루틴
    private Coroutine countdownCoroutine;

    // 색상 변수
    private Color unreadyColor;
    private Color readyColor;

    // 현재 나의 레디 상태 (UI 표시용)
    private bool isMyReadyState = false;

    void Awake()
    {
        // 1. 버튼 이미지 컴포넌트 가져오기
        if (Ready_btn != null)
        {
            readyBtnImage = Ready_btn.GetComponent<Image>();
        }

        // 2. 시작 시 불필요한 UI 숨기기
        if (Notice_text) Notice_text.gameObject.SetActive(false);
        if (Count_text) Count_text.gameObject.SetActive(false);

        // 3. 버튼 초기화
        if (Ready_btn) { Ready_btn.interactable = true; Ready_btn.gameObject.SetActive(true); }
        if (Quit_btn) { Quit_btn.interactable = true; Quit_btn.gameObject.SetActive(true); }

        // 4. 색상 초기화 (#FFAAAA: 준비전, #FFFFFF: 준비완료)
        ColorUtility.TryParseHtmlString("#FFAAAA", out unreadyColor);
        ColorUtility.TryParseHtmlString("#FFFFFF", out readyColor);

        // 5. 슬롯 초기화 (모든 슬롯을 비우거나 기본 상태로)
        InitializeSlots();
    }

    // UI 슬롯을 깔끔하게 초기화하는 함수
    void InitializeSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            if (i < Player_Frames.Length && Player_Frames[i] != null)
                Player_Frames[i].color = unreadyColor;

            if (i < Player_Nicknames.Length && Player_Nicknames[i] != null)
                Player_Nicknames[i].text = "Waiting...";

            if (i < Player_Rates.Length && Player_Rates[i] != null)
                Player_Rates[i].text = "-";
        }
    }

    // ---------------------------------------------------------
    // [서버 연동] UserInfoManager에서 호출하는 함수들
    // ---------------------------------------------------------

    // 1. 테두리 색상 변경 (레디 상태 반영)
    public void UpdatePlayerFrameColor(int slotIndex, bool isPlayerReady)
    {
        if (slotIndex < 0 || slotIndex >= Player_Frames.Length) return;

        if (Player_Frames[slotIndex] != null)
        {
            Player_Frames[slotIndex].color = isPlayerReady ? readyColor : unreadyColor;
        }
    }

    // 2. [신규] 텍스트 정보 갱신 (닉네임, 레이팅 반영)
    // UserInfoManager.RefreshUI() 에서 이 함수를 호출해야 합니다.
    public void UpdateSlotText(int slotIndex, string nick, int rate)
    {
        if (slotIndex < 0) return; // 아직 번호를 할당받지 못한 경우 무시

        // 닉네임 갱신
        if (slotIndex < Player_Nicknames.Length && Player_Nicknames[slotIndex] != null)
        {
            Player_Nicknames[slotIndex].text = string.IsNullOrEmpty(nick) ? "Connecting..." : nick;
        }

        // 레이팅 갱신
        if (slotIndex < Player_Rates.Length && Player_Rates[slotIndex] != null)
        {
            Player_Rates[slotIndex].text = (rate == -1) ? "" : $"Score: {rate}";
        }
    }


    // ---------------------------------------------------------
    // [유저 입력] 버튼 클릭 이벤트
    // ---------------------------------------------------------

    public void Lobby_Ready()
    {
        // 1. 내 로컬 플레이어 객체를 UserInfoManager 타입으로 가져옵니다.
        UserInfoManager myInfo = NetworkClient.localPlayer?.GetComponent<UserInfoManager>();

        if (myInfo != null)
        {
            // 2. Mirror 룸매니저에게 내 레디 상태를 반전시켜달라고 명령합니다. (CmdChangeReadyState)
            // 이 명령을 내리면 서버가 상태를 바꾸고, 모든 유저의 OnClientReadyStateChanged가 실행됩니다.
            bool nextState = !myInfo.readyToBegin;
            myInfo.CmdChangeReadyState(nextState);

            // 3. 내 버튼의 시각적 텍스트/색상만 즉시 업데이트합니다.
            UpdateMyButtonState(nextState);
        }
    }

    // 내 버튼의 텍스트와 색상을 바꾸는 내부 함수
    private void UpdateMyButtonState(bool ready)
    {
        if (ready)
        {
            if (Ready_btn_text) Ready_btn_text.text = "CANCEL";
            if (readyBtnImage) readyBtnImage.color = readyColor;
        }
        else
        {
            if (Ready_btn_text) Ready_btn_text.text = "READY";
            if (readyBtnImage) readyBtnImage.color = unreadyColor;
        }
    }

    public void Lobby_Quit()
    {
        // 네트워크 연결 종료
        if (NetworkClient.active)
        {
            if (NetworkServer.active) NetworkManager.singleton.StopHost();
            else NetworkManager.singleton.StopClient();
        }
        // 메인 타이틀로 이동
        SceneManager.LoadScene("Main_Title!");
    }


    // ---------------------------------------------------------
    // [게임 흐름] 게임 시작 카운트다운
    // ---------------------------------------------------------

    public void StartGameSequence()
    {
        // 버튼 잠금
        if (Ready_btn) Ready_btn.interactable = false;
        if (Quit_btn) Quit_btn.interactable = false;

        // 텍스트 활성화
        if (Notice_text) Notice_text.gameObject.SetActive(true);
        if (Count_text) Count_text.gameObject.SetActive(true);

        // 중복 실행 방지 후 코루틴 시작
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(C_StartCountdown());
    }

    public void CancelGameSequence()
    {
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);

        if (Notice_text) Notice_text.gameObject.SetActive(false);
        if (Count_text) Count_text.gameObject.SetActive(false);

        // 버튼 다시 활성화
        if (Ready_btn) Ready_btn.interactable = true;
        if (Quit_btn) Quit_btn.interactable = true;
    }

    IEnumerator C_StartCountdown()
    {
        if (Notice_text) Notice_text.text = "게임이 곧 시작됩니다.";

        int count = 3;
        while (count > 0)
        {
            if (Count_text) Count_text.text = count.ToString();
            yield return new WaitForSeconds(1.0f);
            count--;
        }

        if (Count_text) Count_text.text = "START!";

        // 씬 이동은 서버(Host)만 명령 가능
        if (NetworkServer.active)
        {
            yield return new WaitForSeconds(0.5f);
            NetworkManager.singleton.ServerChangeScene("Main_InGame!");
        }
    }
}