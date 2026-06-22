using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance;
    private string prevNode = "";
    private Dictionary<string, PathInfo> pathMap = new Dictionary<string, PathInfo>();
    private List<UsedPath> currentRoute = new List<UsedPath>();
    private List<PlaythroughData> allPlaythroughs = new List<PlaythroughData>();
    private float segmentStartTime = 0f; //storing time for the prevNode
    private int playthroughNumber = 1;
    private float totalWeight = 0f;
    private float totalTime = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        addPath("P1_A", "P1_B","001", 2f);
        addPath("P2_A", "P2_B", "002", 6f);
    }

    public void EnterNode(string nodeID)
    {
        Debug.Log("PathManager received node: " + nodeID);

        
        if (nodeID.StartsWith("Red")) //checking if the player reach the end to print the whole playthrough
        {
            FinishPlaythrough();
            return;
        }

        if(prevNode == "")
        {
            prevNode = nodeID;
            segmentStartTime = Time.time;
            Debug.Log("PathManager first node: " + prevNode + "Time start: " + segmentStartTime);
            return;

        }


        string pathKey = makePathKey(prevNode, nodeID);
        Debug.Log("PathManager path created: " + pathKey);

        if (pathMap.ContainsKey(pathKey))
        {
            PathInfo path = pathMap[pathKey];
            float timeTravel = Time.time - segmentStartTime;

            UsedPath storedPath = new UsedPath();

            storedPath.pathInfo = path;
            storedPath.timeTaken = timeTravel;


            currentRoute.Add(storedPath);

            totalWeight += path.weight;
            totalTime += timeTravel;

            Debug.Log(
                "RECORDED PATH: " + path.pathName +
                " | From: " + path.fromNode +
                " | To: " + path.toNode +
                " | Weight: " + path.weight +
                " | Time: " + timeTravel.ToString("F3") + " seconds"
            );

            Debug.Log(
                "CURRENT TOTAL" +
                " | Paths: " + currentRoute.Count +
                " | Total Weight: " + totalWeight +
                " | Total Time: " + totalTime.ToString("F2") + " seconds"
            );

            Debug.Log("Current route has " + currentRoute.Count + " path(s).");

            
        }
        else
        {
            Debug.LogWarning("NO PATH FOUND for key: " + pathKey);
        }

        prevNode = nodeID;
        segmentStartTime = Time.time;

        

        
    }

     private void PrintFullPlaythrough()
    {
        Debug.Log("========== PLAYTHROUGH FINISHED ==========");

        for (int i = 0; i < currentRoute.Count; i++)
        {
            UsedPath usedPath = currentRoute[i];
            PathInfo path = usedPath.pathInfo;

            Debug.Log(
                (i + 1) + ". " +
                path.pathName +
                " | " + path.fromNode + " -> " + path.toNode +
                " | Weight: " + path.weight +
                " | Time: " + usedPath.timeTaken.ToString("F2") + " seconds"
            );
        }

        Debug.Log(
            "FINAL TOTAL" +
            " | Paths Used: " + currentRoute.Count +
            " | Total Weight: " + totalWeight +
            " | Total Time: " + totalTime.ToString("F2") + " seconds"
        );
    }

    private void FinishPlaythrough()
    {
        if (currentRoute.Count == 0)
        {
            Debug.LogWarning("Red node reached, but no paths were recorded.");
            ResetCurrentPlaythrough();
            return;
        }

        PrintFullPlaythrough();

        PlaythroughData playthrough = new PlaythroughData
        {
            playthroughID = playthroughNumber,
            paths = new List<UsedPath>(currentRoute),
            finalTotalWeight = totalWeight,
            finalTotalTime = totalTime
        };

        allPlaythroughs.Add(playthrough);

        Debug.Log("Playthrough " + playthroughNumber + " saved in memory.");
        Debug.Log("Total saved playthroughs: " + allPlaythroughs.Count);

        playthroughNumber++;

        ResetCurrentPlaythrough();
    }


    private void ResetCurrentPlaythrough()
    {
        prevNode = "";
        segmentStartTime = 0f;

        currentRoute.Clear();

        totalWeight = 0f;
        totalTime = 0f;

        Debug.Log("Current playthrough reset. Ready for next replay.");
    }
    private void addPath(string fromNode, string toNode, string pathName, float weight)
    {
        string key = makePathKey(fromNode, toNode);

        PathInfo path = new PathInfo();

        path.fromNode = fromNode;
        path.toNode = toNode;
        path.pathName = pathName;
        path.weight = weight;

        pathMap[key] = path;


    }
    private string makePathKey(string fromNode, string toNode)
    {
        //return fromNode + "->" + toNode;
        return $"{fromNode} -> {toNode}";
    }

}

public class PathInfo
{
    public string fromNode;
    public string toNode;
    public string pathName;
    public float weight;
}

public class UsedPath
{
    public PathInfo pathInfo;
    public float timeTaken;

}

public class PlaythroughData
{
    public int playthroughID;
    public List<UsedPath> paths;
    public float finalTotalWeight;
    public float finalTotalTime;
}
