using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGM_start : MonoBehaviour
{
    void Start()
    {
        AudioManager.instance.PlayBGM("TitleBGM");
    }
}
