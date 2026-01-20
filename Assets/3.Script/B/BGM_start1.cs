using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGM_start1 : MonoBehaviour
{
    void Start()
    {
        AudioManager.instance.StopBGM();
        AudioManager.instance.PlayBGM("TitleBGM");
    }
}
