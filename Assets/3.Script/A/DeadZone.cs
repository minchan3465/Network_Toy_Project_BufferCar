using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;

        if (other.CompareTag("Player"))
        {
            PlayerRespawn respawnController = other.GetComponent<PlayerRespawn>();

            if (respawnController != null)
            {
                respawnController.OnFellInDeadZone();
            }
        }
    }
}
