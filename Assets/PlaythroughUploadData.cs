using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

[Serializable]
public class PlaythroughUploadData
{
    // Information about the overall upload.
    public string schemaVersion;
    public string sessionID;
    public string submittedAtUtc;

    // Information about this playthrough.
    public int playthroughID;
    public bool completed;
    public string completionStatus;

    // Detailed records.
    public UsedPathUploadData[] paths;
    public DecisionUploadData[] decisions;

    // Final totals.
    public float finalTotalWeight;
    public float finalTravelTime;
    public float finalDecisionTime;
}

[Serializable]
public class UsedPathUploadData
{
    public int order;

    public string pathName;
    public string fromNode;
    public string fromNodeEnterTimestamp;
    public string toNode;
    public string toNodeEnterTimestamp;

    public float weight;
    public float travelTime;

    public string toDecisionNodeAndEnd;
    public string exitEnterTimestamp;
}

[Serializable]
public class DecisionUploadData
{
    public int order;

    public string decisionNodeID;
    public float decisionTime;

    public string decisionEnterTimestamp;
    public string decisionExitTimestamp;
}