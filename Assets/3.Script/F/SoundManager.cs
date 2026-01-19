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



    [Header("오디오 클립 설정(여기에 넣어도 자동 재생 안 됨)")]
    public AudioClip TitleBGM;
    public AudioClip MainBGM;

    [Header("오디오 소스 설정(실제 재생에 사용되는 곳)")]
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
        //StopAllCoroutines();

        if (scene.name == "Main_Title!" || scene.name == "Main_Room")
        {
            PlayBGM(BGM.TitleBGM);
        }
        else if(scene.name == "Main_InGame!")
        {
            //PlayBGM(BGM.MainGameBGM);
            StartCoroutine(StartGameSoundSequence());
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

    // 특정 효과음을 루프(반복)로 재생할지 선택하는 기능
    public void PlayLoopSFX(string clipName, int sourceIndex = 0)
    {
        SoundData data = sfxClips.Find(x => x.name == clipName);
        if (data != null && sourceIndex < sfxSources.Length)
        {
            sfxSources[sourceIndex].clip = data.clip;
            sfxSources[sourceIndex].loop = true;
            sfxSources[sourceIndex].volume = data.volum;
            sfxSources[sourceIndex].Play();
        }
    }

    //실제 재생하는 곳
    private void PlaySFXInternal(string clipName)
    {
        SoundData data = sfxClips.Find(x => x.name == clipName);
        if (data != null&&sfxSources.Length > 1)
        {

            sfxSources[1].PlayOneShot(data.clip,data.volum);
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
        if (bgmSource.clip == target && bgmSource.isPlaying) return;

        bgmSource.Stop();
        bgmSource.clip = target;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    IEnumerator StartGameSoundSequence()
    {


        // 1. 메인 BGM 시작
        PlayBGM(BGM.MainGameBGM);
        Debug.Log("브금 시작");

        // 2. 잠시 후 시동 소리 반복 재생
        yield return new WaitForSeconds(1.0f);
        PlayLoopSFX("Engine_IdleSFX", 0);
        Debug.Log("부릉 소리 시작");


        // 3. 경적 소리 재생
        yield return new WaitForSeconds(0.5f);

        if (isServer)
        {
            RpcPlaySFX("GameStartSFX");
        }
        else
        {
            // 서버가 없는 로컬 테스트 환경이면 강제로 재생
            PlaySFXInternal("GameStartSFX");
        }
        Debug.Log("경적 소리 로직 실행");

    }

}



