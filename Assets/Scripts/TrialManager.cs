using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TrialManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MazeConfig config;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlatformTrigger platformTrigger;
    [SerializeField] private Transform platformRoot;
    [SerializeField] private Transform poolCenter;
    [SerializeField] private DataLogger dataLogger;
    [SerializeField] private TrialAudioFeedback audioFeedback; 
    [SerializeField] private ExperimentStatusUI statusUI;

    [Header("Experiment")]
    [SerializeField] private Key startKey = Key.T;

    [SerializeField, Min(0f)]
    private float trialIntroDuration = 1.5f;

    private bool experimentRunning;
    private bool trialRunning;
    private bool platformReached;
    private bool headingErrorRecorded;

    private TrialPhase currentPhase;
    private int currentTrialNumber;

    private float trialStartTime;
    private float headingError = -1f;

    private Vector3 trialStartPosition;

    private float probeTimeNE;
    private float probeTimeNW;
    private float probeTimeSW;
    private float probeTimeSE;

    private void Start()
    {
        if (player != null)
            player.enabled = false;

        if (platformTrigger != null)
        {
            platformTrigger.SetVisible(false);
            platformTrigger.SetActive(false);
        }

        Debug.Log("VMWT ready. Press T to start experiment.");
    }

    private void OnEnable()
    {
        if (platformTrigger != null)
        {
            platformTrigger.OnPlayerReachedPlatform +=
                HandlePlayerReachedPlatform;
        }
    }

    private void OnDisable()
    {
        if (platformTrigger != null)
        {
            platformTrigger.OnPlayerReachedPlatform -=
                HandlePlayerReachedPlatform;
        }
    }

    private void Update()
    {
        if (!experimentRunning)
        {
            if (Keyboard.current != null && Keyboard.current[startKey].wasPressedThisFrame)
                StartExperiment();

            return;
        }

        if (!trialRunning)
            return;

        if (currentPhase == TrialPhase.Hidden)
            TryRecordHeadingError();

        if (currentPhase == TrialPhase.Probe)
            RecordProbeQuadrantTime();
    }

    public void StartExperiment()
    {
        if (experimentRunning)
            return;

        if (!ValidateReferences())
            return;

        SyncPlatformPositionWithConfig();

        StartCoroutine(RunExperiment());
    }

    private IEnumerator RunExperiment()
    {
        experimentRunning = true;

        Debug.Log("===== VMWT EXPERIMENT STARTED =====");

        // FAZ I
        yield return RunPhase(
            TrialPhase.Exploration,
            config.ExplorationTrialCount);

        yield return WaitITI();

        // FAZ II
        yield return RunPhase(
            TrialPhase.Hidden,
            config.HiddenTrialCount);

        // ARA
        yield return RunBreak();

        // FAZ III
        yield return ShowTrialIntro(
            TrialPhase.Probe,
            1,
            1);

        yield return RunSingleTrial(
            TrialPhase.Probe,
            1,
            CardinalPoint.N);

        yield return WaitITI();

        // FAZ IV
        yield return RunPhase(
            TrialPhase.Visible,
            config.VisibleTrialCount);

        platformTrigger.SetVisible(false);
        platformTrigger.SetActive(false);

        player.enabled = false;
        experimentRunning = false;

        statusUI.ShowCompleted();

        Debug.Log(
            "===== VMWT EXPERIMENT COMPLETED =====\n" +
            $"CSV file: {dataLogger.FilePath}");
    }

    private IEnumerator RunPhase(
        TrialPhase phase,
        int trialCount)
    {
        Debug.Log(
            $"===== {phase.ToString().ToUpper()} PHASE STARTED =====");

        List<CardinalPoint> deck = new();

        for (int i = 0; i < trialCount; i++)
        {
            if (deck.Count == 0)
                deck = CreateShuffledStartDeck();

            CardinalPoint startPoint = deck[0];
            deck.RemoveAt(0);

            yield return ShowTrialIntro(
                phase,
                i + 1,
                trialCount);

            yield return RunSingleTrial(
                phase,
                i + 1,
                startPoint);

            if (i < trialCount - 1)
                yield return WaitITI();
        }

        Debug.Log(
            $"===== {phase.ToString().ToUpper()} PHASE COMPLETED =====");
    }

    private IEnumerator RunSingleTrial(
        TrialPhase phase,
        int trialNumber,
        CardinalPoint startPoint)
    {
        currentPhase = phase;
        currentTrialNumber = trialNumber;

        ConfigurePlatform(phase);

        Vector3 center = poolCenter.position;
        Vector3 startPosition;

        if (phase == TrialPhase.Probe)
        {
            startPosition =
                QuadrantUtils.GetProbeStartPosition(
                    platformRoot.position,
                    center,
                    config.PoolRadius);
        }
        else
        {
            startPosition =
                QuadrantUtils.GetCardinalStartPosition(
                    startPoint,
                    center,
                    config.PoolRadius);
        }

        startPosition.y = player.transform.position.y;

        Quaternion startRotation =
            QuadrantUtils.GetRotationFacingCenter(
                startPosition,
                center);

        player.enabled = true;
        player.Teleport(startPosition, startRotation);

        trialStartPosition = startPosition;
        trialStartTime = Time.time;

        platformReached = false;
        headingError = -1f;
        headingErrorRecorded = false;

        ResetProbeTimes();

        trialRunning = true;

        string startName =
            phase == TrialPhase.Probe
                ? "Probe-180deg"
                : startPoint.ToString();

        Debug.Log(
            $"{phase} trial {trialNumber} started. " +
            $"Start: {startName}");

        float timeLimit =
            phase == TrialPhase.Probe
                ? config.ProbeDuration
                : config.TrialTimeLimit;

        while (Time.time - trialStartTime < timeLimit)
        {
            if (phase != TrialPhase.Probe &&
                platformReached)
            {
                break;
            }

            yield return null;
        }

        trialRunning = false;

        float elapsedTime =
            Mathf.Min(
                Time.time - trialStartTime,
                timeLimit);

        bool foundPlatform =
            phase != TrialPhase.Probe &&
            platformReached;

        float pathLength =
            player.GetPathLength();

        float normalizedPathLength =
            pathLength / config.PoolDiameter;

        platformTrigger.SetActive(false);

        if (phase == TrialPhase.Probe)
        {
            LogProbeResult(
                trialNumber,
                elapsedTime,
                normalizedPathLength);
        }
        else
        {
            LogTrialResult(
                phase,
                trialNumber,
                startPoint,
                foundPlatform,
                elapsedTime,
                pathLength,
                normalizedPathLength);
        }

        // Hidden trial'da süre dolduysa hedef konumu öğret.
        if (phase == TrialPhase.Hidden &&
            !foundPlatform)
        {
            yield return GuideToGoal();
        }

        platformTrigger.SetVisible(false);

        player.enabled = false;
    }

    private IEnumerator GuideToGoal()
    {
        Debug.Log(
            $"Hidden trial {currentTrialNumber} timed out. " +
            "Showing goal.");

        platformTrigger.SetActive(false);
        platformTrigger.SetVisible(true);

        Vector3 guidedPosition =
            platformRoot.position;

        guidedPosition.y =
            player.transform.position.y;

        Quaternion guidedRotation =
            QuadrantUtils.GetRotationFacingCenter(
                guidedPosition,
                poolCenter.position);

        player.Teleport(
            guidedPosition,
            guidedRotation);

        player.enabled = false;

        yield return new WaitForSeconds(
            config.GuidedGoalDuration);

        platformTrigger.SetVisible(false);
    }

    private IEnumerator RunBreak()
    {
        player.enabled = false;

        platformTrigger.SetVisible(false);
        platformTrigger.SetActive(false);

        float remaining =
            config.BreakDuration;

        while (remaining > 0f)
        {
            statusUI.ShowBreak(
                remaining);

            remaining -= Time.deltaTime;

            yield return null;
        }

        statusUI.Hide();
    }

    private IEnumerator WaitITI()
    {
        player.enabled = false;

        platformTrigger.SetVisible(false);
        platformTrigger.SetActive(false);

        float remaining =
            config.InterTrialInterval;

        while (remaining > 0f)
        {
            statusUI.ShowInterTrial(
                remaining);

            remaining -= Time.deltaTime;

            yield return null;
        }

        statusUI.Hide();
    }

    private void ConfigurePlatform(
        TrialPhase phase)
    {
        switch (phase)
        {
            case TrialPhase.Exploration:
            case TrialPhase.Visible:
                platformTrigger.SetVisible(true);
                platformTrigger.SetActive(true);
                break;

            case TrialPhase.Hidden:
                platformTrigger.SetVisible(false);
                platformTrigger.SetActive(true);
                break;

            case TrialPhase.Probe:
                platformTrigger.SetVisible(false);
                platformTrigger.SetActive(false);
                break;
        }
    }

    private List<CardinalPoint> CreateShuffledStartDeck()
    {
        List<CardinalPoint> deck = new()
        {
            CardinalPoint.N,
            CardinalPoint.S,
            CardinalPoint.E,
            CardinalPoint.W
        };

        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex =
                Random.Range(i, deck.Count);

            (deck[i], deck[randomIndex]) =
                (deck[randomIndex], deck[i]);
        }

        return deck;
    }

    private void TryRecordHeadingError()
    {
        if (headingErrorRecorded)
            return;

        Vector3 movement =
            player.transform.position -
            trialStartPosition;

        movement.y = 0f;

        if (movement.magnitude <
            config.HeadingCheckDistance)
        {
            return;
        }

        headingError =
            QuadrantUtils.CalculateHeadingError(
                trialStartPosition,
                player.transform.position,
                platformRoot.position);

        headingErrorRecorded = true;

        Debug.Log(
            $"Heading error: {headingError:F2}°");
    }

    private void RecordProbeQuadrantTime()
    {
        MazeQuadrant quadrant =
            QuadrantUtils.GetQuadrant(
                player.transform.position,
                poolCenter.position);

        switch (quadrant)
        {
            case MazeQuadrant.NE:
                probeTimeNE += Time.deltaTime;
                break;

            case MazeQuadrant.NW:
                probeTimeNW += Time.deltaTime;
                break;

            case MazeQuadrant.SW:
                probeTimeSW += Time.deltaTime;
                break;

            case MazeQuadrant.SE:
                probeTimeSE += Time.deltaTime;
                break;
        }
    }

    private void ResetProbeTimes()
    {
        probeTimeNE = 0f;
        probeTimeNW = 0f;
        probeTimeSW = 0f;
        probeTimeSE = 0f;
    }

    private void HandlePlayerReachedPlatform()
    {
        if (!trialRunning)
            return;

        if (currentPhase == TrialPhase.Probe)
            return;

        if (platformReached)
            return;

        platformReached = true;

        audioFeedback.PlaySuccess();
    }

    private void LogTrialResult(
        TrialPhase phase,
        int trialNumber,
        CardinalPoint startPoint,
        bool foundPlatform,
        float latency,
        float pathLength,
        float normalizedPathLength)
    {
        string headingText =
            phase == TrialPhase.Hidden
                ? $"{headingError:F2}°"
                : "N/A";

        Debug.Log(
            "Trial finished\n" +
            $"Phase: {phase}\n" +
            $"Trial: {trialNumber}\n" +
            $"Start: {startPoint}\n" +
            $"Found platform: {foundPlatform}\n" +
            $"Latency: {latency:F2} s\n" +
            $"Path length: {pathLength:F2} m\n" +
            $"Normalized path: {normalizedPathLength:F3}\n" +
            $"Heading error: {headingText}");

        MazeQuadrant platformQuadrant =
            QuadrantUtils.GetQuadrant(
                platformRoot.position,
                poolCenter.position);

        float headingValue =
            phase == TrialPhase.Hidden
                ? headingError
                : -1f;

        dataLogger.LogTrial(
            phase,
            trialNumber,
            startPoint.ToString(),
            platformQuadrant,
            latency,
            normalizedPathLength,
            headingValue,
            foundPlatform);
    }

    private void LogProbeResult(
        int trialNumber,
        float duration,
        float normalizedPathLength)
    {
        float total =
            probeTimeNE +
            probeTimeNW +
            probeTimeSW +
            probeTimeSE;

        if (total <= 0f)
            total = 1f;

        float percentNE =
            probeTimeNE / total * 100f;

        float percentNW =
            probeTimeNW / total * 100f;

        float percentSW =
            probeTimeSW / total * 100f;

        float percentSE =
            probeTimeSE / total * 100f;

        Debug.Log(
            "Probe trial finished\n" +
            $"Trial: {trialNumber}\n" +
            $"Duration: {duration:F2} s\n" +
            $"Normalized path: {normalizedPathLength:F3}\n" +
            $"Time NE: {percentNE:F1}%\n" +
            $"Time NW: {percentNW:F1}%\n" +
            $"Time SW: {percentSW:F1}%\n" +
            $"Time SE: {percentSE:F1}%");

        MazeQuadrant platformQuadrant =
            QuadrantUtils.GetQuadrant(
                platformRoot.position,
                poolCenter.position);

        dataLogger.LogProbe(
            trialNumber,
            platformQuadrant,
            duration,
            normalizedPathLength,
            percentNE,
            percentNW,
            percentSW,
            percentSE);
    }

    private void SyncPlatformPositionWithConfig()
    {
        Vector3 position =
            poolCenter.position +
            config.PlatformPosition;

        position.y =
            platformRoot.position.y;

        platformRoot.position = position;
    }

    private bool ValidateReferences()
    {
        if (config != null &&
            player != null &&
            platformTrigger != null &&
            platformRoot != null &&
            poolCenter != null &&
            dataLogger != null &&
            audioFeedback != null)
        {
            return true;
        }

        Debug.LogError(
            "TrialManager references are incomplete.",
            this);

        return false;
    }

    private IEnumerator ShowTrialIntro(
    TrialPhase phase,
    int trialNumber,
    int totalTrials)
    {
        player.enabled = false;

        platformTrigger.SetVisible(false);
        platformTrigger.SetActive(false);

        statusUI.ShowTrialStart(
            phase,
            trialNumber,
            totalTrials);

        yield return new WaitForSeconds(
            trialIntroDuration);

        statusUI.Hide();
    }
}