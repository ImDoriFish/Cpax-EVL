using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance;

    // The player that will be teleported.
    public Transform player;

    // The location inside the start room.
    public Transform startRoomPoint;

    // The three maze entrance locations.
    public Transform[] entrancePoints;

    private void Awake()
    {
        Instance = this;
    }

    // Teleports the player back to the start room.
    public void TeleportPlayerToStartRoom()
    {
        if (startRoomPoint == null)
        {
            Debug.LogError(
                "Start Room Point is not assigned in PlayerRespawnManager."
            );

            return;
        }

        TeleportPlayer(startRoomPoint);

        Debug.Log(
            "Player teleported to start room: " +
            startRoomPoint.name
        );
    }

    // Teleports the player to a specific entrance.
    public void TeleportPlayerToEntrance(int entranceIndex)
    {
        if (entrancePoints == null || entrancePoints.Length == 0)
        {
            Debug.LogError(
                "No entrance points assigned in PlayerRespawnManager."
            );

            return;
        }

        // Make sure the requested entrance exists.
        if (entranceIndex < 0 || entranceIndex >= entrancePoints.Length)
        {
            Debug.LogError(
                "Invalid entrance index: " + entranceIndex
            );

            return;
        }

        Transform selectedEntrance = entrancePoints[entranceIndex];

        if (selectedEntrance == null)
        {
            Debug.LogError(
                "Entrance " + entranceIndex + " is not assigned."
            );

            return;
        }

        TeleportPlayer(selectedEntrance);

        Debug.Log(
            "Player teleported to entrance: " +
            selectedEntrance.name
        );
    }

    // Handles the actual player movement.
    private void TeleportPlayer(Transform destination)
    {
        if (player == null)
        {
            Debug.LogError(
                "Player is not assigned in PlayerRespawnManager."
            );

            return;
        }

        CharacterController controller =
            player.GetComponent<CharacterController>();

        // CharacterController must be disabled before changing position.
        if (controller != null)
        {
            controller.enabled = false;

            player.position = destination.position;
            player.rotation = destination.rotation;

            controller.enabled = true;
        }
        else
        {
            player.position = destination.position;
            player.rotation = destination.rotation;
        }
    }
}