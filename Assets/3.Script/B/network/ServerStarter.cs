using UnityEngine;
using Mirror;

public enum Type1
{
    Server = 0,
    Client = 1
}

public class ServerStarter : MonoBehaviour
{
    [SerializeField] public Type1 ty;

    private NetworkManager manager;

    private void Start()
    {
        manager = NetworkManager.singleton;

        if (ty.Equals(Type1.Server))
        {
            StartServer();
        }
        else
        {
            StartClient();
        }
    }

    private void StartServer()
    {
        if(Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("can't Start server on webSL");
        }
        else
        {
            manager.StartServer();
            Debug.Log($"{manager.networkAddress} : StartServer");
        }
    }

    private void StartClient()
    {
        manager.StartClient();
        Debug.Log($"{manager.networkAddress} : StartClient");
    }

    private void OnApplicationQuit()
    {
        if (NetworkClient.isConnected)
        {
            manager.StopClient();
        }
        if (NetworkServer.active)
        {
            manager.StopServer();
        }
    }
}
