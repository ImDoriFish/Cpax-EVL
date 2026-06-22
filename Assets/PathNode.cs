using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public string nodeID;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PathManager.Instance.EnterNode(nodeID);
        }
        else
        {
            return;
        }

    }

}
