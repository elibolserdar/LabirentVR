using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class DataLogger : MonoBehaviour
{
    [Header("Participant - Temporary")]
    [SerializeField] private string participantId = "TEST001";
    [SerializeField] private string group = "HC";

    private string filePath;
    private bool initialized;

    public string FilePath => filePath;

    public void StartSession(
        string newParticipantId,
        string newGroup)
    {
        participantId = newParticipantId.Trim();
        group = newGroup.Trim();

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

        string directory =
            Path.Combine(
                documentsPath,
                "VMWT_Data");

        #else

                string directory =
                    Path.Combine(
                        Application.persistentDataPath,
                        "VMWT_Data");

        #endif

        Directory.CreateDirectory(directory);

        string timestamp =
            DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string safeParticipantId =
            string.IsNullOrWhiteSpace(participantId)
                ? "UNKNOWN"
                : participantId.Trim();

        filePath =
            Path.Combine(
                directory,
                $"{safeParticipantId}_{timestamp}.csv");

        string header =
            "participant_id;" +
            "group;" +
            "phase;" +
            "trial_no;" +
            "start_position;" +
            "platform_quadrant;" +
            "latency_s;" +
            "path_length_norm;" +
            "heading_error_deg;" +
            "found_platform;" +
            "percent_time_NE;" +
            "percent_time_NW;" +
            "percent_time_SW;" +
            "percent_time_SE";

        File.WriteAllText(
            filePath,
            header + Environment.NewLine,
            new UTF8Encoding(true));

        initialized = true;

        Debug.Log(
            $"DataLogger initialized:\n{filePath}");
    }

    public void LogTrial(
        TrialPhase phase,
        int trialNumber,
        string startPosition,
        MazeQuadrant platformQuadrant,
        float latency,
        float normalizedPathLength,
        float headingError,
        bool foundPlatform)
    {
        EnsureInitialized();

        string row = string.Join(";",
            Escape(participantId),
            Escape(group),
            phase.ToString(),
            trialNumber.ToString(),
            startPosition,
            platformQuadrant.ToString(),
            Format(latency),
            Format(normalizedPathLength),
            Format(headingError),
            foundPlatform.ToString(),
            "",
            "",
            "",
            "");

        Append(row);
    }

    public void LogProbe(
        int trialNumber,
        MazeQuadrant platformQuadrant,
        float duration,
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
            TrialPhase.Probe.ToString(),
            trialNumber.ToString(),
            "Probe-180deg",
            platformQuadrant.ToString(),
            Format(duration),
            Format(normalizedPathLength),
            "-1",
            "False",
            Format(percentNE),
            Format(percentNW),
            Format(percentSW),
            Format(percentSE));

        Append(row);
    }

    private void Append(string row)
    {
        File.AppendAllText(
            filePath,
            row + Environment.NewLine);
    }

    private void EnsureInitialized()
    {
        if (!initialized)
            Initialize();
    }

    private static string Format(float value)
    {
        return value.ToString(
            "0.###",
            CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return "\"" +
               value.Replace("\"", "\"\"") +
               "\"";
    }
}