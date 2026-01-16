using System;
using System.Collections;
using UnityEngine;

public class Inputsystem : MonoBehaviour
{
    public Vector2 move_input { get; private set; }
    public Action ESCEvent;
    private bool canmove = true;
    private float canmovetime = 0.3f;

    private @Player_Input _controls;//input system c#

    private void Awake()
    {
        _controls = new @Player_Input();
        _controls.Player.ESC.performed += ctx => ESCEvent?.Invoke();
        // 버튼 액션 구독 (performed 시점에 이벤트 발생)
    }

    private void Update()
    {
        if (!canmove) {
            move_input = Vector2.zero;
            return;
        }
        move_input = _controls.Player.Move.ReadValue<Vector2>();
        // 이동 값 매 프레임 읽기
    }

    public void Enter()//충돌시 밀리면서(PlayerCollision) 잠깐 이동이 불가능해집니다
    {
        if (!canmove) return;
        StartCoroutine(cantmove());
    }

    private IEnumerator cantmove()
    {
        canmove = false;
        yield return new WaitForSeconds(canmovetime);
        canmove = true;
    }

    // 입력을 활성화/비활성화 해줘야 합니다.
    private void OnEnable() => _controls.Enable();
    private void OnDisable() => _controls.Disable();
}
