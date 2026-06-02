using UnityEngine;

/// <summary>
/// Render Texture obligatoria del examen: cámara secundaria que dibuja en un RT
/// mostrado en un monitor del laboratorio (RawImage o material Unlit).
/// </summary>
[RequireComponent(typeof(Camera))]
public class LabSecurityCamera : MonoBehaviour
{
    [SerializeField] RenderTexture securityRenderTexture;
    [SerializeField] Transform lookAtTarget;
    [SerializeField] Vector3 lookAtOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] bool findPlayerOnStart = true;

    Camera securityCamera;

    void Awake()
    {
        securityCamera = GetComponent<Camera>();
        if (securityRenderTexture != null)
            securityCamera.targetTexture = securityRenderTexture;

        securityCamera.depth = -10;
        securityCamera.enabled = true;
    }

    void Start()
    {
        if (lookAtTarget == null && findPlayerOnStart)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                lookAtTarget = player.transform;
        }
    }

    void LateUpdate()
    {
        if (lookAtTarget == null)
            return;

        Vector3 target = lookAtTarget.position + lookAtOffset;
        transform.LookAt(target);
    }

    public RenderTexture GetRenderTexture() => securityRenderTexture;
}
