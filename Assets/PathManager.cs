using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.IO;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance;
    private string prevNode = "";
    private Dictionary<string, PathInfo> pathMap = new Dictionary<string, PathInfo>();
    private List<UsedPath> currentRoute = new List<UsedPath>();
    private List<DecisionData> currentDecisions = new List<DecisionData>();
    private List<PlaythroughData> allPlaythroughs = new List<PlaythroughData>();
    private float segmentStartTime = 0f; //storing time for the prevNode
    private int playthroughNumber = 1;
    private float totalWeight = 0f;
    private float totalTime = 0f;


    //decision timing variables
    private bool isDecisionTiming = false;
    private float decisionStartTime = 0f;
    private float totalDecisionTime = 0f;
    private string currentDecisionNode = "";

    private bool playthroughFinished = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {   //Id = RowPosition
        addPath("P10_A", "P10_B","1001", 2f);
        addPath("P30_A", "P20_B","2001", 6f);
        addPath("P40_A", "P30_B","3001", 2f);
        addPath("P50_A", "P40_B","4001", 6f);
        addPath("P20_A", "P50_B","5001", 6f);
        addPath("P50_A", "P60_B","6001", 5f);
        
        
        addPath("P10_C", "P10_D","1002", 1f);
        addPath("P41_C", "P20_D","2002", 3f);
        addPath("P31_C", "P31_D","3002", 3f);
        addPath("P32_C", "P32_D","4002", 5f);
        addPath("P50_C", "P41_D","5002", 6f);
        addPath("P42_C", "P42_D","6002", 5f);
        addPath("P20_C", "P50_D","7002", 6f);
        addPath("P60_C", "P60_D","8002", 3f);



        addPath("P10_E", "P10_F","1003", 8f);
        addPath("P50_E", "P20_F","2003", 4f);
        addPath("P31_E", "P31_F","3003", 3f);
        addPath("P41_E", "P32_F","4003", 7f);
        addPath("P42_E", "P41_F","5003", 7f);
        addPath("P20_E", "P42_F","6003", 7f);
        addPath("P32_E", "P50_F","7003", 3f);
        addPath("P60_E", "P60_F","8003", 9f);



        
    }

    public void EnterNode(string nodeID)
    {
        Debug.Log("PathManager received node: " + nodeID);

        // Blue node = decision-making area.
        // Start timing the decision.
        // Do NOT change prevNode.
        // Do NOT record a path.
        if (nodeID.StartsWith("Blue"))
        {
            isDecisionTiming = true;
            decisionStartTime = Time.time;
            currentDecisionNode = nodeID;

            Debug.Log("Decision node entered: " + currentDecisionNode);
            Debug.Log("Decision timer started.");

            return;
        }

        
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


        if (isDecisionTiming)
        {
            float decisionTime = Time.time - decisionStartTime;
            totalDecisionTime += decisionTime;

            DecisionData decisionData = new DecisionData();
            decisionData.decisionNodeID = currentDecisionNode;
            decisionData.decisionTime = decisionTime;

            currentDecisions.Add(decisionData);

            Debug.Log("Decision finished at node: " + currentDecisionNode);
            Debug.Log("Decision time: " + decisionTime.ToString("F3") + " seconds");

            isDecisionTiming = false;
            decisionStartTime = 0f;
            currentDecisionNode = "";

            prevNode = nodeID;
            segmentStartTime = Time.time;

            Debug.Log("New path start after decision: " + prevNode);

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

        //print each decision node time
        Debug.Log("========== DECISION TIMES ==========");

        for (int i = 0; i < currentDecisions.Count; i++)
        {
            DecisionData decision = currentDecisions[i];

            Debug.Log(
                (i + 1) + ". " +
                decision.decisionNodeID +
                " | Decision Time: " +
                decision.decisionTime.ToString("F2") + " seconds"
            );
        }

        Debug.Log(
            "FINAL TOTAL" +
            " | Paths Used: " + currentRoute.Count +
            " | Total Weight: " + totalWeight +
            " | Total Time: " + totalTime.ToString("F2") + " seconds" +
            " | Total Decision Time: " + totalDecisionTime.ToString("F2") + " seconds"
        );
    }

    private void SavePlaythroughToCSV(PlaythroughData playthrough)
{
    string filePath = Application.dataPath + "/path_results.csv";

    bool fileExists = File.Exists(filePath);

    using (StreamWriter writer = new StreamWriter(filePath, true))
    {
        if (!fileExists)
        {
            writer.WriteLine("PlaythroughID,RecordType,Order,Name,FromNode,ToNode,Weight,TravelTime,DecisionTime,FinalTotalWeight,FinalTravelTime,FinalDecisionTime");
        }

        // Save path records
        for (int i = 0; i < playthrough.paths.Count; i++)
        {
            UsedPath usedPath = playthrough.paths[i];
            PathInfo path = usedPath.pathInfo;

            writer.WriteLine(
                playthrough.playthroughID + "," +
                "Path" + "," +
                (i + 1) + "," +
                path.pathName + "," +
                path.fromNode + "," +
                path.toNode + "," +
                path.weight + "," +
                usedPath.timeTaken.ToString("F3") + "," +
                "" + "," +
                playthrough.finalTotalWeight + "," +
                playthrough.finalTotalTime.ToString("F3") + "," +
                playthrough.finalDecisionTime.ToString("F3")
            );
        }

        // Save decision records
        for (int i = 0; i < playthrough.decisions.Count; i++)
        {
            DecisionData decision = playthrough.decisions[i];

            writer.WriteLine(
                playthrough.playthroughID + "," +
                "Decision" + "," +
                (i + 1) + "," +
                decision.decisionNodeID + "," +
                "" + "," +
                "" + "," +
                "" + "," +
                "" + "," +
                decision.decisionTime.ToString("F3") + "," +
                playthrough.finalTotalWeight + "," +
                playthrough.finalTotalTime.ToString("F3") + "," +
                playthrough.finalDecisionTime.ToString("F3")
            );
        }
    }

    Debug.Log("CSV saved to: " + filePath);
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
            finalTotalTime = totalTime,
            decisions = new List<DecisionData>(currentDecisions),
            finalDecisionTime = totalDecisionTime
        };

        allPlaythroughs.Add(playthrough);
        SavePlaythroughToCSV(playthrough);

        Debug.Log("Playthrough " + playthroughNumber + " saved in memory and CSV.");
        Debug.Log("Total saved playthroughs: " + allPlaythroughs.Count);
        playthroughFinished = true;
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

        //reset decision data
        isDecisionTiming = false;
        decisionStartTime = 0f;
        totalDecisionTime = 0f;
        currentDecisionNode = "";
        currentDecisions.Clear();

        Debug.Log("Current playthrough reset. Ready for next replay.");
    }
    public void MarkResetUsed()
    {
        playthroughFinished = false;
    }
    public bool CanUseResetNode()
    {
        return playthroughFinished;
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

    public List<DecisionData> decisions;

    // This stores the sum of all decision times.
    public float finalDecisionTime;
}

// This stores one Blue decision node's time.
public class DecisionData
{
    public string decisionNodeID;
    public float decisionTime;
}