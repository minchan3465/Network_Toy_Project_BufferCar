using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inputsystem : MonoBehaviour
{
    public Vector2 move_input { get; private set; }
    public Action ESCEvent;
    private bool canmove = true;
    private float canmovetime = 0.3f;

    private @Player_Input _controls;

    private void Awake()
    {
        // 인스턴스화
        _controls = new @Player_Input();

        // 버튼 액션 구독 (performed 시점에 이벤트 발생)
        _controls.Player.ESC.performed += ctx => ESCEvent?.Invoke();
    }

    private void Update()
    {
        if (!canmove) {
            move_input = Vector2.zero;
            return;
        }
            
        // 이동 값 매 프레임 읽기 (Polling 방식)
        move_input = _controls.Player.Move.ReadValue<Vector2>();
    }

    public void Enter()
    {
        StartCoroutine(cantmove());
    }

    private IEnumerator cantmove()
    {
        canmove = false;
        yield return new WaitForSeconds(canmovetime);
        canmove = true;
    }

    // 매우 중요: 입력을 활성화/비활성화 해줘야 합니다.
    private void OnEnable() => _controls.Enable();
    private void OnDisable() => _controls.Disable();
}
