using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour {
    // 서버가 정해주는 순번 (모든 클라이언트에게 동기화)
    [SyncVar(hook = nameof(OnIndexChanged))]
    public int playernumber = -1;

    // 클라이언트에서 순번이 정해졌을 때 실행될 로직 (예: UI 업데이트 등)
        //Debug.Log($"내 플레이어 순번이 {newIndex}로 설정되었습니다.");

    // 서버에서 이 객체가 생성(Spawn)될 때 실행됨
    // GameManager에게 이 플레이어를 등록해달라고 요청
    // 근데 내 플레이어가 아니면 꺼지쇼
    public override void OnStartServer() {
        base.OnStartServer();
        if (GameManager.Instance != null) {
            GameManager.Instance.RegisterPlayer(this);
        }
    }
    void OnIndexChanged(int oldIndex, int newIndex) { }

	//-----------추락
	private void OnTriggerEnter(Collider other) {
		if(other.CompareTag("DeadZone")) {
            OnFall();
		}
	}

	public void OnFall() {
        if (isLocalPlayer) {
            Debug.Log("응애 나 떨어짐");
            CmdRequestFell();
        }
    }

    [Command]
    void CmdRequestFell() {
        GameManager.Instance.ProcessPlayerFell(playernumber);
    }
}
