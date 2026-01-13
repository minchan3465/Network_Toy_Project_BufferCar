using UnityEngine;
using Unity.Cinemachine;
using System;
using System.Collections;

public class Camera_manager : MonoBehaviour
{
    public static Camera_manager instance;
    private CinemachineCamera cam;
    private CinemachineImpulseSource impulseSource;
    private float shake = 0.12f;

    private void Awake()
    {
        if (instance == null) { instance = this; }
        cam = FindAnyObjectByType<CinemachineCamera>();
        TryGetComponent<CinemachineImpulseSource>(out impulseSource);
    }

    public void ShakeCamera()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithVelocity(Vector3.forward * shake);
        }
    }

    public void SetCamera(Transform playertransform)
    // playercontroller에서 실행 islocalPlayer
    {
        if (cam != null)
        {
            cam.Follow = playertransform;
            cam.LookAt = playertransform;
            cam.Lens.FieldOfView = 60;
        }
    }
}
