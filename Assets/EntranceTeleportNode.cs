using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntranceTeleportNode : MonoBehaviour
{
    // Entrance 1 uses index 0.
    // Entrance 2 uses index 1.
    // Entrance 3 uses index 2.
    public int entranceIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerRespawnManager.Instance.TeleportPlayerToEntrance(
            entranceIndex
        );
    }
}
