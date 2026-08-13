using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ParticipantIdKeyboard : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject keyboardPanel;

    private static readonly string[] CharacterRows =
    {
        "1234567890",
        "QWERTYUIOP",
        "ASDFGHJKL",
        "ZXCVBNM-"
    };

    private void Awake()
    {
        BuildKeyboard();

        // XR ile InputField'a tıklandığını garanti etmek için
        // click listener'ı otomatik ekliyoruz.
        ParticipantIdInputActivator activator =
            inputField.gameObject.GetComponent<ParticipantIdInputActivator>();

        if (activator == null)
        {
            activator =
                inputField.gameObject.AddComponent<ParticipantIdInputActivator>();
        }

        activator.Initialize(this);

        keyboardPanel.SetActive(false);
    }

    public void ShowKeyboard()
    {
        keyboardPanel.SetActive(true);
    }

    public void AddCharacter(string character)
    {
        inputField.text += character;
        inputField.caretPosition = inputField.text.Length;
    }

    public void Backspace()
    {
        if (string.IsNullOrEmpty(inputField.text))
            return;

        inputField.text =
            inputField.text.Substring(
                0,
                inputField.text.Length - 1);

        inputField.caretPosition = inputField.text.Length;
    }

    public void Clear()
    {
        inputField.text = string.Empty;
    }

    public void Done()
    {
        keyboardPanel.SetActive(false);
    }

    private void BuildKeyboard()
    {
        // Aynı klavyeyi yanlışlıkla iki kez üretmeyelim.
        if (keyboardPanel.transform.Find("GeneratedKeyboard") != null)
            return;

        GameObject root =
            new GameObject(
                "GeneratedKeyboard",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));

        root.transform.SetParent(
            keyboardPanel.transform,
            false);

        RectTransform rootRect =
            root.GetComponent<RectTransform>();

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;

        rootRect.offsetMin =
            new Vector2(12f, 12f);

        rootRect.offsetMax =
            new Vector2(-12f, -12f);

        VerticalLayoutGroup vertical =
            root.GetComponent<VerticalLayoutGroup>();

        vertical.spacing = 6f;
        vertical.padding =
            new RectOffset(0, 0, 0, 0);

        vertical.childAlignment =
            TextAnchor.MiddleCenter;

        vertical.childControlHeight = true;
        vertical.childControlWidth = true;

        vertical.childForceExpandHeight = true;
        vertical.childForceExpandWidth = true;

        foreach (string row in CharacterRows)
        {
            CreateCharacterRow(root.transform, row);
        }

        CreateControlRow(root.transform);
    }

    private void CreateCharacterRow(
        Transform parent,
        string characters)
    {
        GameObject row =
            CreateRow(parent);

        foreach (char character in characters)
        {
            string value =
                character.ToString();

            CreateButton(
                row.transform,
                value,
                () => AddCharacter(value));
        }
    }

    private void CreateControlRow(
        Transform parent)
    {
        GameObject row =
            CreateRow(parent);

        CreateButton(
            row.transform,
            "SİL",
            Backspace);

        CreateButton(
            row.transform,
            "TEMİZLE",
            Clear);

        CreateButton(
            row.transform,
            "TAMAM",
            Done);
    }

    private static GameObject CreateRow(
        Transform parent)
    {
        GameObject row =
            new GameObject(
                "Row",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));

        row.transform.SetParent(
            parent,
            false);

        HorizontalLayoutGroup horizontal =
            row.GetComponent<HorizontalLayoutGroup>();

        horizontal.spacing = 6f;

        horizontal.childAlignment =
            TextAnchor.MiddleCenter;

        horizontal.childControlHeight = true;
        horizontal.childControlWidth = true;

        horizontal.childForceExpandHeight = true;
        horizontal.childForceExpandWidth = true;

        return row;
    }

    private void CreateButton(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject =
            new GameObject(
                $"Key_{label}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));

        buttonObject.transform.SetParent(
            parent,
            false);

        Image image =
            buttonObject.GetComponent<Image>();

        image.color =
            new Color32(
                52,
                60,
                72,
                255);

        Button button =
            buttonObject.GetComponent<Button>();

        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textObject =
            new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        textObject.transform.SetParent(
            buttonObject.transform,
            false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        text.text = label;
        text.fontSize = 26f;
        text.alignment =
            TextAlignmentOptions.Center;

        text.color = Color.white;
        text.raycastTarget = false;
    }
}


// ParticipantIdInput'a XR ray ile tıklandığında
// klavyeyi otomatik açar.
public sealed class ParticipantIdInputActivator :
    MonoBehaviour,
    IPointerClickHandler,
    ISelectHandler
{
    private ParticipantIdKeyboard keyboard;

    public void Initialize(
        ParticipantIdKeyboard participantKeyboard)
    {
        keyboard = participantKeyboard;
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        keyboard?.ShowKeyboard();
    }

    public void OnSelect(
        BaseEventData eventData)
    {
        keyboard?.ShowKeyboard();
    }
}