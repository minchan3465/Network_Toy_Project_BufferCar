
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class RPCController : NetworkBehaviour
{
    [SerializeField] private TMP_Text chat_text;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private GameObject chat_Panel;

    private static event Action<string> onMessage;

    public override void OnStartAuthority()
    {
        //base.OnStartAuthority();
        if (isLocalPlayer)
        {
            chat_Panel.SetActive(true);
        }
        onMessage += NewMessage;
    }
    private void NewMessage(string message)
    {
        chat_text.text += message;
    }
    ///Mirror에서 RPC를 사용하고 싶다면 
    ///RPC 순서 : clientRPC 명령어 -> 명령어를 server에게 전달, Commnad(server) 명령어 할당 -> 실질적으로 
    ///
    [ClientCallback]
    private void OnDestroy()
    {
        if (!isLocalPlayer) return;
        onMessage -= NewMessage;
    }
    [Client]
    public void Send()
    {//실제 내 클라이언트에서 실행할 메서드
        if (!Input.GetKeyDown(KeyCode.Return)) return;
        if (string.IsNullOrWhiteSpace(input.text)) return;
        cmdSendMessage(input.text);
        input.text = string.Empty;
    }
    [Command]
    private void cmdSendMessage(string message)
    {
        //서버에서 다른 클라이언트에게 행동 전달
        RPCHandleMessage($"[{connectionToClient.connectionId}] : {message}");
    }
    [ClientRpc]
    private void RPCHandleMessage(string message)
    {
        onMessage?.Invoke($"\n{message}");
    }

}
