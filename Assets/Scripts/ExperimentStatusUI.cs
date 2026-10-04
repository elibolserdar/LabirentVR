using TMPro;
using UnityEngine;

public sealed class ExperimentStatusUI : MonoBehaviour
{
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private TMP_Text statusTitle;
    [SerializeField] private TMP_Text statusDetail;

    private void Awake()
    {
        Hide();
    }

    public void ShowTrialStart(
        TrialPhase phase,
        int trialNumber,
        int totalTrials)
    {
        statusPanel.SetActive(true);

        statusTitle.text = GetPhaseTitle(phase);

        statusDetail.text =
            phase == TrialPhase.Probe
                ? "Prob denemesi\nHazır olun..."
                : $"Deneme {trialNumber} / {totalTrials}\nHazır olun...";
    }

    public void ShowDualBlockReproductionPrompt(bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "Süre Tahmini";

        string buttonInstruction = useQuestController
            ? "A butonuna basılı tutun."
            : "Space tuşuna basılı tutun.";

        statusDetail.text =
            "BA-BA-BA sesini çıkarın.\\n" +
            buttonInstruction + "\\n" +
            "Bırakınca kayıt tamamlanır.";
    }

    public void ShowDualBlockReproductionProgress(bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "● KAYIT AKTİF ●";

        statusDetail.text =
            "Sesinizi sürdürün.\\n" +
            (useQuestController
                ? "A butonunu bırakınca tamamlanır."
                : "Space tuşunu bırakınca tamamlanır.");
    }

    public void ShowDualBlockReproductionComplete()
    {
        statusPanel.SetActive(true);
        statusTitle.text = "KAYIT TAMAMLANDI";
        statusDetail.text = "Devam ediliyor...";
    }

    public void ShowInterTrial(float remainingSeconds)
    {
        statusPanel.SetActive(true);

        statusTitle.text = "Denemeler Arası";

        statusDetail.text =
            $"Sonraki denemeye " +
            $"{Mathf.CeilToInt(remainingSeconds)} sn";
    }

    public void ShowBreak(float remainingSeconds)
    {
        statusPanel.SetActive(true);

        int totalSeconds =
            Mathf.CeilToInt(remainingSeconds);

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        statusTitle.text = "Ara";

        statusDetail.text =
            $"Lütfen dinlenin\n" +
            $"{minutes:00}:{seconds:00}";
    }

    public void ShowCompleted()
    {
        statusPanel.SetActive(true);

        statusTitle.text =
            "Deney Tamamlandı";

        statusDetail.text =
            "Teşekkür ederiz.";
    }

    public void Hide()
    {
        statusPanel.SetActive(false);
    }

    private static string GetPhaseTitle(
        TrialPhase phase)
    {
        return phase switch
        {
            TrialPhase.Exploration =>
                "Keşif",

            TrialPhase.Hidden =>
                "Gizli Platform",

            TrialPhase.Probe =>
                "Prob Denemesi",

            TrialPhase.DualBlock =>
                "Dual Blok",

            TrialPhase.Visible =>
                "Görünür Platform",

            _ => phase.ToString()
        };
    }
}