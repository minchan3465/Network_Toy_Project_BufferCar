using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Mirror;
using UnityEngine.SceneManagement;


public enum BGM
{
    TitleBGM,
    MainGameBGM
}


[Serializable]
public class SoundData
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volum = 1f;
}

public class SoundManager : NetworkBehaviour
{
    public static SoundManager instance = null;


    [Header("오디오 클립 설정(BGM)")]
    public AudioClip TitleBGM;
    public AudioClip MainBGM;

    [Header("오디오 소스 설정")]
    public AudioSource bgmSource;
    public AudioSource[] sfxSources;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded+=OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "Main_Title!" || scene.name == "Room!")
        {
            PlayBGM(BGM.TitleBGM);
        }
        else if(scene.name == "Main_InGame!")
        {
            PlayBGM(BGM.MainGameBGM);
        }
    }

    // 모든 클라이언트에서 소리가 나오게 하는 곳
    [ClientRpc]
    public void RpcPlaySFX(string clipName)
    {
        PlaySFXInternal(clipName);
    }

    // 특정 위치에서 소리가 나게 하는 곳
    [ClientRpc]
    public void PlaySFXPoint(string clipName, Vector3 position, float volum,float volumeMultiplier)
    {
        SoundData data = sfxClips.Find(x => x.name == clipName);
        if(data != null)
        {
            sfxSources[0].PlayOneShot(data.clip,data.volum* volumeMultiplier);
        }
    }
    //실제 재생하는 곳
    private void PlaySFXInternal(string clipName)
    {
        SoundData data = sfxClips.Find(x => x.name == clipName);
        if (data != null)
        {

            sfxSources[0].PlayOneShot(data.clip,data.volum);
        }
    }

    // BGM 재생 함수
    public void PlayBGM(BGM type)
    {
        AudioClip target = null;

        switch (type)
        {
            case BGM.TitleBGM:
                target = TitleBGM;
                break;
            case BGM.MainGameBGM:
                target = MainBGM;
                break;
        }

        bgmSource.Stop();
        bgmSource.clip = target;
        bgmSource.loop = true;
        bgmSource.Play();
    }

}



