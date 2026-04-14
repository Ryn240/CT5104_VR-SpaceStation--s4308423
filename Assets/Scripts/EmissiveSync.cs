using UnityEngine;

/// <summary>
/// EMISSIVE SYNC
/// -------------
/// Syncs a material's emission with the LightingManager.
/// 
/// Supports:
/// - Emergency baseline glow (lights OFF but not fully dark)
/// - Smooth fade to full brightness
/// 
/// Attach to any object with an emissive material.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class EmissiveSync : MonoBehaviour
{
    private Renderer rend;
    private MaterialPropertyBlock mpb;

    [Header("Emission Settings")]

    // Full brightness colour (normal powered state)
    [SerializeField] private Color fullEmissionColor = Color.white;

    // Low-level "emergency lighting"
    [SerializeField] private Color emergencyEmissionColor = new Color(0.1f, 0.1f, 0.1f);

    // Intensity multipliers
    [SerializeField] private float fullIntensity = 2f;
    [SerializeField] private float emergencyIntensity = 0.05f;

    private float currentLerp = 0f; // 0 = emergency, 1 = full

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Register with LightingManager
        if (LightingManager.Instance != null)
        {
            LightingManager.Instance.RegisterEmissive(this);
        }

        // Start in emergency mode
        ApplyEmission(0f);
    }

    /// <summary>
    /// Called by LightingManager during fades
    /// </summary>
    public void SetEmissionLevel(float t)
    {
        currentLerp = t;
        ApplyEmission(t);
    }

    private void ApplyEmission(float t)
    {
        // Blend between emergency and full
        Color finalColor = Color.Lerp(
            emergencyEmissionColor * emergencyIntensity,
            fullEmissionColor * fullIntensity,
            t
        );

        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_EmissionColor", finalColor);
        rend.SetPropertyBlock(mpb);
    }
}