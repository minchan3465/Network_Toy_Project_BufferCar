using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour {
    // 서버가 정해주는 순번 (모든 클라이언트에게 동기화)
    public int playerIndex = -1;
    public string playerNickname;
    public int playerRating = 2000;


    public MeshRenderer meshRenderer;
    public NetworkPlayer networkPlayer;

	private void Awake() {
        TryGetComponent(out networkPlayer);
    }

    private void Start()
    {
        // [1단계] 무조건 데이터를 먼저 옮겨 담습니다. (로비여도 수행)
        // UserInfoManager에 있는 소중한 데이터를 PlayerManager로 복사하는 과정입니다.
        UserInfoManager myInfo = GameObject.FindAnyObjectByType<UserInfoManager>();
        if (myInfo != null)
        {
            // 서버 번호(1~4)를 인덱스(0~3)로 변환하여 저장
            playerIndex = myInfo.PlayerNum - 1;
            playerNickname = myInfo.PlayerNickname;
        }

        // [2단계] 데이터 복사가 끝난 후에만 로비인지 체크해서 중단합니다.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "A_Room")
        {
            return;
        }

        // [3단계] 여기는 인게임 씬에서만 실행되는 로직입니다.
        setCarBodyColor(playerIndex);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this, networkPlayer);
        }
    }

    private void setCarBodyColor(int index) {
        Color color;
        switch(index) {
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
        meshRenderer.materials[0].color = color;
    }

	//-----------추락
	private void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Deadzone")) {
            if (isLocalPlayer) {
                CmdRequestFell();
            }
		}
	}

    [Command]
    void CmdRequestFell() {
        GameManager.Instance.ProcessPlayerFell(playerIndex);
    }
}
