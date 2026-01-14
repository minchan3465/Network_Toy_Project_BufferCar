using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SoundData
{
    public string name;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance = null;

    

    [Header("오디오 소스 설정")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("효과음 클립 리스트")]
    public List<SoundData> sfxClips;

    void Awake()
    {
        if (instance == null) 
        {
            instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else 
        {
            Destroy(gameObject); 
        }
    }

    // 효과음 재생 함수
    public void PlaySFX(string clipName)
    {
        SoundData data = sfxClips.Find(x => x.name == clipName);
        if (data != null)
        {
           
            sfxSource.PlayOneShot(data.clip);
        }
    }

    public void PlaySFXPoint(string clipName, Vector3 position, float volum = 1.0f)
    {
        SoundData data = sfxClips.Find(x => x.name == clipName);
        if(data != null)
        {
            sfxSource.PlayOneShot(data.clip);
        }
    }

    // BGM 재생 함수
    public void PlayBGM(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

}



