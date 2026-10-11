using TMPro;
using UnityEngine;

public sealed class ExperimentStatusUI : MonoBehaviour
{
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private TMP_Text statusTitle;
    [SerializeField] private TMP_Text statusDetail;

    private void Awake()
    {
        ConfigureTextLayout();
        Hide();
    }

    private void ConfigureTextLayout()
    {
        // Uzun faz yönergelerinin başlıkla çakışmasını önle.
        RectTransform titleRect = statusTitle.rectTransform;
        titleRect.anchoredPosition = new Vector2(0f, 180f);
        titleRect.sizeDelta = new Vector2(700f, 80f);

        // Açıklama alanını büyüt ve metni kutuya sığdır.
        RectTransform detailRect = statusDetail.rectTransform;
        detailRect.anchoredPosition = new Vector2(0f, -50f);
        detailRect.sizeDelta = new Vector2(700f, 320f);

        statusDetail.enableAutoSizing = true;
        statusDetail.fontSize = 28f;
        statusDetail.fontSizeMin = 20f;
        statusDetail.fontSizeMax = 28f;
        statusDetail.verticalAlignment = VerticalAlignmentOptions.Middle;
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

    public void ShowPhaseInstruction(
        TrialPhase phase,
        bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = GetPhaseTitle(phase);

        string instruction = phase switch
        {
            TrialPhase.Exploration =>
                "Platform içindeki hedefi görmektesiniz. Hareket kontrollerini kullanarak hedefe ulaşmaya çalışın. Bu aşamada ortamı keşfedin ve hareket kontrollerini öğrenin.",

            TrialPhase.Hidden =>
                "Hedef, ortamda sabit bir konumda bulunacak ancak görünmeyecektir. Her denemede farklı bir başlangıç konumundan başlayarak çevresel ipuçlarından yararlanın ve gizli hedefi bulmaya çalışın.",

            TrialPhase.Probe =>
                "Gizli hedefi bulmaya çalışmaya devam edin.",

            TrialPhase.DualBlock =>
                "Şimdi yine hedefi bulmaya çalışın. Hedefi bulduğunuzda geçen süre kadar A tuşuna basın. Tuşa basarken -bababa şeklinde içinizden söyleyin. Test süresince sürenin ne kadar geçtiğini içinizden saymaya çalışmayın, yalnızca mevcut görevinize odaklanın.",

            TrialPhase.Visible =>
                "Son bölümdesiniz. Şimdi havuzda görünen bir hedef göreceksiniz. Her denemede mümkün olan en hızlı ve en kısa yoldan platforma ulaşmaya çalışın.",

            _ => string.Empty
        };

        string advanceInstruction = useQuestController
            ? "Yönergeyi okuduktan sonra devam etmek için A butonuna basın."
            : "Yönergeyi okuduktan sonra devam etmek için Enter tuşuna basın.";

        statusDetail.text = instruction + "\n\n" + advanceInstruction;
    }

    public void ShowPhaseTransition(
        string completedPhase,
        string nextPhase,
        bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "Bölüm Geçişi";

        string advanceInstruction = useQuestController
            ? "Devam etmek için A butonuna basın."
            : "Devam etmek için Enter tuşuna basın.";

        statusDetail.text =
            $"{completedPhase} bölümü tamamlandı.\n" +
            $"Sıradaki bölüm: {nextPhase}.\n" +
            advanceInstruction;
    }

    public void ShowDualBlockReproductionPrompt(bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "Süre Tahmini";

        string buttonInstruction = useQuestController
            ? "A butonuna basılı tutun."
            : "Space tuşuna basılı tutun.";

        statusDetail.text =
            "BA-BA-BA sesini çıkarın.\n" +
            buttonInstruction + "\n" +
            "Bırakınca kayıt tamamlanır.";
    }

    public void ShowDualBlockReproductionProgress(bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "● KAYIT AKTİF ●";

        statusDetail.text =
            "Sesinizi sürdürün.\n" +
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

    public void ShowContinuePrompt(bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "Devam Etmeye Hazır";

        statusDetail.text = useQuestController
            ? "Geri sayım tamamlandı.\nSonraki denemeye geçmek için A butonuna basın."
            : "Geri sayım tamamlandı.\nSonraki denemeye geçmek için Enter tuşuna basın.";
    }

    public void ShowBreakContinuePrompt(bool useQuestController)
    {
        statusPanel.SetActive(true);
        statusTitle.text = "Ara Tamamlandı";

        statusDetail.text = useQuestController
            ? "Hazır olduğunuzda devam etmek için A butonuna basın."
            : "Hazır olduğunuzda devam etmek için Enter tuşuna basın.";
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
            $"Lütfen dinlenin.\n" +
            $"Bir sonraki bölüm başlamadan önce hazır olduğunuzdan emin olun.\n" +
            $"{minutes:00}:{seconds:00}";
    }

    public void ShowCompleted()
    {
        statusPanel.SetActive(true);

        statusTitle.text =
            "Deney Tamamlandı";

        statusDetail.text =
            "Deney tamamlandı. Katılımınız için teşekkür ederiz. " +
            "Lütfen araştırmacının yönlendirmesini bekleyin.";
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
                "Gizli Hedef",

            TrialPhase.Probe =>
                "Prob Denemesi",

            TrialPhase.DualBlock =>
                "Dual",

            TrialPhase.Visible =>
                "Görünür Platform",

            _ => phase.ToString()
        };
    }
}