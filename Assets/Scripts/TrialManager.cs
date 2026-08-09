using UnityEngine;

public sealed class TrialManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MazeConfig config;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlatformTrigger platformTrigger;
    [SerializeField] private Transform platformRoot;
    [SerializeField] private Transform poolCenter;

    [Header("Prototype Test")]
    [SerializeField] private TrialPhase testPhase = TrialPhase.Hidden;
    [SerializeField] private CardinalPoint testStartPoint = CardinalPoint.S;
    [SerializeField] private KeyCode startKey = KeyCode.T;

    private bool trialRunning;
    private bool headingErrorRecorded;

    private float trialStartTime;
    private float headingError = -1f;

    private Vector3 trialStartPosition;

    private float probeTimeNE;
    private float probeTimeNW;
    private float probeTimeSW;
    private float probeTimeSE;

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
        if (!trialRunning)
        {
            if (Input.GetKeyDown(startKey))
                StartTestTrial();

            return;
        }

        if (testPhase == TrialPhase.Hidden)
            TryRecordHeadingError();

        if (testPhase == TrialPhase.Probe)
            RecordProbeQuadrantTime();

        float elapsedTime = Time.time - trialStartTime;

        float timeLimit =
            testPhase == TrialPhase.Probe
                ? config.ProbeDuration
                : config.TrialTimeLimit;

        if (elapsedTime >= timeLimit)
            FinishTrial(foundPlatform: false);
    }

    public void StartTestTrial()
    {
        if (trialRunning)
            return;

        if (!ValidateReferences())
            return;

        Vector3 center = poolCenter.position;

        Vector3 startPosition;

        if (testPhase == TrialPhase.Probe)
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
                    testStartPoint,
                    center,
                    config.PoolRadius);
        }

        // Player'ın mevcut zemin yüksekliğini koru.
        startPosition.y = player.transform.position.y;

        Quaternion startRotation =
            QuadrantUtils.GetRotationFacingCenter(
                startPosition,
                center);

        ConfigurePlatform(testPhase);

        player.Teleport(startPosition, startRotation);

        trialStartPosition = startPosition;
        trialStartTime = Time.time;

        headingError = -1f;
        headingErrorRecorded = false;

        ResetProbeTimes();

        trialRunning = true;

        Debug.Log(
            $"{testPhase} trial started. " +
            $"Start position: {startPosition}");
    }

    private void ConfigurePlatform(TrialPhase phase)
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

    private void TryRecordHeadingError()
    {
        if (headingErrorRecorded)
            return;

        Vector3 movement =
            player.transform.position - trialStartPosition;

        movement.y = 0f;

        if (movement.magnitude < config.HeadingCheckDistance)
            return;

        headingError =
            QuadrantUtils.CalculateHeadingError(
                trialStartPosition,
                player.transform.position,
                platformRoot.position);

        headingErrorRecorded = true;

        Debug.Log(
            $"Heading error recorded: {headingError:F2} degrees.");
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

        // Probe'da platform bulunamaz.
        if (testPhase == TrialPhase.Probe)
            return;

        FinishTrial(foundPlatform: true);
    }

    private void FinishTrial(bool foundPlatform)
    {
        if (!trialRunning)
            return;

        trialRunning = false;
        platformTrigger.SetActive(false);

        float timeLimit =
            testPhase == TrialPhase.Probe
                ? config.ProbeDuration
                : config.TrialTimeLimit;

        float latency =
            Mathf.Min(
                Time.time - trialStartTime,
                timeLimit);

        float pathLength =
            player.GetPathLength();

        float normalizedPathLength =
            pathLength / config.PoolDiameter;

        if (testPhase == TrialPhase.Probe)
        {
            LogProbeResult(
                latency,
                normalizedPathLength);

            return;
        }

        Debug.Log(
            "Trial finished\n" +
            $"Phase: {testPhase}\n" +
            $"Found platform: {foundPlatform}\n" +
            $"Start point: {testStartPoint}\n" +
            $"Latency: {latency:F2} s\n" +
            $"Path length: {pathLength:F2} m\n" +
            $"Normalized path: {normalizedPathLength:F3}\n" +
            $"Heading error: {headingError:F2}°");
    }

    private void LogProbeResult(
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

        float percentNE = probeTimeNE / total * 100f;
        float percentNW = probeTimeNW / total * 100f;
        float percentSW = probeTimeSW / total * 100f;
        float percentSE = probeTimeSE / total * 100f;

        Debug.Log(
            "Probe trial finished\n" +
            $"Duration: {duration:F2} s\n" +
            $"Normalized path: {normalizedPathLength:F3}\n" +
            $"Time NE: {percentNE:F1}%\n" +
            $"Time NW: {percentNW:F1}%\n" +
            $"Time SW: {percentSW:F1}%\n" +
            $"Time SE: {percentSE:F1}%");
    }

    private bool ValidateReferences()
    {
        if (config != null &&
            player != null &&
            platformTrigger != null &&
            platformRoot != null &&
            poolCenter != null)
        {
            return true;
        }

        Debug.LogError(
            "TrialManager references are incomplete.",
            this);

        return false;
    }
}