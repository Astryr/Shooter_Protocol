using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de créditos obligatoria para el examen de Programación Gráfica.
/// Editar apellidos en el Inspector o en el componente del objeto Exam Systems.
/// </summary>
public class ExamCreditsUI : MonoBehaviour
{
    [TextArea(1, 8)]
    [SerializeField] string[] participantSurnames =
    {
        "Apellido Integrante 1",
        "Apellido Integrante 2",
        "Apellido Integrante 3",
        "Apellido Integrante 4"
    };

    [SerializeField] string materiaLine = "Programación Gráfica — 2do Parcial";
    [SerializeField] string proyectoLine = "Shooter Protocol";

    GameObject creditsRoot;

    public void ToggleCredits()
    {
        if (creditsRoot == null)
            BuildCreditsUI();

        creditsRoot.SetActive(!creditsRoot.activeSelf);
    }

    public void ShowCredits(bool show)
    {
        if (creditsRoot == null)
            BuildCreditsUI();

        creditsRoot.SetActive(show);
    }

    void BuildCreditsUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        creditsRoot = new GameObject("Credits Overlay");
        creditsRoot.transform.SetParent(parent, false);

        RectTransform panelRect = creditsRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image overlay = creditsRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 1f);

        GameObject textObject = new GameObject("Credits Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(creditsRoot.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(900f, 520f);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 32;
        tmp.color = new Color(0.75f, 0.95f, 1f, 1f);
        tmp.text = BuildCreditsText();

        GameObject closeButton = new GameObject("Close Credits", typeof(RectTransform), typeof(Image), typeof(Button));
        closeButton.transform.SetParent(creditsRoot.transform, false);

        RectTransform btnRect = closeButton.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 48f);
        btnRect.sizeDelta = new Vector2(280f, 56f);

        Image btnImage = closeButton.GetComponent<Image>();
        btnImage.color = new Color(0.12f, 0.22f, 0.32f, 0.95f);

        Button button = closeButton.GetComponent<Button>();
        button.targetGraphic = btnImage;
        button.onClick.AddListener(() => creditsRoot.SetActive(false));

        GameObject btnLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnLabel.transform.SetParent(closeButton.transform, false);
        RectTransform labelRect = btnLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTmp = btnLabel.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "Cerrar";
        labelTmp.fontSize = 24;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = Color.white;

        creditsRoot.SetActive(false);
    }

    string BuildCreditsText()
    {
        var lines = new System.Text.StringBuilder();
        lines.AppendLine("<b>CRÉDITOS</b>");
        lines.AppendLine();
        lines.AppendLine(proyectoLine);
        lines.AppendLine(materiaLine);
        lines.AppendLine();
        lines.AppendLine("<b>Integrantes</b>");

        for (int i = 0; i < participantSurnames.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(participantSurnames[i]))
                lines.AppendLine("• " + participantSurnames[i]);
        }

        return lines.ToString();
    }
}
