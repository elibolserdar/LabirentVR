using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExperimentSetupUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject setupPanel;
    [SerializeField] private TMP_InputField participantIdInput;
    [SerializeField] private TMP_Dropdown groupDropdown;
    [SerializeField] private Button startButton;

    [Header("Experiment")]
    [SerializeField] private DataLogger dataLogger;
    [SerializeField] private TrialManager trialManager;
    [SerializeField] private MouseLook mouseLook;

    private void Awake()
    {
        setupPanel.SetActive(true);
        mouseLook.SetLookEnabled(false);

        startButton.onClick.AddListener(
            HandleStartClicked);
    }

    private void OnDestroy()
    {
        startButton.onClick.RemoveListener(
            HandleStartClicked);
    }

    private void HandleStartClicked()
    {
        string participantId =
            participantIdInput.text.Trim();

        if (string.IsNullOrWhiteSpace(participantId))
        {
            Debug.LogWarning(
                "Participant ID cannot be empty.");

            return;
        }

        string group =
            groupDropdown.options[
                groupDropdown.value].text;

        dataLogger.StartSession(
            participantId,
            group);

        setupPanel.SetActive(false);

        mouseLook.SetLookEnabled(true);

        trialManager.StartExperiment();
    }
}