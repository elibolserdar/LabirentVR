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
    [SerializeField] private CardinalPoint testStartPoint = CardinalPoint.S;
    [SerializeField] private KeyCode startKey = KeyCode.T;

    private bool trialRunning;
    private bool headingErrorRecorded;

    private float trialStartTime;
    private float headingError = -1f;

    private Vector3 trialStartPosition;

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

        TryRecordHeadingError();

        float elapsedTime = Time.time - trialStartTime;

        if (elapsedTime >= config.TrialTimeLimit)
            FinishTrial(foundPlatform: false);
    }

    public void StartTestTrial()
    {
        if (trialRunning)
            return;

        if (!ValidateReferences())
            return;

        Vector3 center = poolCenter.position;

        Vector3 startPosition =
            QuadrantUtils.GetCardinalStartPosition(
                testStartPoint,
                center,
                config.PoolRadius);

        // CharacterController'ın mevcut zemin yüksekliğini koruyoruz.
        startPosition.y = player.transform.position.y;

        Quaternion startRotation =
            QuadrantUtils.GetRotationFacingCenter(
                startPosition,
                center);

        platformTrigger.SetActive(true);
        platformTrigger.SetVisible(true);

        player.Teleport(startPosition, startRotation);

        trialStartPosition = startPosition;
        trialStartTime = Time.time;

        headingError = -1f;
        headingErrorRecorded = false;
        trialRunning = true;

        Debug.Log(
            $"Test trial started from {testStartPoint}. " +
            "Use Up/Left/Right arrows.");
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

    private void HandlePlayerReachedPlatform()
    {
        if (!trialRunning)
            return;

        FinishTrial(foundPlatform: true);
    }

    private void FinishTrial(bool foundPlatform)
    {
        if (!trialRunning)
            return;

        trialRunning = false;
        platformTrigger.SetActive(false);

        float latency =
            Mathf.Min(
                Time.time - trialStartTime,
                config.TrialTimeLimit);

        float pathLength = player.GetPathLength();

        float normalizedPathLength =
            pathLength / config.PoolDiameter;

        Debug.Log(
            "Trial finished\n" +
            $"Found platform: {foundPlatform}\n" +
            $"Start point: {testStartPoint}\n" +
            $"Latency: {latency:F2} s\n" +
            $"Path length: {pathLength:F2} m\n" +
            $"Normalized path: {normalizedPathLength:F3}\n" +
            $"Heading error: {headingError:F2}°");
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