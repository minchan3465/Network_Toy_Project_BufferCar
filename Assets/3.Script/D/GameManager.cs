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

    public List<PlayerUI> playerUIs = new List<PlayerUI>();
    public readonly SyncList<int> playersHp = new SyncList<int>();
    private List<PlayerManager> _connectedPlayers = new List<PlayerManager>();

    private void Awake() {
		if(Instance == null) { Instance = this; } 
        else { Destroy(Instance); }
	}

	private void Start() {
        SetupGame();
	}

    public override void OnStartClient() {
        base.OnStartClient();
        playersHp.OnChange += OnHpListChanged; // SyncList 값이 변할 때마다 실행될 함수 등록 (최신 Mirror 방식)
        RefreshAllHpUI();
    }

    //게임이 시작 전, 설정 및 초기화.
    //1. 체력 초기화
    //2. 플레이어 데이터 초기화
    [Server]
    public void SetupGame() {
        // 1. 기존 데이터 초기화
        playersHp.Clear();
        _connectedPlayers.Clear();

        // 2. 현재 씬에 있는 모든 PlayerIdentity 객체를 찾음
        // (실제 서비스에서는 NetworkManager에서 생성될 때 리스트에 추가하는 방식이 더 정확합니다)
        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag("Player");

        for (int i = 0; i < playerObjs.Length; i++) {
            if(playerObjs[i].TryGetComponent(out PlayerManager manager)) {
                manager.playernumber = i; // 순번 배정
                _connectedPlayers.Add(manager);
                playersHp.Add(3); // 초기 HP 설정
			}
        }

        Debug.Log($"게임 셋업 완료: {_connectedPlayers.Count}명의 플레이어 준비됨.");
    }

    [Server]
    public void RegisterPlayer(PlayerManager player) {
        // 1. 이미 등록된 플레이어인지 확인
        if (_connectedPlayers.Contains(player)) return;

        // 2. 리스트에 추가 (들어온 순서대로 순번이 결정됨)
        _connectedPlayers.Add(player);

        // 3. 플레이어 객체에 순번 할당 (SyncVar를 통해 클라이언트로 전달됨)
        int newIndex = _connectedPlayers.Count - 1;
        player.playernumber = newIndex;

        // 4. 해당 플레이어의 초기 HP 생성 (3으로 설정)
        playersHp.Add(3);

        Debug.Log($"플레이어 등록 완료: Index {newIndex}, 현재 총원: {_connectedPlayers.Count}");
    }

    [Server]
    public void ProcessPlayerFell(int playerNum) {
        if (playerNum < 0 || playerNum >= playersHp.Count) return;

        playersHp[playerNum] -= 1;
        if (playersHp[playerNum] > 0) {
            //리스폰
        } else {
            //게임 오버~
        }
    }

    private void OnHpListChanged(SyncList<int>.Operation op, int playernumber, int newItem) {
        UpdateSinglePlayerUI(playernumber, newItem);
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
        Debug.Log(currentHp);
        if (playernumber >= playerUIs.Count) return;
        for (int i = 0; i < playerUIs[playernumber].hpButtons.Length; i++) {
            playerUIs[playernumber].hpButtons[i].interactable = (i < currentHp);
        }
    }
}
