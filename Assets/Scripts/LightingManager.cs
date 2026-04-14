using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LIGHTING MANAGER (GLOBAL SYSTEM)
/// --------------------------------
/// Controls:
/// - All Light components (registered automatically)
/// - All EmissiveSync components (for material glow)
///
/// Features:
/// - Start in darkness (no manual setup required)
/// - Emergency emissive baseline (scene never fully dead)
/// - Instant ON/OFF
/// - Smooth fade transitions
/// - UnityEvent-compatible API (no parameters needed)
///
/// Architecture:
/// Trigger → LightingManager → Lights + Emissives
/// </summary>
public class LightingManager : MonoBehaviour
{
    // =========================================================
    // 🔹 SINGLETON (global access point)
    // =========================================================

    public static LightingManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Duplicate LightingManager found. Destroying this instance.");
            Destroy(gameObject);
        }
    }

    // =========================================================
    // 🔹 INSPECTOR SETTINGS
    // =========================================================

    [Header("Initial State")]
    [SerializeField] private bool startInDarkness = true;

    [Header("Fade Settings")]
    [SerializeField] private float defaultFadeDuration = 2f;

    // =========================================================
    // 🔹 INTERNAL STORAGE
    // =========================================================

    private List<Light> lights = new List<Light>();
    private Dictionary<Light, float> originalIntensity = new Dictionary<Light, float>();

    private List<EmissiveSync> emissives = new List<EmissiveSync>();

    // =========================================================
    // 🔹 REGISTRATION
    // =========================================================

    /// <summary>
    /// Called by LightAutoRegister
    /// </summary>
    public void RegisterLight(Light l)
    {
        if (l == null) return;

        if (!lights.Contains(l))
        {
            lights.Add(l);

            if (!originalIntensity.ContainsKey(l))
            {
                originalIntensity.Add(l, l.intensity);
            }
        }
    }

    /// <summary>
    /// Called by EmissiveSync
    /// </summary>
    public void RegisterEmissive(EmissiveSync e)
    {
        if (e == null) return;

        if (!emissives.Contains(e))
        {
            emissives.Add(e);
        }
    }

    // =========================================================
    // 🔹 INITIAL STATE (IMPORTANT)
    // =========================================================

    private void Start()
    {
        if (startInDarkness)
        {
            StartCoroutine(InitialiseDarkness());
        }
    }

    private IEnumerator InitialiseDarkness()
    {
        // Wait one frame so all objects register first
        yield return null;

        TurnLightsOff();
    }

    // =========================================================
    // 🔹 PUBLIC API (UNITYEVENT SAFE)
    // =========================================================

    /// <summary>
    /// Instant ON
    /// </summary>
    public void TurnLightsOn()
    {
        StopAllCoroutines();

        foreach (Light l in lights)
        {
            if (l == null) continue;

            l.enabled = true;
            l.intensity = originalIntensity[l];
        }

        // Emissives → full power
        foreach (var e in emissives)
        {
            if (e != null) e.SetEmissionLevel(1f);
        }
    }

    /// <summary>
    /// Instant OFF (but emissives stay at emergency level)
    /// </summary>
    public void TurnLightsOff()
    {
        StopAllCoroutines();

        foreach (Light l in lights)
        {
            if (l == null) continue;

            l.enabled = false;
        }

        // Emissives → emergency baseline
        foreach (var e in emissives)
        {
            if (e != null) e.SetEmissionLevel(0f);
        }
    }

    /// <summary>
    /// Fade ON (default duration)
    /// </summary>
    public void FadeLightsOn()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(true, defaultFadeDuration));
    }

    /// <summary>
    /// Fade OFF
    /// </summary>
    public void FadeLightsOff()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(false, defaultFadeDuration));
    }

    // =========================================================
    // 🔹 CORE FADE SYSTEM
    // =========================================================

    private IEnumerator FadeRoutine(bool turnOn, float duration)
    {
        float timer = 0f;

        // Ensure lights are enabled before fading in
        if (turnOn)
        {
            foreach (Light l in lights)
            {
                if (l == null) continue;

                l.enabled = true;
                l.intensity = 0f;
            }
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            foreach (Light l in lights)
            {
                if (l == null) continue;

                float target = originalIntensity.ContainsKey(l) ? originalIntensity[l] : 1f;

                if (turnOn)
                {
                    l.intensity = Mathf.Lerp(0f, target, t);
                }
                else
                {
                    l.intensity = Mathf.Lerp(target, 0f, t);
                }
            }

            // 🔴 CRITICAL: sync emissives with same curve
            foreach (EmissiveSync e in emissives)
            {
                if (e != null)
                {
                    e.SetEmissionLevel(turnOn ? t : 1f - t);
                }
            }

            yield return null;
        }

        // Final correction pass
        foreach (Light l in lights)
        {
            if (l == null) continue;

            if (turnOn)
            {
                l.intensity = originalIntensity[l];
            }
            else
            {
                l.intensity = 0f;
                l.enabled = false;
            }
        }
    }

    // =========================================================
    // 🔹 DEBUG (RIGHT-CLICK INSPECTOR)
    // =========================================================

    [ContextMenu("DEBUG → Lights ON")]
    private void DebugOn()
    {
        TurnLightsOn();
    }

    [ContextMenu("DEBUG → Lights OFF")]
    private void DebugOff()
    {
        TurnLightsOff();
    }
}