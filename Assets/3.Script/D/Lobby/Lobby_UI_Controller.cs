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
    public Image[] Player_Frames;
    public Text[] Player_Nicknames;
    public Text[] Player_Rates;

    private Coroutine countdownCoroutine;
    private Color unreadyColor;
    private Color readyColor;

    void Awake()
    {
        if (Ready_btn != null) readyBtnImage = Ready_btn.GetComponent<Image>();
        if (Notice_text) Notice_text.gameObject.SetActive(false);
        if (Count_text) Count_text.gameObject.SetActive(false);
        if (Ready_btn) { Ready_btn.interactable = true; Ready_btn.gameObject.SetActive(true); }
        if (Quit_btn) { Quit_btn.interactable = true; Quit_btn.gameObject.SetActive(true); }

        ColorUtility.TryParseHtmlString("#FFAAAA", out unreadyColor);
        ColorUtility.TryParseHtmlString("#FFFFFF", out readyColor);

        InitializeSlots();
    }

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

    public void UpdatePlayerFrameColor(int slotIndex, bool isPlayerReady)
    {
        if (slotIndex < 0 || slotIndex >= Player_Frames.Length) return;

        if (Player_Frames[slotIndex] != null)
        {
            Color targetColor = isPlayerReady ? readyColor : unreadyColor;
            Player_Frames[slotIndex].color = targetColor;
        }

        if (NetworkClient.localPlayer != null)
        {
            var myInfo = NetworkClient.localPlayer.GetComponent<UserInfoManager>();
            if (myInfo != null && myInfo.PlayerNum - 1 == slotIndex)
            {
                UpdateMyButtonState(isPlayerReady);
            }
        }
    }

    public void UpdateSlotText(int slotIndex, string nick, int rate)
    {
        if (slotIndex < 0) return;
        if (slotIndex < Player_Nicknames.Length && Player_Nicknames[slotIndex] != null)
        {
            Player_Nicknames[slotIndex].text = string.IsNullOrEmpty(nick) ? "Connecting..." : nick;
        }
        if (slotIndex < Player_Rates.Length && Player_Rates[slotIndex] != null)
        {
            Player_Rates[slotIndex].text = (rate == -1) ? "" : $"{rate} RP";
        }
    }

    // [수정 핵심] Lobby_UI_Controller는 씬에 하나만 존재하므로,
    // 여기서 버튼을 누르면 '내 소유의 UserInfoManager'를 찾아 명령을 내려야 권한 오류가 안 납니다.
    public void Lobby_Ready()
    {
        // NetworkClient.localPlayer는 항상 이 클라이언트가 주인인 객체를 가리킵니다.
        if (NetworkClient.localPlayer != null)
        {
            var info = NetworkClient.localPlayer.GetComponent<UserInfoManager>();
            if (info != null)
            {
                Debug.Log("[UI] 내 캐릭터의 ToggleReady 호출");
                info.ToggleReady();
            }
        }
        else
        {
            Debug.LogWarning("[UI] 로컬 플레이어 객체를 찾을 수 없습니다.");
        }
    }

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
        if (NetworkClient.active)
        {
            if (NetworkServer.active) NetworkManager.singleton.StopHost();
            else NetworkManager.singleton.StopClient();
        }
        SceneManager.LoadScene("Main_Title!");
    }

    public void StartGameSequence()
    {
        if (Ready_btn) Ready_btn.interactable = false;
        if (Quit_btn) Quit_btn.interactable = false;
        if (Notice_text) Notice_text.gameObject.SetActive(true);
        if (Count_text) Count_text.gameObject.SetActive(true);
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        //countdownCoroutine = StartCoroutine(C_StartCountdown());
    }

    public void CancelGameSequence()
    {
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        if (Notice_text) Notice_text.gameObject.SetActive(false);
        if (Count_text) Count_text.gameObject.SetActive(false);
        if (Ready_btn) Ready_btn.interactable = true;
        if (Quit_btn) Quit_btn.interactable = true;
    }

    //IEnumerator C_StartCountdown()
    //{
    //    if (Notice_text) Notice_text.text = "게임이 곧 시작됩니다.";
    //    int count = 3;
    //    while (count > 0)
    //    {
    //        if (Count_text) Count_text.text = count.ToString();
    //        yield return new WaitForSeconds(1.0f);
    //        count--;
    //    }
    //    if (Count_text) Count_text.text = "START!";
    //    if (NetworkServer.active)
    //    {
    //        yield return new WaitForSeconds(0.5f);
    //        NetworkManager.singleton.ServerChangeScene("Main_InGame!");
    //    }
    //}
}