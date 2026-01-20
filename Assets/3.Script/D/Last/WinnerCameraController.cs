using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class WinnerCameraController : MonoBehaviour {
    private CinemachineOrbitalFollow _orbital;

    public float orbitSpeed = 10f; // 회전 속도

    private void Awake() {
        TryGetComponent(out _orbital);
    }

	private void OnEnable() {
		_orbital.HorizontalAxis.Value = orbitSpeed;
		SoundManager.instance.RpcPlaySFX("VictorySFX");
	}

	private void Update() {
		_orbital.HorizontalAxis.Value -= orbitSpeed * Time.deltaTime;
	}
}
