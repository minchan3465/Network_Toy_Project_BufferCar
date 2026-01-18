using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class D_server_plz_dont_del : MonoBehaviour {
    private NetworkManager manager;

    private void Start() {
        manager = NetworkManager.singleton;
        manager.StartServer();
        Debug.Log("서버 열림");
    }
}
