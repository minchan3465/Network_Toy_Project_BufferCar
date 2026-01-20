

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using kcp2k;
using LitJson;
using System.IO;

public enum Type
{
    Empty = 0,
    Server,
    Client
}
public class Item
{
    public string License { get; private set; }
    public string ServerIP { get; private set; }
    public string Port { get; private set; }
    public Item(string L_index, string _ip, string _port)
    {
        License = L_index;
        ServerIP = _ip;
        Port = _port;
    }
}
public class Serverchecker : MonoBehaviour
{
    [SerializeField] public Type type;
    [SerializeField] private NetworkManager manager;
    [SerializeField]private KcpTransport transport;
    public string ServerIP { get; private set; }
    public string ServerPort { get; private set; }
    private string Path = string.Empty;
    private void Awake()
    {
        if (Path.Equals(string.Empty))
        {
            Path = Application.dataPath + "/License";
        }
        if (!Directory.Exists(Path))
        {
            Directory.CreateDirectory(Path);
        }
        if (!File.Exists(Path + "/License.json"))
        {
            Default_data(Path);
        }
        Path = Path + "/License.json";
        manager = NetworkManager.singleton;
        transport = (KcpTransport)manager.transport;
    }
    private void Default_data(string path)
    {
        List<Item> items = new List<Item>();
        items.Add(new Item("0", "127.0.0.1", "7777"));

        JsonData data = JsonMapper.ToJson(items);
        File.WriteAllText(path + "/License.json", data.ToString());
    }
    private Type License_type(string path)
    {
        Type type = Type.Empty;
        ///public string License { get; private set; }
        ///public string ServerIP { get; private set; }
        ///public string Port { get; private set; }
        ///
        try
        {
            string jsonString = File.ReadAllText(path);
            JsonData itemdata = JsonMapper.ToObject(jsonString);

            string string_type = itemdata[0]["License"].ToString();
            string string_serverIP = itemdata[0]["ServerIP"].ToString();
            string string_port = itemdata[0]["Port"].ToString();

            ServerIP = string_serverIP;
            ServerPort = string_port;
            type = (Type)Enum.Parse(typeof(Type), string_type);

            manager.networkAddress = ServerIP;
            transport.port = ushort.Parse(ServerPort);

            return type;
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
            return Type.Empty;
        }
}
    private void Start()
    {

        type = License_type(Path);
        //manager = NetworkManager.singleton;
        //type별로 행동을 넣기
        if (type.Equals(Type.Server))
        {
            Start_Server();
        }
        //else
        //{
        //    Start_Client();
        //}
    }
    private void Start_Server()
    {
        if(Application.platform == RuntimePlatform.WebGLPlayer)
        {
            //Debug.Log("cannot webGL Server");
        }
        else
        {
            NetworkServer.OnConnectedEvent += (NetworkConnectionToClient) =>
            {
                //Debug.Log($"New Client connect : {NetworkConnectionToClient.address }");
            };
            NetworkServer.OnDisconnectedEvent +=(NetworkConnectionToClient) =>
            {
                //Debug.Log($"Client Disconnect : {NetworkConnectionToClient.address }");
            };

            manager.StartServer();
            //Debug.Log($"{manager.networkAddress} : start Server...");
        }
    }
    public void Start_Client()
    {
        manager.StartClient();
        //Debug.Log($"{manager.networkAddress} : start client...");
    }
    private void OnApplicationQuit()
    {//프로그램이 꺼졌을 때
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