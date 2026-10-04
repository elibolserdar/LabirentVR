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
            ? "A butonunu basılı tutun."
            : "Space tuşunu basılı tutun.";

        statusDetail.text =
            "BA-BA-BA diye ses çıkarmaya başlayın.\n" +
            buttonInstruction + "\n" +
            "Sürenin dolduğunu düşündüğünüzde bırakın.";
    }

    public void ShowDualBlockReproductionProgress(
        float elapsedSeconds,
        bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "● KAYIT AKTİF ●";

        string buttonInstruction = useQuestController
            ? "A butonunu bırakınca ölçüm tamamlanır."
            : "Space tuşunu bırakınca ölçüm tamamlanır.";

        statusDetail.text =
            "BA-BA-BA sesini sürdürün.\n\n" +
            $"{elapsedSeconds:F1} saniye\n\n" +
            buttonInstruction;
    }

    public void ShowDualBlockReproductionComplete(float elapsedSeconds)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "SÜRE KAYDEDİLDİ";
        statusDetail.text =
            $"Tahmininiz: {elapsedSeconds:F1} saniye\n" +
            "Sonraki aşamaya geçiliyor...";
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