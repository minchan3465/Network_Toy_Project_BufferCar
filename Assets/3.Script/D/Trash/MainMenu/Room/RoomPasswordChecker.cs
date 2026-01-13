using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct AuthRequestMessage : NetworkMessage {
	public int password;
}

public struct AuthResponseMessage : NetworkMessage {
	public bool success;
}

public class RoomPasswordChecker : NetworkAuthenticator {
	//룸 매니저 참조...
	public RoomManager roomMnager;


	//------------------------------------------------------
	//[Server] 기준!


	//이게 뭐냐면..
	//클라이언트가 서버에 접속을 시도할때, OnServerAddPlayer인가? 하면서 플레이어 추가를 하는데
	//NetworkManager에서 Authenticator가 존재하면, 생명 주기를 따라가면서 이게 먼저 실행되는거같음.

	//어... 아닌가? 근데 NetworkAuthenticator의 부모는 MonoBehavior인거보니, 그냥 생명주기상 먼저 실행되는게 아니라,
	//이게 존재하면 메서드가 override된다거나 먼저 실행되는거같음.

	//정확히 OnServerAddPlayer 이게 호출되기 전에 서버가 먼저 하는거 맞는듯.

	public override void OnServerAuthenticate(NetworkConnectionToClient conn) {
		//클라이언트로부터 메시지 대기중...
		NetworkServer.ReplaceHandler<AuthRequestMessage>(OnAuthRequest, false);
	}

	//메시지 받고, 검사 후, 결과 출력하는 메서드
	private void OnAuthRequest(NetworkConnectionToClient conn, AuthRequestMessage msg) {
		//만약 접속한 사람이 Local, 즉 방장이라면 무조건 허용
		if(conn.connectionId.Equals(NetworkServer.localConnection?.connectionId)) {
			Debug.Log("호스트구나... 너는 넘어갈게.");
			conn.Send(new AuthResponseMessage { success = true });
			OnServerAuthenticated.Invoke(conn);
			return;
		}

		if(roomMnager.Compare_Password(msg.password)) {
			//만약 메시지를 받고, 비밀번호가 일치한다면

			//반환 메시지 true로 설정해주고~
			conn.Send(new AuthResponseMessage { success = true });

			//Client에게 메시지 발송.
			OnServerAuthenticated.Invoke(conn);
		}
	}



	//------------------------------------------------------
	//[Client] 기준!

	public override void OnClientAuthenticate() {
		//플레이어 개인 룸 매니저의 tempPassword를 가져오는듯.
		//메시지로 보내기위해 AuthRequestMessage의 password 멤버변수에 담아주기.
		AuthRequestMessage msg = new AuthRequestMessage { password = roomMnager.tempPassword };

		//네트워크 클라이언트 기준으로 서버에 메시지 보내기.
		//위의 OnServerAuthenticate가 수신하는거같음.
		NetworkClient.Send(msg);

		//그 TCP에서 비동기같은걸로 메시지 수신 대기하는건듯.
		NetworkClient.ReplaceHandler<AuthResponseMessage>(OnAuthResponse, false);
	}

	//메시지 보내기, 수신한 결과값 출력.
	private void OnAuthResponse(AuthResponseMessage msg) {
		if (msg.success) {
			Debug.Log("<color=green>접속 성공!</color>");
			OnClientAuthenticated.Invoke();
		} else {
			// 2번 버그 해결: 클라이언트 화면에 로그 출력
			Debug.LogError("<color=red>비밀번호가 틀렸습니다!</color>");

			// 연결이 꼬이지 않게 클라이언트 완전히 정지
			(NetworkManager.singleton as RoomManager).StopClient();
		}
	}
}
