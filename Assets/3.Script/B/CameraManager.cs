using UnityEngine;
using Unity.Cinemachine;
using System;
using System.Collections;

public class Camera_manager : MonoBehaviour
{
    public static Camera_manager instance;
    private CinemachineCamera cam;
    private CinemachineImpulseSource impulseSource;
    private float shake = 1.5f; //카메라 흔들림 강도

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

            cam.LookAt = null;

            // 현재 설정된 Offset 값을 고려하여 카메라가 즉시 위치해야 할 좌표를 계산
            Vector3 targetOffset = new Vector3(0, 42, -42);
            Vector3 immediatePos = playertransform.position + targetOffset;

            // 카메라를 해당 위치로 즉시 순간이동
            // Quaternion.Euler은 카메라가 아래를 내려다보는 고정 각도
            cam.ForceCameraPosition(immediatePos, Quaternion.Euler(52, 0, 0));

            cam.Lens.FieldOfView = 60;
        }
    }
}
