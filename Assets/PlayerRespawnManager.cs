using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance;

    public Transform player;
    public Transform[] entrancePoints;

    private void Awake()
    {
        Instance = this;
    }

    public void TeleportPlayerToRandomEntrance()
    {
        if (player == null)
        {
            Debug.LogError("Player is not assigned in PlayerRespawnManager.");
            return;
        }

        if (entrancePoints == null || entrancePoints.Length == 0)
        {
            Debug.LogError("No entrance points assigned in PlayerRespawnManager.");
            return;
        }

        int randomIndex = Random.Range(0, entrancePoints.Length);
        Transform chosenEntrance = entrancePoints[randomIndex];

        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
            player.position = chosenEntrance.position;
            player.rotation = chosenEntrance.rotation;
            controller.enabled = true;
        }
        else
        {
            player.position = chosenEntrance.position;
            player.rotation = chosenEntrance.rotation;
        }

        Debug.Log("Player teleported to: " + chosenEntrance.name);
    }
}
