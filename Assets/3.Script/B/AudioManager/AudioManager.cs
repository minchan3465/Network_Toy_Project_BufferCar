using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]//데이터 직렬화
public class Sound
{
    public string name;
    public AudioClip clip;
}
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance = null;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("AudioManager instance가 이미 있습니다. Destroy");
        }
        AutoSetting();
    }

    [Space(10f)]//인스펙터 창에서 공간 띄우기
    [Header("Audio Clip")]
    [Space(10f)]
    [SerializeField] private Sound[] BGM;
    [SerializeField] private Sound[] SFX;

    [Space(50f)]
    [Header("Audio Source")]
    [Space(10f)]
    [SerializeField] private AudioSource BGMPlayer;
    [SerializeField] private AudioSource[] SFXPlayer;

    private void AutoSetting()
    {
        BGMPlayer = transform.GetChild(0).GetComponent<AudioSource>();
        SFXPlayer = transform.GetChild(1).GetComponents<AudioSource>();
    }

    public void PlayBGM(string name)
    {
        foreach (Sound s in BGM)
        {
            if (s.name.Equals(name))
            {
                BGMPlayer.clip = s.clip;
                BGMPlayer.Play();
                break;
            }
        }
    }
    public void StopBGM()
    {
        BGMPlayer.Stop();
    }

    public void PlaySFX(string name)
    {
        foreach (Sound s in SFX)
        {
            if (s.name.Equals(name))
            {//해당 SFX를 찾았다.
                for (int i = 0; i < SFXPlayer.Length; i++)
                {
                    if (!SFXPlayer[i].isPlaying)
                    {
                        SFXPlayer[i].clip = s.clip;
                        SFXPlayer[i].Play();
                        return; //Method에서 나가!
                    }
                }
                Debug.Log("모든 Audio Source가 Play 중입니다.");
                return;
            }
        }
        Debug.Log($"해당 name: [{name}] 을 key로 가진 SFX가 없습니다.");
    }
}
