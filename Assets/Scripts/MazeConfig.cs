using UnityEngine;

[CreateAssetMenu(
    fileName = "VMWT_DefaultConfig",
    menuName = "VMWT/Maze Config")]
public sealed class MazeConfig : ScriptableObject
{
    [Header("Environment")]
    [SerializeField, Min(1f)]
    private float poolDiameter = 10f;

    [SerializeField, Min(1f)]
    private float roomSize = 20f;

    [SerializeField, Range(1.2f, 1.5f)]
    private float platformDiameter = 1.3f;

    [Tooltip("X ve Z koordinatları")]
    [SerializeField]
    private Vector2 platformPositionXZ = new(1.75f, 1.75f);

    [SerializeField, Range(0.1f, 1f)]
    private float headingErrorDistanceRatio = 0.25f;

    [Header("Trial Counts")]
    [SerializeField, Min(1)]
    private int explorationTrialCount = 4;

    [SerializeField, Min(1)]
    private int hiddenTrialCount = 20;

    [SerializeField, Min(1)]
    private int visibleTrialCount = 10;

    [Header("Trial Durations - Seconds")]
    [SerializeField, Min(1f)]
    private float trialTimeLimit = 60f;

    [SerializeField, Min(1f)]
    private float probeDuration = 60f;

    [SerializeField, Min(0f)]
    private float interTrialInterval = 3f;

    [SerializeField, Min(0f)]
    private float breakDuration = 300f;

    [SerializeField, Min(0f)]
    private float guidedGoalDuration = 2f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float maxForwardSpeed = 1.1f;

    [SerializeField, Min(1f)]
    private float turnSpeed = 50f;

    [SerializeField, Min(0.01f)]
    private float accelerationTime = 0.4f;

    [SerializeField, Range(0f, 0.9f)]
    private float inputDeadzone = 0.15f;

    [Header("Comfort Vignette")]
    [SerializeField, Range(0f, 1f)]
    private float vignetteMaxAlpha = 0.6f;

    [SerializeField, Min(0.01f)]
    private float vignetteSmoothTime = 0.15f;

    public float PoolDiameter => poolDiameter;
    public float PoolRadius => poolDiameter * 0.5f;
    public float RoomSize => roomSize;
    public float PlatformDiameter => platformDiameter;

    public Vector3 PlatformPosition =>
        new(platformPositionXZ.x, 0f, platformPositionXZ.y);

    public float HeadingCheckDistance =>
        poolDiameter * headingErrorDistanceRatio;

    public int ExplorationTrialCount => explorationTrialCount;
    public int HiddenTrialCount => hiddenTrialCount;
    public int VisibleTrialCount => visibleTrialCount;

    public float TrialTimeLimit => trialTimeLimit;
    public float ProbeDuration => probeDuration;
    public float InterTrialInterval => interTrialInterval;
    public float BreakDuration => breakDuration;
    public float GuidedGoalDuration => guidedGoalDuration;

    public float MaxForwardSpeed => maxForwardSpeed;
    public float TurnSpeed => turnSpeed;
    public float AccelerationTime => accelerationTime;
    public float InputDeadzone => inputDeadzone;

    public float VignetteMaxAlpha => vignetteMaxAlpha;
    public float VignetteSmoothTime => vignetteSmoothTime;
}