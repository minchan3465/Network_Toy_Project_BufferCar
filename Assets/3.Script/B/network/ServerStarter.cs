using UnityEngine;
using Mirror;

public enum Type
{
    Server = 0,
    Client = 1
}

public class ServerStarter : MonoBehaviour
{
    [SerializeField] public Type ty;

    private NetworkManager manager;

    private void Start()
    {
        manager = NetworkManager.singleton;

        if (ty.Equals(Type.Server))
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
