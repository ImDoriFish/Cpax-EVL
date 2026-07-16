using UnityEditor;
using UnityEngine;

public class BackwardBlockerSetup
{
    // Creates backward blockers for every normal PathNode in the scene.
    [MenuItem("Tools/Add Backward Blockers To Normal Nodes")]
    public static void AddBackwardBlockers()
    {
        // Find every PathNode in the scene.
        PathNode[] allNodes =
            Object.FindObjectsOfType<PathNode>(true);

        int blockersCreated = 0;

        for (int i = 0; i < allNodes.Length; i++)
        {
            PathNode node = allNodes[i];

            // Skip nodes that do not have an ID.
            if (node.nodeID == "")
            {
                continue;
            }

            // Check what type of node this is.
            bool isBlueNode = node.nodeID.StartsWith("Blue");
            bool isRedNode = node.nodeID.StartsWith("Red");
            bool isStartNode = node.nodeID.StartsWith("Start");
            bool isResetNode = node.nodeID.StartsWith("Reset");

            // Only normal nodes should receive blockers.
            if (
                isBlueNode ||
                isRedNode ||
                isStartNode ||
                isResetNode
            )
            {
                continue;
            }

            // Skip this node if it already has a blocker assigned.
            if (node.backwardBlocker != null)
            {
                continue;
            }

            // Create a child object for the blocker.
            GameObject blockerObject =
                new GameObject("BackwardBlocker");

            blockerObject.transform.SetParent(node.transform);

            blockerObject.transform.localPosition = Vector3.zero;
            blockerObject.transform.localRotation = Quaternion.identity;
            blockerObject.transform.localScale = Vector3.one;

            // Add a solid Box Collider.
            BoxCollider blockerCollider =
                blockerObject.AddComponent<BoxCollider>();

            blockerCollider.isTrigger = false;

            // Copy the size of the normal node's trigger collider.
            BoxCollider triggerCollider =
                node.GetComponent<BoxCollider>();

            if (triggerCollider != null)
            {
                blockerCollider.center = triggerCollider.center;
                blockerCollider.size = triggerCollider.size;
            }

            // The blocker should start disabled.
            blockerCollider.enabled = false;

            // Automatically assign it to the PathNode script.
            node.backwardBlocker = blockerCollider;

            // Tell Unity that the scene object was changed.
            EditorUtility.SetDirty(node);

            blockersCreated++;
        }

        Debug.Log(
            "Backward blocker setup finished. " +
            blockersCreated +
            " blocker(s) were created."
        );
    }
}