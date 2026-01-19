
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using MySql.Data.MySqlClient;
public class ServerInfo
{
    //서버 아이피, 서버 포트, 테이블이름
    public string Server_IP { get; private set; }
    public string Server_Port { get; private set; }
    public string Server_ID { get; private set; }
    public string Server_PW { get; private set; }
    public string TableName { get; private set; }
    public ServerInfo(string _IP, string _Port, string _id, string _pw, string name)
    {
        Server_IP = _IP;
        Server_Port = _Port;
        Server_ID = _id;
        Server_PW = _pw;
        TableName = name;
    }
}
public class PlayerInfo
{
    // 유저 아이디, 비밀번호, 등수
    public string User_ID { get; private set; }
    public string User_Nic { get; private set; }
    public int User_Rate { get; private set; }
    public int PlayerNum { get; set; }
    public PlayerInfo(string _id, string _nic, int _rate)
    {
        User_ID = _id;
        User_Nic = _nic;
        User_Rate = _rate;
    }
}

public class DataManager : MonoBehaviour
{
    [SerializeField] private string path = string.Empty;
    private MySqlConnection connection;
    private MySqlDataReader reader;
    public PlayerInfo playerInfo { get; private set; }
    public static DataManager instance = null;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
#if UNITY_EDITOR            //에디터인 상황
        path = Application.dataPath + "/DataBase";
#elif UNITY_STANDALONE_WIN  //window로 빌드를 한 상황
        path = Application.dataPath + "/DataBase";
#endif
        string serverinfo = serverSet();
        try
        {
            if (serverinfo.Equals(string.Empty))
            {
                Debug.Log("SQL Server JsonError");
                return;
            }
            connection = new MySqlConnection(serverinfo);
            connection.Open();
            Debug.Log("SQL Server Open");
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }
    private string serverSet()
    {
        ///json 담은 폴더 있어?
        ///if (!FolderCheck()) //폴더가 있는지 없는지 판단
        ///{//폴더가 없다면
        ///    
        ///}
        ///else
        ///{
        ///    
        ///}
        ///json 있어?
        ///Server_IP = _IP;
        ///Server_Port = _Port;
        ///Server_ID = _id;
        ///Server_PW = _pw;
        ///TableName = name;

        if (!FolderCheck())
        {
            CreateFolder();
        }
        if (!JsonCheck())
        {
            CreateJson();
        }
        //json에 초기값 세팅
        string Jsonstring = File.ReadAllText(path + "/config.json");
        JsonData itemdata = JsonMapper.ToObject(Jsonstring);
        try
        {
            string ServerInfo = $"Server={itemdata[0]["Server_IP"]};" + $"Database={itemdata[0]["TableName"]};" + $"Uid={itemdata[0]["Server_ID"]};" + $"Pwd={itemdata[0]["Server_PW"]};" + $"Port={itemdata[0]["Server_Port"]};" + "Charset=utf8;";
            return ServerInfo;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
        return string.Empty;
    }
    private void CreateJson()
    {
        List<ServerInfo> item = new List<ServerInfo>();
        item.Add(new ServerInfo("127.0.0.1", "programming", "root", "1234", "3306"));
        // 자료구조로 되어 있는 것을 json으로 변경
        JsonData data = JsonMapper.ToJson(item);

        File.WriteAllText(path + "/config.json", data.ToString());
        Debug.Log("Create config");
    }
    private bool FolderCheck()
    {
        return Directory.Exists(path);
    }
    private void CreateFolder()
    {
        Directory.CreateDirectory(path);
    }
    private bool JsonCheck()
    {
        return File.Exists(path + "/config.json");
    }
    private bool Connection_Check(MySqlConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                return false;
            }
        }
        return true;
    }
    public bool Login(string _name, string _paasword)
    {
        try
        {
            if (!Connection_Check(connection))
            {
                return false;
            }
            string sqlCommand = string.Format(@"SELECT User_Name, User_Password, User_Nic, User_Rate FROM userinfo WHERE User_Name='{0}' AND User_Password = '{1}';", _name, _paasword);

            MySqlCommand command = new MySqlCommand(sqlCommand, connection);
            reader = command.ExecuteReader();
            if (reader.HasRows)
            {// 조회된 데이터가 있는지 확인
                while (reader.Read())
                {
                    string name = (reader.IsDBNull(0) ? string.Empty : reader["User_Name"].ToString());
                    string pwd = (reader.IsDBNull(1) ? string.Empty : reader["User_Password"].ToString());
                    string nic = (reader.IsDBNull(2) ? string.Empty : reader["User_Nic"].ToString());
                    int rate = (reader.IsDBNull(3) ? 0 : int.Parse(reader["User_Rate"].ToString()));
                    //string num = (reader.IsDBNull(2) ? string.Empty : reader["User_Rate"].ToString());
                    if (!name.Equals(string.Empty) || !pwd.Equals(string.Empty)/* || !num.Equals(string.Empty)*/)
                    {//데이터 정상
                        playerInfo = new PlayerInfo(name, nic, rate);
                        if (!reader.IsClosed) reader.Close();
                        return true;
                    }
                    else
                    {
                        if (!reader.IsClosed) reader.Close();
                        return false;
                    }
                }
            }
            if (!reader.IsClosed) reader.Close();
            return false;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            if (!reader.IsClosed) reader.Close();
            return false;
        }
    }
    public bool SignupIDCheck(string _name)
    {
        try
        {
            if (!Connection_Check(connection))
            {
                return false;
            }
            //SELECT User_Name FROM userinfo WHERE User_Name = 'lwj';
            string sqlCommand = string.Format(@"SELECT User_Name FROM userinfo WHERE User_Name='{0}';", _name);

            MySqlCommand command = new MySqlCommand(sqlCommand, connection);
            reader = command.ExecuteReader();
            if (reader.HasRows)
            {// 조회된 데이터가 있는지 확인
                while (reader.Read())
                {
                    string name = (reader.IsDBNull(0) ? string.Empty : reader["User_Name"].ToString());
                    //string num = (reader.IsDBNull(2) ? string.Empty : reader["User_Rate"].ToString());
                    if (!name.Equals(string.Empty))
                    {//데이터 정상
                        if (!reader.IsClosed) reader.Close();
                        return true;
                    }
                    else
                    {
                        if (!reader.IsClosed) reader.Close();
                        return false;
                    }
                }
            }
            if (!reader.IsClosed) reader.Close();
            return false;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            if (!reader.IsClosed) reader.Close();
            return false;
        }
    }
    public bool SignupNicCheck(string _nic)
    {
        try
        {
            if (!Connection_Check(connection))
            {
                return false;
            }
            //SELECT User_Name FROM userinfo WHERE User_Name = 'lwj';
            string sqlCommand = string.Format(@"SELECT User_Nic FROM userinfo WHERE User_Nic='{0}';", _nic);

            MySqlCommand command = new MySqlCommand(sqlCommand, connection);
            reader = command.ExecuteReader();
            if (reader.HasRows)
            {// 조회된 데이터가 있는지 확인
                while (reader.Read())
                {
                    string Nic = (reader.IsDBNull(0) ? string.Empty : reader["User_Nic"].ToString());
                    //string num = (reader.IsDBNull(2) ? string.Empty : reader["User_Rate"].ToString());
                    if (!Nic.Equals(string.Empty))
                    {//데이터 정상
                        if (!reader.IsClosed) reader.Close();
                        return true;
                    }
                    else
                    {
                        if (!reader.IsClosed) reader.Close();
                        return false;
                    }
                }
            }
            if (!reader.IsClosed) reader.Close();
            return false;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            if (!reader.IsClosed) reader.Close();
            return false;
        }
    }
    public bool Signup(string _name, string _nic, string _paasword)
    {
        try
        {
            if (!Connection_Check(connection))
            {
                Debug.Log("connection not open");
                return false;
            }
            string sqlCommand = string.Format(@"INSERT INTO `userdata`.`userinfo` (`User_Name`, `User_Nic`, `User_Password`, `User_Rate`) VALUES('{0}', '{1}', '{2}', '{3}');", _name, _nic, _paasword, 2000);
            MySqlCommand command = new MySqlCommand(sqlCommand, connection);
            if (command.ExecuteNonQuery() == 1)//데이터 업데이트 성공
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return false;
        }
    }
    public bool GetRate(string _name)
    {
        try
        {
            if (!Connection_Check(connection))
            {
                return false;
            }
            string sqlCommand = string.Format(@"SELECT User_Name, User_Nic, User_Rate FROM userinfo WHERE User_Name='{0}';", _name);

            MySqlCommand command = new MySqlCommand(sqlCommand, connection);
            reader = command.ExecuteReader();
            if (reader.HasRows)
            {// 조회된 데이터가 있는지 확인
                while (reader.Read())
                {
                    string name = (reader.IsDBNull(0) ? string.Empty : reader["User_Name"].ToString());
                    string Nic = (reader.IsDBNull(1) ? string.Empty : reader["User_Nic"].ToString());
                    int num = (reader.IsDBNull(2) ? -1 : int.Parse(reader["User_Rate"].ToString()));
                    if (!name.Equals(string.Empty) || !Nic.Equals(string.Empty) || !num.Equals(-1))
                    {//데이터 정상
                        playerInfo = new PlayerInfo(name, Nic, num);
                        if (!reader.IsClosed) reader.Close();
                        return true;
                    }
                    else
                    {
                        if (!reader.IsClosed) reader.Close();
                        return false;
                    }
                }
            }
            if (!reader.IsClosed) reader.Close();
            return false;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            if (!reader.IsClosed) reader.Close();
            return false;
        }
    }
    public bool SetRate(string _name, int _rate)
    {
        try
        {
            if (!Connection_Check(connection))
            {
                return false;
            }
            string sqlCommand = string.Format(@"UPDATE `userdata`.`userinfo` SET `User_Rate`='{0}' WHERE  `User_Name`='{1}';", _name, _rate);
            MySqlCommand command = new MySqlCommand(sqlCommand, connection);
            reader = command.ExecuteReader();
            if (command.ExecuteNonQuery() == 1)//데이터 업데이트 성공
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return false;
        }
    }
}
