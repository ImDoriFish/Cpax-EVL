using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PlaythroughUploader : MonoBehaviour
{
    public static PlaythroughUploader Instance;

    [Header("Server Settings")]

    // Leave blank until the server team gives you the endpoint URL.
    [SerializeField]
    private string serverUrl = "";

    // Leave blank until the authentication method is confirmed.
    [SerializeField]
    private string authenticationToken = "";

    [Header("Testing")]

    // When checked, JSON is created and printed,
    // but nothing is sent to a server.
    [SerializeField]
    private bool testMode = true;

    // Unique ID for this game session.
    private string sessionID;

    private void Awake()
    {
        Instance = this;

        sessionID = Guid.NewGuid().ToString();

        Debug.Log(
            "Upload session created: " + sessionID
        );
    }

    public void PrepareAndUpload(
        PlaythroughData playthrough
    )
    {
        if (playthrough == null)
        {
            Debug.LogError(
                "Cannot prepare upload because playthrough is null."
            );

            return;
        }

        PlaythroughUploadData uploadData =
            CreateUploadData(playthrough);

        // Convert the upload object into JSON text.
        string json = JsonUtility.ToJson(
            uploadData,
            true
        );

        Debug.Log(
            "========== PREPARED JSON ==========\n" +
            json
        );

        // Stop here while there is no server.
        if (testMode)
        {
            Debug.Log(
                "Test Mode is enabled. No server request was sent."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            Debug.LogError(
                "Server URL is empty."
            );

            return;
        }

        StartCoroutine(
            SendJsonToServer(json)
        );
    }

    private PlaythroughUploadData CreateUploadData(
        PlaythroughData playthrough
    )
    {
        UsedPathUploadData[] uploadedPaths =
            new UsedPathUploadData[
                playthrough.paths.Count
            ];

        for (int i = 0;
             i < playthrough.paths.Count;
             i++)
        {
            UsedPath usedPath =
                playthrough.paths[i];

            uploadedPaths[i] =
                new UsedPathUploadData
                {
                    order = i + 1,

                    pathName =
                        usedPath.pathInfo.pathName,

                    fromNode =
                        usedPath.pathInfo.fromNode,

                    fromNodeEnterTimestamp =
                        usedPath.fromNodeEnterTimestamp,

                    toNode =
                        usedPath.pathInfo.toNode,

                    toNodeEnterTimestamp =
                        usedPath.toNodeEnterTimestamp,

                    weight =
                        usedPath.pathInfo.weight,

                    travelTime =
                        usedPath.timeTaken,

                    toDecisionNodeAndEnd =
                        usedPath.toDecisionNodeAndEnd,

                    exitEnterTimestamp =
                        usedPath.exitEnterTimestamp
                };
        }

        DecisionUploadData[] uploadedDecisions =
            new DecisionUploadData[
                playthrough.decisions.Count
            ];

        for (int i = 0;
             i < playthrough.decisions.Count;
             i++)
        {
            DecisionData decision =
                playthrough.decisions[i];

            uploadedDecisions[i] =
                new DecisionUploadData
                {
                    order = i + 1,

                    decisionNodeID =
                        decision.decisionNodeID,

                    decisionTime =
                        decision.decisionTime,

                    decisionEnterTimestamp =
                        decision.decisionEnterTimestamp,

                    decisionExitTimestamp =
                        decision.decisionExitTimestamp
                };
        }

        return new PlaythroughUploadData
        {
            schemaVersion = "1.0",
            sessionID = sessionID,

            submittedAtUtc =
                DateTime.UtcNow.ToString("o"),

            playthroughID =
                playthrough.playthroughID,

            // FinishPlaythrough is called after reaching Red.
            completed = true,
            completionStatus = "Completed",

            paths = uploadedPaths,
            decisions = uploadedDecisions,

            finalTotalWeight =
                playthrough.finalTotalWeight,

            finalTravelTime =
                playthrough.finalTotalTime,

            finalDecisionTime =
                playthrough.finalDecisionTime
        };
    }

    private IEnumerator SendJsonToServer(
        string json
    )
    {
        byte[] jsonBytes =
            Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request =
               new UnityWebRequest(serverUrl, "POST"))
        {
            request.uploadHandler =
                new UploadHandlerRaw(jsonBytes);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            AddAuthentication(request);

            Debug.Log(
                "Sending playthrough to: " +
                serverUrl
            );

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                Debug.Log(
                    "Upload successful." +
                    "\nServer response: " +
                    request.downloadHandler.text
                );
            }
            else
            {
                Debug.LogError(
                    "Upload failed." +
                    "\nError: " + request.error +
                    "\nHTTP status: " +
                    request.responseCode +
                    "\nResponse: " +
                    request.downloadHandler.text
                );
            }
        }
    }

    private void AddAuthentication(
        UnityWebRequest request
    )
    {
        // Leave empty until the server team confirms
        // how authentication should work.
        if (string.IsNullOrWhiteSpace(
            authenticationToken
        ))
        {
            return;
        }

        // This currently assumes a Bearer token.
        // Change this method later if the server
        // uses a different authentication system.
        request.SetRequestHeader(
            "Authorization",
            "Bearer " + authenticationToken
        );
    }
}