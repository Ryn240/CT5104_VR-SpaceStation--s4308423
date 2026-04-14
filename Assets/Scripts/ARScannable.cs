using UnityEngine;

/// <summary>
/// ARScannable
/// -----------
/// Attach to any object that can be scanned.
///
/// RESPONSIBILITIES:
/// - Stores scan data (name, description)
/// - Defines anchor points:
///     • overlayAnchor → hologram position
///     • scanTargetAnchor → beam target position
/// - Handles scan visuals (hologram, X-ray)
/// - Handles highlight (with optional pulsing)
///
/// DESIGN PRINCIPLE:
/// Separate interaction (targeting) from presentation (visuals)
/// </summary>
public class ARScannable : MonoBehaviour
{
    // =========================================================
    // 🔹 SCAN DATA
    // =========================================================

    [Header("Scan Info")]

    public string displayName = "Unknown Object";

    [TextArea]
    public string description = "No data available.";

    // =========================================================
    // 🔹 ANCHORS (CORE SYSTEM)
    // =========================================================

    [Header("Anchors")]

    [Tooltip("Where hologram appears (above object)")]
    public Transform overlayAnchor;

    [Tooltip("Where scanner beam targets (centre mass)")]
    public Transform scanTargetAnchor;

    // =========================================================
    // 🔹 VISUAL ELEMENTS
    // =========================================================

    [Header("Visual Feedback")]

    public GameObject overlayPrefab;
    public GameObject hiddenXRayObject;

    // =========================================================
    // 🔹 SCAN SETTINGS
    // =========================================================

    [Header("Scan Settings")]

    public float requiredScanTime = 1.5f;

    // =========================================================
    // 🔹 HIGHLIGHT SYSTEM
    // =========================================================

    [Header("Highlight")]

    [Tooltip("Renderers to highlight (leave empty = auto-detect)")]
    public Renderer[] highlightRenderers;

    [Tooltip("Highlight emission colour")]
    public Color highlightColor = Color.cyan;

    [Tooltip("Enable pulsing highlight")]
    public bool usePulse = true;

    [Tooltip("Pulse speed")]
    public float pulseSpeed = 2f;

    [Tooltip("Pulse intensity")]
    public float pulseIntensity = 2f;

    private MaterialPropertyBlock propertyBlock;
    private bool isHighlighted = false;

    // =========================================================
    // 🔹 INTERNAL STATE
    // =========================================================

    private GameObject activeOverlayInstance;

    // =========================================================
    // 🔹 INITIALISE
    // =========================================================

    private void Awake()
    {
        // Auto-assign renderers if not set
        if (highlightRenderers == null || highlightRenderers.Length == 0)
        {
            highlightRenderers = GetComponentsInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
    }

    // =========================================================
    // 🔹 UPDATE (FOR PULSE EFFECT)
    // =========================================================

    private void Update()
    {
        if (!isHighlighted || !usePulse) return;

        float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
        float intensity = 1f + (pulse * pulseIntensity);

        ApplyEmission(highlightColor * intensity);
    }

    // =========================================================
    // 🔹 HIGHLIGHT CONTROL
    // =========================================================

    public void SetHighlight(bool state)
    {
        if (isHighlighted == state) return;

        isHighlighted = state;

        if (!state)
        {
            ApplyEmission(Color.black);
        }
    }

    private void ApplyEmission(Color color)
    {
        foreach (Renderer rend in highlightRenderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", color);
            rend.SetPropertyBlock(propertyBlock);
        }
    }

    // =========================================================
    // 🔹 SCAN VISUALS
    // =========================================================

    public void ShowScanVisuals()
    {
        if (hiddenXRayObject != null)
            hiddenXRayObject.SetActive(true);

        if (overlayPrefab != null && activeOverlayInstance == null)
        {
            Transform anchor = overlayAnchor != null ? overlayAnchor : transform;

            activeOverlayInstance = Instantiate(
                overlayPrefab,
                anchor.position,
                anchor.rotation,
                anchor
            );
        }
    }

    public void HideScanVisuals()
    {
        if (hiddenXRayObject != null)
            hiddenXRayObject.SetActive(false);

        if (activeOverlayInstance != null)
            Destroy(activeOverlayInstance);
    }

    // =========================================================
    // 🔹 DEBUG GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (scanTargetAnchor != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(scanTargetAnchor.position, 0.05f);
        }

        if (overlayAnchor != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(overlayAnchor.position, 0.05f);
        }
    }
}