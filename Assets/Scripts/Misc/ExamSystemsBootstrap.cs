using UnityEngine;

/// <summary>
/// Agrupa sistemas del examen (créditos + referencia a cámara de seguridad).
/// Colocar un GameObject "Exam Systems" en MainLevel.
/// </summary>
public class ExamSystemsBootstrap : MonoBehaviour
{
    [SerializeField] ExamCreditsUI examCreditsUI;
    [SerializeField] LabSecurityCamera securityCamera;

    void Awake()
    {
        if (examCreditsUI == null)
            examCreditsUI = GetComponentInChildren<ExamCreditsUI>(true);

        if (securityCamera == null)
            securityCamera = FindFirstObjectByType<LabSecurityCamera>();
    }

    public ExamCreditsUI Credits => examCreditsUI;
    public LabSecurityCamera SecurityCamera => securityCamera;
}
