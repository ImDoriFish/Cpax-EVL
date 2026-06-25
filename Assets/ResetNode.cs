using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetNode : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (!PathManager.Instance.CanUseResetNode())
        {
            Debug.Log("Reset node touched, but playthrough is not finished yet.");
            return;
        }

        PlayerRespawnManager.Instance.TeleportPlayerToRandomEntrance();

        PathManager.Instance.MarkResetUsed();
    }
}