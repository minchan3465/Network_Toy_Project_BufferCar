using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class PlayerRespawn : NetworkBehaviour
{
    private Rigidbody rb;

    [SyncVar(hook = nameof(OnRespawnStateChanged))]
    public bool isRespawning = false;

    [SyncVar(hook = nameof(OnKinematicChanged))]
    public bool isKinematicSynced = false;

    [SyncVar(hook = nameof(OnCanRespawnChanged))]
    public bool canRespawn = true;

    private GameObject respawn_ob;
    [SerializeField] private GameObject car;
    //[SerializeField] private NetworkPlayer nplayer;

    public int playerNumber = -1;//값 외부에서 받아주세요

    public override void OnStartLocalPlayer()
    {
        transform.TryGetComponent(out rb); //player한테 넣어주세요

        //playerNumber = nplayer.playerNumber - 1; //NetworkPlayer 안쓰면 제외

        List<Transform> startPositions = NetworkManager.startPositions;

        // 이름순 정렬 (순서 꼬임 방지) 0123
        startPositions.Sort((a, b) => string.Compare(a.name, b.name));

        // 부여받은 번호가 있고, 리스트 범위 내에 있다면 해당 위치 사용
        if (playerNumber != -1 && playerNumber < startPositions.Count)
        {
            Transform targetPos = startPositions[playerNumber];
            transform.position = targetPos.position;
            transform.rotation = targetPos.rotation;
        }

        var respawnList = FindAnyObjectByType<RespawnList>();
        if (respawnList != null && playerNumber < respawnList.spawnList.Count)
        {
            respawn_ob = respawnList.spawnList[playerNumber];
        }
    }

    #region Respawn Logic

    // 변수 값이 변할 때마다 모든 클라이언트에서 실행
    //[SyncVar(hook = nameof(OnRespawnStateChanged))] public bool isRespawning = false;
    private void OnRespawnStateChanged(bool oldVal, bool newVal)
    {
        if (car == null) return;
        if (newVal == true) // 리스폰 시작
        {
            car.SetActive(false);
        }
        else // 리스폰 위치 이동 완료/ 깜빡임 시작
        {
            StartCoroutine(BlinkVisuals());
        }
    }

    private void OnKinematicChanged(bool oldVal, bool newVal)//SyncVar
    {
        if (rb == null) transform.TryGetComponent(out rb);
        if (rb != null)
        {
            rb.isKinematic = newVal;
        }
    }

    [Command]
    public void CmdSetKinematic(bool state) => isKinematicSynced = state;//SyncVar

    [Server]
    private void ResetRespawnFlag() => isRespawning = false;//SyncVar

    //

    private IEnumerator BlinkVisuals()//깜빡깜빡
    {
        float duration = 2.0f;
        float interval = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            car.SetActive(!car.activeSelf);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        car.SetActive(true);

        if (isLocalPlayer)
        {
            // 깜빡임이 끝난 시점에 모든 클라이언트의 물리 엔진을 켬
            CmdSetKinematic(false);

            // 물리 해제 직후 속도 초기화
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    [Command]
    public void CmdRequestRespawn()
    {
        if (isRespawning) return;

        if (!canRespawn)
        {
            Debug.Log($"{name}탈락");
            return;
        }

        isRespawning = true;
        isKinematicSynced = true; // 서버에서 물리 고정 시작

        TargetRpcRespawn(connectionToClient);
        Invoke(nameof(ResetRespawnFlag), 3.0f);
    }

    private Coroutine respawnRoutine;

    [TargetRpc]
    void TargetRpcRespawn(NetworkConnection target)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        if (respawnRoutine != null) StopCoroutine(respawnRoutine);
        respawnRoutine = StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        // 1초 대기 (추락한 곳에서 잠시 멈춤), 파티클 시간에 따라 시간 바꿔야됌
        yield return new WaitForSeconds(1f);
        // 리스폰 위치로 이동
        transform.position = respawn_ob.transform.position;
        transform.rotation = respawn_ob.transform.rotation;

        CmdRequestAppearEffect(transform.position);
        //공중에서 잠시 대기 여기서도 부활 파티클 같은거 있으면 좋을 것 같습니다.(공중에 그냥 가만히 있음)

        respawnRoutine = null;
    }

    private void OnCanRespawnChanged(bool oldVal, bool newVal)
    {
        // 만약 canRespawn이 false가 되었다면 (탈락 확정)
        if (newVal == false)
        {
            // 1. 물리 연산 중지
            if (rb != null) rb.isKinematic = true;

            // 2. 충돌체 끄기 (Deadzone 재감지 방지 및 다른 플레이어와 충돌 방지)
            if (TryGetComponent(out MeshCollider col)) col.enabled = false;

            // 3. 시각적 제거 (선택 사항: 완전히 없애거나 투명하게 처리)
            if (car != null) car.SetActive(false);

            Debug.Log($"{gameObject.name} 플레이어가 최종 탈락하여 모든 기능을 정지합니다.");
        }
    }

    #endregion

    #region Respawn Particle
    [SerializeField] private GameObject appearParticlePrefab;

    [Command]
    public void CmdRequestAppearEffect(Vector3 pos)
    {
        RpcPlayAppearEffect(pos); // 서버가 모든 클라이언트에게 실행 명령을 내림
    }

    [ClientRpc]
    void RpcPlayAppearEffect(Vector3 pos)
    {
        // 모든 플레이어의 화면에서 실행됨
        if (appearParticlePrefab != null)
        {
            // 지정된 위치에 파티클 생성
            GameObject effect = Instantiate(appearParticlePrefab, pos, Quaternion.identity);

            // 여러 개의 자식 파티클 시스템 중 가장 긴 시간을 찾음
            ParticleSystem[] allParticles = effect.GetComponentsInChildren<ParticleSystem>();

            float maxLifeTime = 0f;

            foreach (ParticleSystem ps in allParticles)
            {
                var main = ps.main;
                // 지속 시간 + 생존 시간 계산
                float currentLifeTime = main.duration + main.startLifetime.constantMax;
                if (currentLifeTime > maxLifeTime)
                {
                    maxLifeTime = currentLifeTime;
                }
            }

            // 가장 긴 파티클이 끝나는 시점에 부모 오브젝트 통째로 삭제
            // 계산된 시간이 없으면 기본 3초 후 삭제
            Destroy(effect, maxLifeTime > 0 ? maxLifeTime : 3.0f);
        }
    }
    #endregion

    private void OnDisable()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
        // 리스폰 도중 오브젝트가 꺼지면 상태를 초기화
        isRespawning = false;
    }
}