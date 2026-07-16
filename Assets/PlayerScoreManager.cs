using UnityEngine;
using TMPro;

public class PlayerScoreManager : MonoBehaviour
{
    public static PlayerScoreManager Instance;

    // Highest possible score.
    public float startingScore = 100f;

    // The player does not lose points during this time.
    public float bestTimeLimit = 20f;

    // Points lost every second after the best time limit.
    public float pointsLostPerSecond = 1f;

    // The player's current score.
    private float currentScore;

    // Time since the player entered the Start node.
    private float elapsedTime = 0f;

    // True while the score timer is running.
    private bool scoreIsRunning = false;

    // Used to print the score once per second.
    private int lastPrintedSecond = -1;

    // Text shown after the player reaches the Red node.
    public TMP_Text finalScoreText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentScore = startingScore;

        Debug.Log(
            "Score system ready. Maximum score: " +
            currentScore.ToString("F2")
        );
    }

    private void Update()
    {
        if (!scoreIsRunning)
        {
            return;
        }

        // Count how long the current playthrough has taken.
        elapsedTime += Time.deltaTime;

        // Update the score.
        CalculateCurrentScore();

        // Print the score once per second.
        int currentSecond = Mathf.FloorToInt(elapsedTime);

        if (currentSecond != lastPrintedSecond)
        {
            lastPrintedSecond = currentSecond;

            Debug.Log(
                "Time: " + elapsedTime.ToString("F2") +
                " seconds | Current Score: " +
                currentScore.ToString("F2")
            );
        }
    }

    // Calculates the current score using elapsed time.
    private void CalculateCurrentScore()
    {
        // Keep the full score during the first 20 seconds.
        if (elapsedTime <= bestTimeLimit)
        {
            currentScore = startingScore;
            return;
        }

        // Only penalize time after the first 20 seconds.
        float penalizedTime = elapsedTime - bestTimeLimit;

        currentScore =
            startingScore -
            (penalizedTime * pointsLostPerSecond);

        // Prevent the score from going below zero.
        if (currentScore < 0f)
        {
            currentScore = 0f;
        }
    }

    // Starts scoring when the player reaches a Start node.
    public void StartScore()
    {
        // Prevent an active score from restarting.
        if (scoreIsRunning)
        {
            return;
        }

        currentScore = startingScore;
        elapsedTime = 0f;
        scoreIsRunning = true;
        lastPrintedSecond = -1;

        // Hide the previous final score.
        if (finalScoreText != null)
        {
            finalScoreText.gameObject.SetActive(false);
        }

        Debug.Log("========== SCORE STARTED ==========");

        Debug.Log(
            "Starting Score: " +
            currentScore.ToString("F2")
        );
    }

    // Stops scoring when the player reaches a Red node.
    public void FinishScore()
    {
        if (!scoreIsRunning)
        {
            Debug.LogWarning(
                "Red node reached, but the score was not running."
            );

            return;
        }

        // Calculate one final time before stopping.
        CalculateCurrentScore();

        scoreIsRunning = false;

        if (finalScoreText != null)
        {
            finalScoreText.text = "Score: " + currentScore.ToString("F2");
            finalScoreText.gameObject.SetActive(true);
        }

        Debug.Log("========== SCORE FINISHED ==========");

        Debug.Log(
            "Completion Time: " +
            elapsedTime.ToString("F2") +
            " seconds"
        );

        Debug.Log(
            "Final Score: " +
            currentScore.ToString("F2")
        );
    }
}