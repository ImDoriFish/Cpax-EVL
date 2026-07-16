using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetNode : MonoBehaviour
{
    // How many seconds to wait before teleporting.
    public float teleportDelay = 5f;

    // Prevents the reset from starting multiple times
    // while the player is waiting.
    private bool isResetting = false;

    private void OnTriggerEnter(Collider other)
    {
        // Ignore anything that is not the player.
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Do not start another countdown if one is already running.
        if (isResetting)
        {
            return;
        }

        // The reset node can only be used after reaching Red.
        if (!PathManager.Instance.CanUseResetNode())
        {
            Debug.Log(
                "Reset node touched, but playthrough is not finished yet."
            );

            return;
        }

        // Start the five-second delay.
        StartCoroutine(TeleportAfterDelay());
    }

    // Waits before teleporting the player back to the start room.
    private IEnumerator TeleportAfterDelay()
    {
        isResetting = true;

        Debug.Log(
            "Reset node reached. Teleporting to the start room in " +
            teleportDelay + " seconds."
        );

        // Pause this function for the selected number of seconds.
        yield return new WaitForSeconds(teleportDelay);

        // Teleport the player after the delay.
        PlayerRespawnManager.Instance.TeleportPlayerToStartRoom();

        // Allow the next playthrough.
        PathManager.Instance.MarkResetUsed();

        isResetting = false;
    }
}