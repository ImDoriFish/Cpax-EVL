


using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance;

    // =========================================================
    // PATH TRACKING
    // =========================================================

    // The most recent normal node entered by the player.
    private string prevNode = "";

    // Real-world timestamp for when prevNode was entered.
    private string prevNodeEnterTimestamp = "";

    // Unity game time when the current path segment started.
    private float segmentStartTime = 0f;

    // Definitions for every valid path in the maze.
    private readonly Dictionary<string, PathInfo> pathMap =
        new Dictionary<string, PathInfo>();

    // Paths used during the current playthrough.
    private readonly List<UsedPath> currentRoute =
        new List<UsedPath>();

    // All completed playthroughs saved during this Play Mode session.
    private readonly List<PlaythroughData> allPlaythroughs =
        new List<PlaythroughData>();

    private int playthroughNumber = 1;
    private float totalWeight = 0f;
    private float totalTime = 0f;

    // =========================================================
    // DECISION TRACKING
    // =========================================================

    private bool isDecisionTiming = false;
    private float decisionStartTime = 0f;
    private float totalDecisionTime = 0f;
    private string currentDecisionNode = "";

    // Real-world timestamp for when the current Blue node was entered.
    private string decisionEnterTimestamp = "";

    // Decisions recorded during the current playthrough.
    private readonly List<DecisionData> currentDecisions =
        new List<DecisionData>();

    // =========================================================
    // PLAYTHROUGH / CSV STATE
    // =========================================================

    private bool playthroughFinished = false;
    private string csvFilePath = "";

    // Prevents the same unfinished playthrough from being saved twice.
    private bool quitDataSaved = false;

    // Runs before Start() when this object is created.
    // Stores this PathManager in the static Instance variable so other scripts
    // can access it by using PathManager.Instance.
    private void Awake()
    {
        Instance = this;
    }

    // Runs once when Play Mode starts.
    // Creates a new CSV filename for this session and registers every valid path.
    private void Start()
    {
        // Only create a local CSV path in the Editor
        // or a normal desktop build.
    #if !UNITY_WEBGL || UNITY_EDITOR
        CreateCsvFilePath();
    #endif

        // The maze paths are required everywhere.
        RegisterAllPaths();
    }

    // =========================================================
    // PUBLIC NODE ENTRY METHOD
    // =========================================================

    // Main entry point called whenever the player enters any node trigger.
    // It decides whether the node is Blue, Red, the first normal node,
    // the end of a decision, or the end of a normal path.
    public void EnterNode(string nodeID)
    {
        // Capture the real-world timestamp once for this trigger event.
        // Reusing one value prevents tiny timestamp differences inside
        // the same node-entry event.
        string nodeEnterTimestamp = GetCurrentTimestamp();

        Debug.Log(
            "PathManager received node: " + nodeID +
            " at " + nodeEnterTimestamp
        );

        // Blue nodes start decision timing and mark where the last path led.
        if (nodeID.StartsWith("Blue", StringComparison.OrdinalIgnoreCase))
        {
            HandleBlueNode(nodeID, nodeEnterTimestamp);
            return;
        }

        // Red nodes are exits. They finish and save the playthrough.
        if (nodeID.StartsWith("Red", StringComparison.OrdinalIgnoreCase))
        {
            HandleRedNode(nodeID, nodeEnterTimestamp);
            return;
        }

        // The first normal node begins the first path segment.
        if (string.IsNullOrEmpty(prevNode))
        {
            StartNewPathSegment(nodeID, nodeEnterTimestamp);
            return;
        }

        // The first normal node after a Blue node ends that decision.
        // It also becomes the starting node for the next path.
        if (isDecisionTiming)
        {
            FinishDecision(nodeID, nodeEnterTimestamp);
            return;
        }

        // Otherwise, the player moved from one normal node to another.
        RecordPath(nodeID, nodeEnterTimestamp);
    }

    // =========================================================
    // NODE HANDLERS
    // =========================================================

    // Handles a Blue decision node.
    // Marks the previous path as leading to this Blue node and starts
    // measuring the player's decision time.
    private void HandleBlueNode(string nodeID, string blueEnterTimestamp)
    {
        // The most recently completed path led to this Blue node.
        if (currentRoute.Count > 0)
        {
            UsedPath lastUsedPath = currentRoute[currentRoute.Count - 1];
            lastUsedPath.toDecisionNodeAndEnd = nodeID;

            Debug.Log(
                "Path " + lastUsedPath.pathInfo.pathName +
                " leads to decision node " + nodeID
            );
        }

        // Start measuring how long the player stays in the decision area.
        isDecisionTiming = true;
        decisionStartTime = Time.time;
        currentDecisionNode = nodeID;
        decisionEnterTimestamp = blueEnterTimestamp;

        Debug.Log(
            "Decision started at " + currentDecisionNode +
            " | Enter timestamp: " + decisionEnterTimestamp
        );
    }

    // Handles a Red exit node.
    // Marks the previous path as leading to the exit, stores the exit timestamp,
    // and finishes the current playthrough.
    private void HandleRedNode(string nodeID, string redEnterTimestamp)
    {
        // The most recently completed path led to this Red exit.
        if (currentRoute.Count > 0)
        {
            UsedPath lastUsedPath = currentRoute[currentRoute.Count - 1];

            lastUsedPath.toDecisionNodeAndEnd = nodeID;

            // Only a path leading to Red receives an exit timestamp.
            lastUsedPath.exitEnterTimestamp = redEnterTimestamp;

            Debug.Log(
                "Path " + lastUsedPath.pathInfo.pathName +
                " reached exit " + nodeID +
                " at " + redEnterTimestamp
            );
        }

        FinishPlaythrough();
    }

    // Starts timing a new path segment from a normal node.
    // Stores the node ID, its real-world entry timestamp, and the Unity start time.
    private void StartNewPathSegment(
        string nodeID,
        string nodeEnterTimestamp
    )
    {
        prevNode = nodeID;
        prevNodeEnterTimestamp = nodeEnterTimestamp;
        segmentStartTime = Time.time;

        Debug.Log(
            "New path segment started at " + prevNode +
            " | Enter timestamp: " + prevNodeEnterTimestamp
        );
    }

    // Ends the current Blue-node decision.
    // Calculates the decision duration, stores its enter/exit timestamps,
    // resets decision state, and starts the next path from the new normal node.
    private void FinishDecision(
        string nextNormalNode,
        string decisionExitTimestamp
    )
    {
        float decisionTime = Time.time - decisionStartTime;
        totalDecisionTime += decisionTime;

        DecisionData decisionData = new DecisionData
        {
            decisionNodeID = currentDecisionNode,
            decisionTime = decisionTime,
            decisionEnterTimestamp = decisionEnterTimestamp,
            decisionExitTimestamp = decisionExitTimestamp
        };

        currentDecisions.Add(decisionData);

        Debug.Log(
            "Decision finished at " + currentDecisionNode +
            " | Duration: " + FormatFloat(decisionTime) + " seconds" +
            " | Entered: " + decisionEnterTimestamp +
            " | Exited: " + decisionExitTimestamp
        );

        // Reset decision state.
        isDecisionTiming = false;
        decisionStartTime = 0f;
        currentDecisionNode = "";
        decisionEnterTimestamp = "";

        // The node that ended the decision begins the next path.
        StartNewPathSegment(nextNormalNode, decisionExitTimestamp);
    }

    // Records travel from prevNode to the current normal node.
    // Looks up the matching path, calculates travel time, stores timestamps,
    // updates totals, and then starts the next possible path segment.
    private void RecordPath(
        string currentNode,
        string currentNodeEnterTimestamp
    )
    {
        string pathKey = MakePathKey(prevNode, currentNode);

        Debug.Log("Checking path: " + pathKey);

        if (pathMap.TryGetValue(pathKey, out PathInfo path))
        {
            float timeTravel = Time.time - segmentStartTime;

            UsedPath storedPath = new UsedPath
            {
                pathInfo = path,
                timeTaken = timeTravel,
                fromNodeEnterTimestamp = prevNodeEnterTimestamp,
                toNodeEnterTimestamp = currentNodeEnterTimestamp
            };

            currentRoute.Add(storedPath);

            totalWeight += path.weight;
            totalTime += timeTravel;

            Debug.Log(
                "RECORDED PATH: " + path.pathName +
                " | " + path.fromNode + " -> " + path.toNode +
                " | Weight: " + FormatFloat(path.weight) +
                " | Travel time: " + FormatFloat(timeTravel) + " seconds" +
                " | From entered: " + storedPath.fromNodeEnterTimestamp +
                " | To entered: " + storedPath.toNodeEnterTimestamp
            );
        }
        else
        {
            Debug.LogWarning("NO PATH FOUND for key: " + pathKey);
        }

        // Whether or not the path was found, this normal node becomes
        // the start of the next possible path segment.
        StartNewPathSegment(currentNode, currentNodeEnterTimestamp);
    }

    // Resets the blockers belonging to every PathNode.
    private void ResetAllBackwardBlockers()
    {
        PathNode[] allNodes = FindObjectsOfType<PathNode>();

        for (int i = 0; i < allNodes.Length; i++)
        {
            allNodes[i].ResetBackwardBlocker();
        }
    }

    // =========================================================
    // PLAYTHROUGH COMPLETION
    // =========================================================

    // Runs when the game closes or when Play Mode stops.
    // If the player did not reach Red, save the completed data as Incomplete.
    private void OnApplicationQuit()
    {
        // Save any unfinished playthrough before trying to open the CSV.
        SaveIncompletePlaythrough();

        // Only open the CSV in an actual Windows or macOS desktop build.
        // Do not run this inside the Unity Editor or WebGL.
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        OpenCsvFile();
#endif
    }

    // Opens the saved CSV using the computer's default spreadsheet application.
    // This runs only after the Windows or macOS desktop build closes normally.
    private void OpenCsvFile()
    {
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            Debug.LogWarning(
                "CSV file could not be opened because it does not exist: " +
                csvFilePath
            );

            return;
        }

        try
        {
#if UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = csvFilePath,
                    UseShellExecute = true
                }
            );

#elif UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/usr/bin/open",
                    Arguments = "\"" + csvFilePath + "\"",
                    UseShellExecute = false
                }
            );
#endif
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not open CSV file: " + exception.Message
            );
        }
    }

    // Saves the paths and decisions that were completed before the player quit.
    // A path or decision that was still in progress is not added.
    private void SaveIncompletePlaythrough()
    {
        // Do not save the same unfinished playthrough more than once.
        if (quitDataSaved)
        {
            return;
        }

        // Do not save anything if the player never entered a normal node
        // and no path or decision was recorded.
        if (
            prevNode == "" &&
            currentRoute.Count == 0 &&
            currentDecisions.Count == 0
        )
        {
            return;
        }

        quitDataSaved = true;

        PlaythroughData playthrough = new PlaythroughData
        {
            playthroughID = playthroughNumber,
            paths = new List<UsedPath>(currentRoute),
            decisions = new List<DecisionData>(currentDecisions),
            finalTotalWeight = totalWeight,
            finalTotalTime = totalTime,
            finalDecisionTime = totalDecisionTime,
            completionStatus = "Incomplete"
        };

        SavePlaythroughToCSV(playthrough);

        Debug.Log(
            "Player quit before reaching Red. " +
            "Incomplete playthrough saved."
        );
    }

    // Completes the current playthrough after the player reaches Red.
    // Copies the current path and decision data, saves it to memory and CSV,
    // increases the playthrough number, and resets for the next run.
    private void FinishPlaythrough()
    {
        if (currentRoute.Count == 0)
        {
            Debug.LogWarning(
                "Red node reached, but no paths were recorded."
            );

            ResetCurrentPlaythrough();
            return;
        }

        PrintFullPlaythrough();

        PlaythroughData playthrough = new PlaythroughData
        {
            playthroughID = playthroughNumber,
            paths = new List<UsedPath>(currentRoute),
            decisions = new List<DecisionData>(currentDecisions),
            finalTotalWeight = totalWeight,
            finalTotalTime = totalTime,
            finalDecisionTime = totalDecisionTime,
            completionStatus = "Completed"
        };

        allPlaythroughs.Add(playthrough);   

        #if UNITY_WEBGL && !UNITY_EDITOR

                // Actual WebGL browser build.
                if (PlaythroughUploader.Instance != null)
                {
                    PlaythroughUploader.Instance.PrepareAndUpload(
                        playthrough
                    );
                }
                else
                {
                    Debug.LogError(
                        "PlaythroughUploader was not found in the scene."
                    );
                }

        #else

                // Unity Editor or desktop build.
                SavePlaythroughToCSV(playthrough);

        #endif

        

        Debug.Log(
            "Playthrough " + playthroughNumber +
            " saved in memory and CSV."
        );

        Debug.Log(
            "Total saved playthroughs: " + allPlaythroughs.Count
        );

        playthroughFinished = true;
        playthroughNumber++;

        ResetCurrentPlaythrough();
    }

    // Clears all temporary data for the current playthrough.
    // This does not delete previously saved playthroughs or the CSV file.
    private void ResetCurrentPlaythrough()
    {
        // Reset path data.
        prevNode = "";
        prevNodeEnterTimestamp = "";
        segmentStartTime = 0f;
        currentRoute.Clear();
        totalWeight = 0f;
        totalTime = 0f;

        // Reset decision data.
        isDecisionTiming = false;
        decisionStartTime = 0f;
        totalDecisionTime = 0f;
        currentDecisionNode = "";
        decisionEnterTimestamp = "";
        currentDecisions.Clear();

        Debug.Log(
            "Current playthrough reset. Ready for the next replay."
        );
    }

    // =========================================================
    // CSV OUTPUT
    // =========================================================

    // Creates one unique CSV filename when Play Mode starts.
    // The timestamp in the filename prevents a new session from overwriting
    // or mixing with an older session.
    private void CreateCsvFilePath()
    {
        string sessionTimestamp =
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        /*csvFilePath = Path.Combine(
            Application.dataPath,
            "path_results_" + sessionTimestamp + ".csv"
        );*/

        csvFilePath = Path.Combine(
        Application.persistentDataPath,
        "path_results_" + sessionTimestamp + ".csv"
        );

        Debug.Log(
            "This Play Mode session will save to: " + csvFilePath
        );
    }

    // Saves one completed playthrough to the current session's CSV file.
    // Writes the header only once, then writes Path, Decision, and Summary rows.
    private void SavePlaythroughToCSV(PlaythroughData playthrough)
    {
        bool fileExists = File.Exists(csvFilePath);

        using (StreamWriter writer =
               new StreamWriter(csvFilePath, append: true))
        {
            if (!fileExists)
            {
                WriteCsvHeader(writer);
            }

            WritePathRows(writer, playthrough);
            WriteDecisionRows(writer, playthrough);
            WriteSummaryRow(writer, playthrough);
        }

        Debug.Log("CSV saved to: " + csvFilePath);
    }

    // Writes the CSV column names.
    // This runs only when the CSV file is first created.
    private static void WriteCsvHeader(StreamWriter writer)
    {
        writer.WriteLine(
            "PlaythroughID," +
            "RecordType," +
            "CompletionStatus," +
            "Order," +
            "Name," +
            "FromNode," +
            "FromNodeEnterTimestamp," +
            "ToNode," +
            "ToNodeEnterTimestamp," +
            "Weight," +
            "TravelTime," +
            "DecisionTime," +
            "DecisionEnterTimestamp," +
            "DecisionExitTimestamp," +
            "ToDecisionNodeAndEnd," +
            "ExitEnterTimestamp," +
            "FinalTotalWeight," +
            "FinalTravelTime," +
            "FinalDecisionTime"
        );
    }

    // Writes one CSV row for every path used in the playthrough.
    // Path rows contain node names, node timestamps, travel time, weight,
    // the following Blue/Red node, and the Red exit timestamp when applicable.
    private static void WritePathRows(
        StreamWriter writer,
        PlaythroughData playthrough
    )
    {
        for (int i = 0; i < playthrough.paths.Count; i++)
        {
            UsedPath usedPath = playthrough.paths[i];
            PathInfo path = usedPath.pathInfo;

            WriteCsvRow(
                writer,
                playthrough.playthroughID.ToString(), // PlaythroughID
                "Path",                              // RecordType
                playthrough.completionStatus,         // CompletionStatus
                (i + 1).ToString(),                   // Order
                path.pathName,                        // Name
                path.fromNode,                        // FromNode
                usedPath.fromNodeEnterTimestamp,      // FromNode timestamp
                path.toNode,                          // ToNode
                usedPath.toNodeEnterTimestamp,        // ToNode timestamp
                FormatFloat(path.weight),             // Weight
                FormatFloat(usedPath.timeTaken),      // TravelTime
                "",                                  // DecisionTime
                "",                                  // DecisionEnterTimestamp
                "",                                  // DecisionExitTimestamp
                usedPath.toDecisionNodeAndEnd,         // Blue or Red destination
                usedPath.exitEnterTimestamp,           // Only filled for Red exit
                "",                                  // FinalTotalWeight
                "",                                  // FinalTravelTime
                ""                                   // FinalDecisionTime
            );
        }
    }

    
    // Writes one CSV row for every Blue decision in the playthrough.
    // Decision rows contain the decision duration and its enter/exit timestamps.
    private static void WriteDecisionRows(
        StreamWriter writer,
        PlaythroughData playthrough
    )
    {
        for (int i = 0; i < playthrough.decisions.Count; i++)
        {
            DecisionData decision = playthrough.decisions[i];

            WriteCsvRow(
                writer,
                playthrough.playthroughID.ToString(), // PlaythroughID
                "Decision",                          // RecordType
                playthrough.completionStatus,         // CompletionStatus
                (i + 1).ToString(),                   // Order
                decision.decisionNodeID,              // Name
                "",                                  // FromNode
                "",                                  // FromNodeEnterTimestamp
                "",                                  // ToNode
                "",                                  // ToNodeEnterTimestamp
                "",                                  // Weight
                "",                                  // TravelTime
                FormatFloat(decision.decisionTime),   // DecisionTime
                decision.decisionEnterTimestamp,      // Decision entered
                decision.decisionExitTimestamp,       // Decision exited
                "",                                  // ToDecisionNodeAndEnd
                "",                                  // ExitEnterTimestamp
                "",                                  // FinalTotalWeight
                "",                                  // FinalTravelTime
                ""                                   // FinalDecisionTime
            );
        }
    }

    // Writes one final Summary row for the playthrough.
    // Only the final total weight, travel time, and decision time are filled here.
    private static void WriteSummaryRow(
        StreamWriter writer,
        PlaythroughData playthrough
    )
    {
        WriteCsvRow(
            writer,
            playthrough.playthroughID.ToString(),     // PlaythroughID
            "Summary",                              // RecordType
            playthrough.completionStatus,           // CompletionStatus
            "",                                     // Order
            "",                                     // Name
            "",                                     // FromNode
            "",                                     // FromNodeEnterTimestamp
            "",                                     // ToNode
            "",                                     // ToNodeEnterTimestamp
            "",                                     // Weight
            "",                                     // TravelTime
            "",                                     // DecisionTime
            "",                                     // DecisionEnterTimestamp
            "",                                     // DecisionExitTimestamp
            "",                                     // ToDecisionNodeAndEnd
            "",                                     // ExitEnterTimestamp
            FormatFloat(playthrough.finalTotalWeight), // FinalTotalWeight
            FormatFloat(playthrough.finalTotalTime),   // FinalTravelTime
            FormatFloat(playthrough.finalDecisionTime) // FinalDecisionTime
        );
    }

    // Writes one safe CSV row. Any value containing a comma, quote,
    // or newline is automatically wrapped and escaped.
    // Accepts any number of string values, makes each value safe for CSV,
    // joins them with commas, and writes the completed row to the file.
    private static void WriteCsvRow(
        StreamWriter writer,
        params string[] values
    )
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = EscapeCsvValue(values[i]);
        }

        writer.WriteLine(string.Join(",", values));
    }

    // Makes one value safe to store in a CSV cell.
    // If the value contains a comma, quotation mark, or new line,
    // it surrounds the value with quotes and escapes internal quotes.
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        bool needsQuotes =
            value.Contains(",") ||
            value.Contains("\"") ||
            value.Contains("\n") ||
            value.Contains("\r");

        if (!needsQuotes)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }


    // Converts a float into text with exactly three digits after the decimal.
    // Example: 2 becomes "2.000" and 6.37842 becomes "6.378".
    private static string FormatFloat(float value)
    {
        return value.ToString("F3");
    }

    // =========================================================
    // DEBUG OUTPUT
    // =========================================================

    // Prints the completed paths, decisions, and totals to the Unity Console.
    // This is for debugging and does not control what is saved in the CSV.
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
                " | Weight: " + FormatFloat(path.weight) +
                " | Time: " + FormatFloat(usedPath.timeTaken) +
                " seconds" +
                " | Leads to: " + usedPath.toDecisionNodeAndEnd
            );
        }

        Debug.Log("========== DECISION TIMES ==========");

        for (int i = 0; i < currentDecisions.Count; i++)
        {
            DecisionData decision = currentDecisions[i];

            Debug.Log(
                (i + 1) + ". " +
                decision.decisionNodeID +
                " | Decision time: " +
                FormatFloat(decision.decisionTime) +
                " seconds"
            );
        }

        Debug.Log(
            "FINAL TOTAL" +
            " | Paths Used: " + currentRoute.Count +
            " | Total Weight: " + FormatFloat(totalWeight) +
            " | Total Travel Time: " + FormatFloat(totalTime) +
            " seconds" +
            " | Total Decision Time: " +
            FormatFloat(totalDecisionTime) + " seconds"
        );
    }

    // =========================================================
    // RESET NODE ACCESS
    // =========================================================

    // Marks the reset node as used after a finished playthrough.
    // This prevents the reset node from being used repeatedly before another run ends.
    public void MarkResetUsed()
    {
        playthroughFinished = false;
        ResetAllBackwardBlockers();
    }

    // Returns true only after a playthrough has finished.
    // Other scripts can call this to decide whether teleport/reset is currently allowed.
    public bool CanUseResetNode()
    {
        return playthroughFinished;
    }

    // =========================================================
    // PATH DEFINITIONS
    // =========================================================

    // Registers every valid path in the maze.
    // Each entry defines its start node, end node, path ID, and weight.
    private void RegisterAllPaths()
    {
        // Route group 1
        //AddPath("P10_A", "P10_B", "1001", 2f);
        AddPath("S0", "L1_0", "1001", 2f);


        //AddPath("P30_A", "P20_B", "2001", 6f);
        AddPath("S1", "L1_0", "2001", 6f);


        //AddPath("P40_A", "P30_B", "3001", 2f);
        AddPath("S1", "L1_1", "3001", 2f);


        //AddPath("P50_A", "P40_B", "4001", 6f);
        AddPath("S2", "L1_2", "4001", 6f);


        //AddPath("P20_A", "P50_B", "5001", 6f);
        AddPath("S0", "L1_3", "5001", 6f);


        //AddPath("P60_A", "P60_B", "6001", 5f);
        AddPath("S2", "L1_3", "6001", 5f);

        // Route group 2
        //AddPath("P10_C", "P10_D", "1002", 1f);
        AddPath("L1_0", "L2_0", "1002", 1f);


        //AddPath("P41_C", "P20_D", "2002", 3f);
        AddPath("L1_2", "L2_0", "2002", 3f);

        
        //AddPath("P31_C", "P31_D", "3002", 3f);
        AddPath("L1_1", "L2_1", "3002", 3f);


        
        //AddPath("P32_C", "P32_D", "4002", 5f);
        AddPath("L1_1", "L2_2", "4002", 5f);



        //AddPath("P50_C", "P41_D", "5002", 6f);
        AddPath("L1_3", "L2_2", "5002", 6f);

        
        //AddPath("P42_C", "P42_D", "6002", 5f);
        AddPath("L1_2", "L2_2", "6002", 5f);


        //AddPath("P20_C", "P50_D", "7002", 6f);
        AddPath("L1_0", "L2_3", "7002", 6f);


        //AddPath("P60_C", "P60_D", "8002", 3f);
        AddPath("L1_3", "L2_3", "8002", 3f);

        // Route group 3

        //AddPath("P10_E", "P10_F", "1003", 8f);
        AddPath("L2_0", "E0", "1003", 8f);


        //AddPath("P50_E", "P20_F", "2003", 4f);
        AddPath("L2_3", "E0", "2003", 4f);

        
        //AddPath("P31_E", "P31_F", "3003", 3f);
        AddPath("L2_1", "E0", "3003", 3f);


        //AddPath("P41_E", "P32_F", "4003", 7f);
        AddPath("L2_2", "E0", "4003", 7f);


        //AddPath("P42_E", "P41_F", "5003", 7f);
        AddPath("L2_2", "E1", "5003", 7f);


        //AddPath("P20_E", "P42_F", "6003", 7f);
        AddPath("L2_0", "E1", "6003", 7f);


        //AddPath("P32_E", "P50_F", "7003", 3f);
        AddPath("L2_1", "E1", "7003", 3f);


        //AddPath("P60_E", "P60_F", "8003", 9f);
        AddPath("L2_3", "E1", "8003", 9f);
    }

    // Creates one PathInfo object and stores it in pathMap.
    // The dictionary key is built from the from-node and to-node IDs.
    private void AddPath(
        string fromNode,
        string toNode,
        string pathName,
        float weight
    )
    {
        string key = MakePathKey(fromNode, toNode);

        pathMap[key] = new PathInfo
        {
            fromNode = fromNode,
            toNode = toNode,
            pathName = pathName,
            weight = weight
        };
    }

    // Builds the dictionary key used to identify a path.
    // Example: P10_A and P10_B become "P10_A -> P10_B".
    private static string MakePathKey(string fromNode, string toNode)
    {
        return fromNode + " -> " + toNode;
    }

    // Returns the current real-world date and time as text.
    // The .fff part includes milliseconds.
    private static string GetCurrentTimestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}

// =============================================================
// DATA CLASSES
// =============================================================

// Permanent definition of one valid maze path.
[System.Serializable]
public class PathInfo
{
    public string fromNode;
    public string toNode;
    public string pathName;
    public float weight;
}

// Data collected when the player actually travels through one path.
[System.Serializable]
public class UsedPath
{
    public PathInfo pathInfo;
    public float timeTaken;

    // When the player entered the path's starting normal node.
    public string fromNodeEnterTimestamp = "";

    // When the player entered the path's ending normal node.
    public string toNodeEnterTimestamp = "";

    // The Blue decision node or Red exit reached after this path.
    public string toDecisionNodeAndEnd = "";

    // Only filled when this path leads to a Red exit.
    public string exitEnterTimestamp = "";
}

// All data for one completed playthrough.
[System.Serializable]
public class PlaythroughData
{
    public int playthroughID;
    public List<UsedPath> paths;
    public List<DecisionData> decisions;

    public float finalTotalWeight;
    public float finalTotalTime;
    public float finalDecisionTime;

    // "Completed" means the player reached Red.
    // "Incomplete" means the player quit before reaching Red.
    public string completionStatus;
}

// Data collected for one Blue decision node.
[System.Serializable]
public class DecisionData
{
    public string decisionNodeID;
    public float decisionTime;

    // When the player entered the Blue node.
    public string decisionEnterTimestamp = "";

    // When the player reached the first normal node after Blue.
    public string decisionExitTimestamp = "";
}