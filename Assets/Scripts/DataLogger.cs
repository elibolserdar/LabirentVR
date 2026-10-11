using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class DataLogger : MonoBehaviour
{
    [Header("Participant")]
    [SerializeField] private string participantId = "TEST001";
    [SerializeField] private string group = "HC";
    [SerializeField] private string sessionLabel = "Baseline";

    private string sessionDirectory;
    private string filePath;
    private string pathPointsFilePath;
    private string configFilePath;
    private bool initialized;

    public string FilePath => filePath;
    public string PathPointsFilePath => pathPointsFilePath;
    public string ConfigFilePath => configFilePath;
    public string SessionDirectory => sessionDirectory;
    public string SessionLabel => sessionLabel;

    public void StartSession(
        string newParticipantId,
        string newGroup,
        string newSessionLabel = "Baseline")
    {
        participantId = newParticipantId.Trim();
        group = newGroup.Trim();
        sessionLabel = string.IsNullOrWhiteSpace(newSessionLabel)
            ? "Baseline"
            : newSessionLabel.Trim();

        initialized = false;
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string documentsPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);

        string rootDirectory =
            Path.Combine(
                documentsPath,
                "VMWT_Data");
#else
        string rootDirectory =
            Path.Combine(
                Application.persistentDataPath,
                "VMWT_Data");
#endif

        string safeParticipantId =
            MakeSafeFileNamePart(participantId, "UNKNOWN");

        string safeSessionLabel =
            MakeSafeFileNamePart(sessionLabel, "Session");

        DateTime now = DateTime.Now;
        string dateFolder = now.ToString("yyyy-MM-dd");
        string timeFolder = now.ToString("HHmmss_fff");

        // Her oturum ayrı klasöre yazılır; yeni kayıtlar eski CSV'leri ezmez.
        sessionDirectory = Path.Combine(
            rootDirectory,
            safeParticipantId,
            safeSessionLabel,
            dateFolder,
            timeFolder);

        Directory.CreateDirectory(sessionDirectory);

        filePath = Path.Combine(sessionDirectory, "trials.csv");
        pathPointsFilePath = Path.Combine(sessionDirectory, "path_points.csv");
        configFilePath = Path.Combine(sessionDirectory, "experiment_config.csv");

        string trialsHeader =
            "participant_id;" +
            "group;" +
            "session;" +
            "phase;" +
            "trial_no;" +
            "start_position;" +
            "platform_quadrant;" +
            "platform_relative_x_m;" +
            "platform_relative_z_m;" +
            "latency_s;" +
            "path_length_m;" +
            "path_length_norm;" +
            "heading_error_deg;" +
            "found_platform;" +
            "percent_time_NE;" +
            "percent_time_NW;" +
            "percent_time_SW;" +
            "percent_time_SE;" +
            "reproduced_time_s";

        string pathPointsHeader =
            "participant_id;" +
            "group;" +
            "session;" +
            "phase;" +
            "trial_no;" +
            "sample_no;" +
            "time_s;" +
            "world_x_m;" +
            "world_y_m;" +
            "world_z_m;" +
            "pool_relative_x_m;" +
            "pool_relative_z_m";

        var utf8WithBom = new UTF8Encoding(true);

        File.WriteAllText(
            filePath,
            trialsHeader + Environment.NewLine,
            utf8WithBom);

        File.WriteAllText(
            pathPointsFilePath,
            pathPointsHeader + Environment.NewLine,
            utf8WithBom);

        initialized = true;

        Debug.Log(
            "DataLogger initialized.\n" +
            "Session folder: " + sessionDirectory + "\n" +
            "Trial results: " + filePath + "\n" +
            "Path points: " + pathPointsFilePath + "\n" +
            "Session: " + sessionLabel);
    }

    public void LogExperimentConfig(
        MazeConfig config,
        Vector3 poolCenterPosition,
        Vector3 platformRelativePosition)
    {
        EnsureInitialized();

        if (config == null)
        {
            Debug.LogWarning("Cannot save experiment settings: MazeConfig is null.");
            return;
        }

        string header = "setting;value";
        var rows = new List<string>
        {
            header,
            ConfigRow("participant_id", participantId),
            ConfigRow("group", group),
            ConfigRow("session", sessionLabel),
            ConfigRow("recorded_at_local", DateTime.Now.ToString("O", CultureInfo.InvariantCulture)),
            ConfigRow("unity_version", Application.unityVersion),
            ConfigRow("platform", Application.platform.ToString()),
            ConfigRow("pool_diameter_m", Format(config.PoolDiameter)),
            ConfigRow("pool_radius_m", Format(config.PoolRadius)),
            ConfigRow("room_size_m", Format(config.RoomSize)),
            ConfigRow("platform_diameter_m", Format(config.PlatformDiameter)),
            ConfigRow("platform_relative_x_m", Format(platformRelativePosition.x)),
            ConfigRow("platform_relative_z_m", Format(platformRelativePosition.z)),
            ConfigRow("pool_center_world_x_m", Format(poolCenterPosition.x)),
            ConfigRow("pool_center_world_y_m", Format(poolCenterPosition.y)),
            ConfigRow("pool_center_world_z_m", Format(poolCenterPosition.z)),
            ConfigRow("exploration_trial_count", config.ExplorationTrialCount.ToString(CultureInfo.InvariantCulture)),
            ConfigRow("hidden_trial_count", config.HiddenTrialCount.ToString(CultureInfo.InvariantCulture)),
            ConfigRow("dual_block_trial_count", config.DualBlockTrialCount.ToString(CultureInfo.InvariantCulture)),
            ConfigRow("visible_trial_count", config.VisibleTrialCount.ToString(CultureInfo.InvariantCulture)),
            ConfigRow("trial_time_limit_s", Format(config.TrialTimeLimit)),
            ConfigRow("probe_duration_s", Format(config.ProbeDuration)),
            ConfigRow("inter_trial_interval_s", Format(config.InterTrialInterval)),
            ConfigRow("break_duration_s", Format(config.BreakDuration)),
            ConfigRow("guided_goal_duration_s", Format(config.GuidedGoalDuration)),
            ConfigRow("max_forward_speed_m_s", Format(config.MaxForwardSpeed)),
            ConfigRow("turn_speed_deg_s", Format(config.TurnSpeed)),
            ConfigRow("acceleration_time_s", Format(config.AccelerationTime)),
            ConfigRow("input_deadzone", Format(config.InputDeadzone)),
            ConfigRow("heading_check_distance_m", Format(config.HeadingCheckDistance))
        };

        File.WriteAllText(
            configFilePath,
            string.Join(Environment.NewLine, rows) + Environment.NewLine,
            new UTF8Encoding(true));

        Debug.Log("Experiment settings saved: " + configFilePath);
    }

    public void LogTrial(
        TrialPhase phase,
        int trialNumber,
        string startPosition,
        MazeQuadrant platformQuadrant,
        Vector3 platformRelativePosition,
        float latency,
        float pathLength,
        float normalizedPathLength,
        float headingError,
        bool foundPlatform,
        float reproducedTime = -1f)
    {
        EnsureInitialized();

        string row = string.Join(";",
            Escape(participantId),
            Escape(group),
            Escape(sessionLabel),
            phase.ToString(),
            trialNumber.ToString(CultureInfo.InvariantCulture),
            Escape(startPosition),
            platformQuadrant.ToString(),
            Format(platformRelativePosition.x),
            Format(platformRelativePosition.z),
            Format(latency),
            Format(pathLength),
            Format(normalizedPathLength),
            Format(headingError),
            foundPlatform.ToString(),
            "",
            "",
            "",
            "",
            reproducedTime >= 0f ? Format(reproducedTime) : "");

        Append(filePath, row);
    }

    public void LogProbe(
        int trialNumber,
        MazeQuadrant platformQuadrant,
        Vector3 platformRelativePosition,
        float duration,
        float pathLength,
        float normalizedPathLength,
        float percentNE,
        float percentNW,
        float percentSW,
        float percentSE)
    {
        EnsureInitialized();

        string row = string.Join(";",
            Escape(participantId),
            Escape(group),
            Escape(sessionLabel),
            TrialPhase.Probe.ToString(),
            trialNumber.ToString(CultureInfo.InvariantCulture),
            "Probe-180deg",
            platformQuadrant.ToString(),
            Format(platformRelativePosition.x),
            Format(platformRelativePosition.z),
            Format(duration),
            Format(pathLength),
            Format(normalizedPathLength),
            "-1",
            "False",
            Format(percentNE),
            Format(percentNW),
            Format(percentSW),
            Format(percentSE),
            "");

        Append(filePath, row);
    }

    public void LogPathPoints(
        TrialPhase phase,
        int trialNumber,
        Vector3 poolCenter,
        IReadOnlyList<Vector3> pathPoints,
        IReadOnlyList<float> pathTimes)
    {
        EnsureInitialized();

        if (pathPoints == null || pathPoints.Count == 0)
        {
            Debug.LogWarning(
                "No path points recorded for " + phase +
                ", trial " + trialNumber + ".");
            return;
        }

        var builder = new StringBuilder();

        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 worldPosition = pathPoints[i];
            Vector3 relativePosition = worldPosition - poolCenter;

            float elapsedSeconds =
                pathTimes != null && i < pathTimes.Count
                    ? pathTimes[i]
                    : 0f;

            string row = string.Join(";",
                Escape(participantId),
                Escape(group),
                Escape(sessionLabel),
                phase.ToString(),
                trialNumber.ToString(CultureInfo.InvariantCulture),
                i.ToString(CultureInfo.InvariantCulture),
                Format(elapsedSeconds),
                Format(worldPosition.x),
                Format(worldPosition.y),
                Format(worldPosition.z),
                Format(relativePosition.x),
                Format(relativePosition.z));

            builder.AppendLine(row);
        }

        File.AppendAllText(
            pathPointsFilePath,
            builder.ToString(),
            new UTF8Encoding(false));
    }

    private static string ConfigRow(string key, string value)
    {
        return Escape(key) + ";" + Escape(value);
    }

    private static void Append(string targetPath, string row)
    {
        File.AppendAllText(targetPath, row + Environment.NewLine);
    }

    private void EnsureInitialized()
    {
        if (!initialized)
            Initialize();
    }

    private static string MakeSafeFileNamePart(
        string value,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();

        foreach (char character in value.Trim())
        {
            if (Array.IndexOf(invalidCharacters, character) >= 0 ||
                char.IsWhiteSpace(character))
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.Length > 0 ? builder.ToString() : fallback;
    }

    private static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}