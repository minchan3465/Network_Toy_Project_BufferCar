using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Something hit DeadZone: {other.name}");

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player Tag Detected!");

            PlayerRespawn respawnController = other.GetComponent<PlayerRespawn>();

            if (respawnController != null)
            {
                if (respawnController.isLocalPlayer)
                {
                    Debug.Log("My car fell! Requesting Respawn to Server...");
                    respawnController.CmdRequestRespawn();
                }
            }
            else
            {
                Debug.LogWarning("PlayerRespawn component NOT found on this object or its parents!");
            }
        }
    }
}
