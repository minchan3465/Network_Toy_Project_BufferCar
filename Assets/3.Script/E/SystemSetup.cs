using UnityEngine;

public class SystemSetup : MonoBehaviour
{
    void Awake()
    {
        // Windows Server나 Standalone 빌드에서 실행될 때
        // 콘솔 출력 인코딩을 UTF-8로 변경합니다.
#if UNITY_STANDALONE_WIN || UNITY_SERVER
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
#endif
    }
}