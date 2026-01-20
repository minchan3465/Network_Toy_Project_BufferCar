using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem;

public class PlayerCollision : NetworkBehaviour
{
    private Rigidbody rb;
    private PlayerRespawn res;
    private Inputsystem input;

    [SerializeField] private float pushForce = 19; // 밀어내는 힘의 세기
    [SerializeField] private double pushCooldown = 0.2f; //밀림 쿨타임(중복 호출 방지)
    [SyncVar] private bool isPushing = false;//밀림 상황
    [SyncVar] private double lastPushTime; // 서버 시간 기록

    private void Start()
    {
        if (!isOwned) { return; }
        input = FindAnyObjectByType<Inputsystem>();
        transform.TryGetComponent(out rb);
        transform.TryGetComponent(out res);
    }

    #region 충돌로직1 OnCollisionEnter_Player
    /*
    private void OnCollisionEnter(Collision collision)
    {
        if (!isOwned) return;
        if (res != null && res.isRespawning) return;
        if (NetworkTime.time < lastPushTime + pushCooldown) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // 상대방의 Respawn 컴포넌트 가져오기
            if (collision.gameObject.TryGetComponent(out PlayerRespawn targetRes))
            {
                // 2. 부딪힌 상대방이 리스폰 중이면 충돌 무시
                if (targetRes.isRespawning) return;
            }
            if (isPushing) return;

            //닿은 점
            ContactPoint contact = collision.GetContact(0);
            Vector3 contactPoint = contact.point;
            Vector3 contactNormal = contact.normal; // 충돌 표면의 방향 (법선)

            NetworkIdentity targetIdentity = collision.gameObject.GetComponent<NetworkIdentity>();

            if (targetIdentity != null)
            {
                // 상대방과의 위치 차이 계산
                Vector3 dirToTarget = (collision.transform.position - transform.position);
                // Y축 값을 0으로 고정(위로 붕뜨거나 아래로 뚫고가려고 하는거 미연 방지)
                dirToTarget.y = 0;
                // 방향벡터에 힘
                Vector3 finalForce = dirToTarget.normalized * pushForce;
                // 서버에 계산된 수평 힘을 전달
                CmdPushBoth(netIdentity, targetIdentity, finalForce, contactPoint, contactNormal);
            }
        }
    }

    [Command]
    public void CmdPushBoth(NetworkIdentity self, NetworkIdentity target, Vector3 force, Vector3 contactPoint, Vector3 contactNormal)
    {
        // 서버측 점검
        if (this.res != null && this.res.isRespawning) return;

        if (target.TryGetComponent(out PlayerRespawn targetRes))
        {
            if (targetRes.isRespawning) return;
        }

        if (NetworkTime.time < lastPushTime + pushCooldown) return;
        lastPushTime = NetworkTime.time; // 현재 서버 시간 저장

        // 서버에서 두 플레이어의 상태를 모두 체크
        if (this.isPushing) return;

        if (target.TryGetComponent(out PlayerCollision targetCol))
        {
            if (targetCol.isPushing)
            {
                Debug.Log("Target is already being pushed. Ignore.");
                return;
            }
            this.isPushing = true;
            targetCol.isPushing = true;

            this.RpcApplyImpulse(-force); // 본인은 뒤로 (작용 반작용)
            targetCol.RpcApplyImpulse(force); // 상대는 앞으로

            RPCSoundandParticle(contactPoint, contactNormal); 

            // 일정 시간 후 서버에서 변수만 false로
            Invoke(nameof(ServerResetPushStatus), (float)pushCooldown);
            targetCol.Invoke(nameof(targetCol.ServerResetPushStatus), (float)pushCooldown);
        }
    }
    */

    //
    //충돌로직1_2
    private void OnCollisionEnter(Collision collision)
    {
        if (!isOwned || isPushing) return;
        if (res != null && res.isRespawning) return;
        if (NetworkTime.time < lastPushTime + pushCooldown) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent(out PlayerRespawn targetRes))
                if (targetRes.isRespawning) return;

            if (collision.gameObject.TryGetComponent(out NetworkIdentity targetIdentity))
            {
                // 방향 계산
                Vector3 dirToTarget = (collision.transform.position - transform.position);
                dirToTarget.y = 0;
                dirToTarget.Normalize();

                // 속도 보너스 (매우 미세하게 설정)
                // magnitude를 사용하여 속도 비례 선형 증가 유도
                float mySpeed = rb.linearVelocity.magnitude;
                float targetSpeed = collision.gameObject.GetComponent<Rigidbody>().linearVelocity.magnitude;

                // (내 속도 - 상대 속도)의 차이를 0~1 사이의 비율로 환산
                float speedDiff = Mathf.Max(0, mySpeed - targetSpeed);
                // 22로 나누어 0~1 사이 값으로 만든 뒤 0.2를 곱함 (최대 20% 보너스 제한)
                float attackerBonus = 1f + (speedDiff / 22f * 0.8f);

                // 최종 힘 계산
                // 기본 pushForce(19)에서 크게 벗어나지 않음 (최대 22~23 정도)
                Vector3 forceToTarget = dirToTarget * pushForce * attackerBonus;
                Vector3 forceToSelf = -dirToTarget * pushForce / attackerBonus;

                ContactPoint contact = collision.GetContact(0);
                PlayVibration(vpower, duration);

                ApplyImpulseLocal(forceToSelf);

                // 서버 전송
                CmdPushBoth(targetIdentity, forceToTarget, forceToSelf, contact.point, contact.normal);
            }
        }
    }
    private void ApplyImpulseLocal(Vector3 force)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force + Vector3.up * 3f, ForceMode.Impulse);
        if (input != null) input.Enter(); // 조작 차단
    }

    [Command]
    public void CmdPushBoth(NetworkIdentity target, Vector3 forceToTarget, Vector3 forceToSelf, Vector3 contactPoint, Vector3 contactNormal)
    {
        if (this.isPushing) return;
        if (target.TryGetComponent(out PlayerCollision targetCol))
        {
            if (targetCol.isPushing) return;

            this.isPushing = true;
            targetCol.isPushing = true;
            this.lastPushTime = NetworkTime.time;
            targetCol.lastPushTime = NetworkTime.time;

            targetCol.RpcApplyImpulse(forceToTarget);

            RPCSoundandParticle(contactPoint, contactNormal);

            Invoke(nameof(ServerResetPushStatus), (float)pushCooldown);
            targetCol.Invoke(nameof(targetCol.ServerResetPushStatus), (float)pushCooldown);
        }
    }
    //

    [Server]
    private void ServerResetPushStatus()
    {
        this.isPushing = false;
    }

    [TargetRpc]
    public void RpcApplyImpulse(Vector3 force)
    {
        PlayVibration(vpower, duration);//진동호출!
        // 각 플레이어의 화면에서 실행
        if (rb == null) rb = GetComponent<Rigidbody>();

        // 조작 일시 정지
        if (input != null) input.Enter();

        // 물리 적용
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force + Vector3.up * 3f, ForceMode.Impulse); // 살짝 띄워줌
    }
    #endregion

    #region Enter Particle
    [SerializeField] private GameObject collisionParticlePrefab; // 충돌 파티클 프리팹

    [ClientRpc]
    public void RPCSoundandParticle(Vector3 pos, Vector3 normal)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("Impact_MetalSFX");
        }
        if (collisionParticlePrefab != null)
        {

            // 위치 보정
            Vector3 spawnPos = pos + (normal * 0.2f) + (Vector3.up * 1.0f);

            // 파티클의 Z축(앞)을 충돌 반사각(normal)과 일치시킵니다.
            Quaternion rotation = Quaternion.LookRotation(normal);

            GameObject effect = Instantiate(collisionParticlePrefab, spawnPos, rotation);

            // 자식 파티클 수명 계산
            float maxLifeTime = 0f;
            ParticleSystem[] allParticles = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                var main = ps.main;
                float currentLifeTime = main.duration + main.startLifetime.constantMax;
                if (currentLifeTime > maxLifeTime) maxLifeTime = currentLifeTime;
            }

            Destroy(effect, maxLifeTime);
        }
    }

    #endregion

    #region 카메라, 패드 진동
    [Header("설정")]
    [SerializeField] private float vpower = 0.7f;  // 진동 강도
    [SerializeField] private float duration = 0.2f;  // 진동 지속 시간

    private Coroutine hapticCoroutine;

    public void PlayVibration(float intensity, float time)
    {
        if (Camera_manager.instance != null)
        {
            Camera_manager.instance.ShakeCamera();
        }

        var xboxGamepad = Gamepad.current;
        if (xboxGamepad == null) return;

        // 이미 진동 중이라면 멈추고 새로 시작
        if (hapticCoroutine != null) StopCoroutine(hapticCoroutine);
        hapticCoroutine = StartCoroutine(HapticRoutine(xboxGamepad, intensity, time));
    }

    private IEnumerator HapticRoutine(Gamepad gamepad, float intensity, float time)
    {
        if (gamepad == null) yield break;
        // 낮은 주파수(왼쪽, 큰 모터라서 진동이 큽니다. 폭발등)와
        // 높은 주파수(오른쪽, 작고 가벼운 모터라서 징~ 거리는 진동)에 강도 적용
        gamepad.SetMotorSpeeds(intensity * 0.8f, intensity);

        yield return new WaitForSeconds(time);

        // 진동 종료
        if (gamepad != null) gamepad.SetMotorSpeeds(0f, 0f);
        hapticCoroutine = null;
    }
    #endregion

    #region 충돌로직2 OnTriggerEnter_Deadzone & Respawn
    private void OnTriggerEnter(Collider other)
    {
        if (!isOwned) return;

        if (other.CompareTag("Deadzone"))
        {
            if (res != null && res.isRespawning) return;

            PlayVibration(vpower, duration);//진동호출!

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            CmdRpcDeadEffect(transform.position);//현재 죽은 위치에 폭발

            StartCoroutine(DelayedRespawnCheck());
        }
    }
    private IEnumerator DelayedRespawnCheck()
    {
        // 0.1초대기
        // 이 시간 동안 서버는 ProcessPlayerFell을 실행하고 
        // canRespawn = false 패킷을 클라이언트에 전송
        yield return new WaitForSeconds(0.1f);

        if (res != null)
        {
            // 서버에서 동기화된 최신 canRespawn 값을 확인
            if (res.canRespawn)
            {
                res.CmdRequestRespawn(); // 리스폰 요청
            }
            else
            {
                // 체력이 0이라서 canRespawn이 false로 변한 경우
                res.CmdSetRespawningTrue();

                // 내 화면에서 즉시 물리 정지 (다른 차를 밀지 않게)
                if (rb != null) rb.isKinematic = true;
            }
        }
    }
    #endregion

    #region Dead_Particle
    [SerializeField] private GameObject deadparticle; // 충돌 파티클 프리팹

    [Command]
    void CmdRpcDeadEffect(Vector3 pos)
    {
        RpcPlayDeadEffect(pos);
    }

    [ClientRpc]
    void RpcPlayDeadEffect(Vector3 pos)
    {
        if (deadparticle != null)
        {
            GameObject effect = Instantiate(deadparticle, pos, Quaternion.identity);

            AudioManager.instance.PlaySFX("Bomb Explosion");

            // 모든 자식 파티클 시스템을 가져옴
            ParticleSystem[] allParticles = effect.GetComponentsInChildren<ParticleSystem>();

            float maxLifeTime = 0f;

            foreach (ParticleSystem ps in allParticles)
            {
                var main = ps.main;
                // 각 파티클마다 (지속시간 + 생존시간)을 계산해서 가장 긴 시간을 찾음
                float currentLifeTime = main.duration + main.startLifetime.constantMax;
                if (currentLifeTime > maxLifeTime)
                {
                    maxLifeTime = currentLifeTime;
                }
            }
            // 가장 긴 파티클이 끝나는 시점에 부모 오브젝트 삭제
            Destroy(effect, maxLifeTime);
        }
    }
    #endregion

    private void OnDisable()
    {
        if (hapticCoroutine != null)
        {
            StopCoroutine(hapticCoroutine);
            hapticCoroutine = null;
        }
        // 오브젝트가 비활성화될 때 진동 강제 종료
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
        // 오브젝트가 꺼질 때 변수를 초기화하여 다음 활성화 때 버그 방지
        isPushing = false;
    }
}
