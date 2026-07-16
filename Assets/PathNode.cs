using System;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    // The ID assigned to this node in the Inspector.
    public string nodeID;

    // A separate solid collider placed across the corridor.
    // It starts disabled and turns on after the player passes this normal node.
    public Collider backwardBlocker;

    // Becomes true after the player enters this normal node's trigger.
    private bool playerEnteredNormalNode = false;

    // Runs when another collider enters this node's trigger.


    // Runs once when the scene starts.
    private void Start()
    {
        // Make sure the backward blocker starts turned off.
        if (backwardBlocker != null)
        {
            backwardBlocker.enabled = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // Ignore anything that is not the player.
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Check whether this is a Start node.
        bool isStartNode = nodeID.StartsWith(
            "Start",
            StringComparison.OrdinalIgnoreCase
        );

        // Check whether this is a Red exit node.
        bool isRedNode = nodeID.StartsWith(
            "Red",
            StringComparison.OrdinalIgnoreCase
        );

        bool isBlueNode = nodeID.StartsWith( "Blue", StringComparison.OrdinalIgnoreCase);

        // Start nodes are only used by the score system.
        // They are not sent to PathManager, so there will be no
        // "Start_1 -> P10_A" missing-path warning.
        if (isStartNode)
        {
            PlayerScoreManager.Instance.StartScore();
            return;
        }

        // Normal, Blue, and Red nodes are sent to PathManager.
        PathManager.Instance.EnterNode(nodeID);

        // Only normal nodes should activate a backward blocker.
        if (!isBlueNode && !isRedNode)
        {
            playerEnteredNormalNode = true;

            Debug.Log(
                nodeID + " is a normal node. Backward blocker is ready."
            );
        }
        

        // Red nodes also finish the score.
        if (isRedNode)
        {
            PlayerScoreManager.Instance.FinishScore();
        }
    }


    // Runs when the player completely leaves this node's trigger.
    private void OnTriggerExit(Collider other)
    {
        // Ignore anything that is not the player.
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Do nothing unless the player previously entered a normal node.
        if (!playerEnteredNormalNode)
        {
            return;
        }

        // Turn on the solid collider after the player has passed the node.
        if (backwardBlocker != null)
        {
            backwardBlocker.enabled = true;
        }
        else
        {
            Debug.LogWarning(
                nodeID + " has no Backward Blocker assigned."
            );
        }

        playerEnteredNormalNode = false;
    }

    public void ResetBackwardBlocker()
    {
        if (backwardBlocker != null)
        {
            backwardBlocker.enabled = false;
        }

        playerEnteredNormalNode = false;
    }
}