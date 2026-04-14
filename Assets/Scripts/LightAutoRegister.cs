using UnityEngine;
using System.Collections;

/// <summary>
/// LIGHT AUTO REGISTER (ROBUST VERSION)
/// -----------------------------------
/// This script automatically registers a Light with the LightingManager,
/// even if the manager is not ready when this object initialises.
///
/// KEY IMPROVEMENT:
/// Uses a retry system instead of relying on execution order.
///
/// WHY THIS MATTERS:
/// Unity does NOT guarantee that scripts initialise in a specific order.
/// This script removes that dependency entirely.
///
/// TEACHING TAKEAWAY:
/// "Don't assume systems exist — verify and retry."
/// </summary>

[RequireComponent(typeof(Light))] // Ensures every object using this script has a Light
public class LightAutoRegister : MonoBehaviour
{
    // =========================================================
    // 🔹 CONFIG (you can expose these later if needed)
    // =========================================================

    [Header("Retry Settings")]

    [Tooltip("Time (seconds) between retry attempts")]
    [SerializeField] private float retryInterval = 0.5f;

    [Tooltip("Maximum number of retry attempts before giving up")]
    [SerializeField] private int maxRetries = 10;

    // =========================================================
    // 🔹 INTERNAL STATE
    // =========================================================

    private Light cachedLight;        // Cached reference to avoid repeated GetComponent calls
    private bool hasRegistered = false;
    private int retryCount = 0;
    private Coroutine retryCoroutine;

    // =========================================================
    // 🔹 INITIALISATION
    // =========================================================

    private void Awake()
    {
        // Cache the Light component immediately
        cachedLight = GetComponent<Light>();

        if (cachedLight == null)
        {
            Debug.LogWarning($"[LightAutoRegister] No Light found on {gameObject.name}");
            return;
        }

        // Start the registration process
        StartRegistrationProcess();
    }

    // =========================================================
    // 🔹 REGISTRATION ENTRY POINT
    // =========================================================

    private void StartRegistrationProcess()
    {
        // Try immediately first (best case scenario)
        if (!TryRegister())
        {
            // If that fails, begin retry loop
            retryCoroutine = StartCoroutine(RetryRegistration());
        }
    }

    // =========================================================
    // 🔹 CORE REGISTRATION LOGIC
    // =========================================================

    /// <summary>
    /// Attempts to register with the LightingManager.
    /// Returns TRUE if successful.
    /// </summary>
    private bool TryRegister()
    {
        if (hasRegistered) return true;

        // Check if the manager exists
        if (LightingManager.Instance != null)
        {
            LightingManager.Instance.RegisterLight(cachedLight);
            hasRegistered = true;

            // Stop retry loop if running
            if (retryCoroutine != null)
            {
                StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }

            // Optional debug
            Debug.Log($"[LightAutoRegister] Registered: {gameObject.name}");

            return true;
        }

        // Manager not ready yet
        return false;
    }

    // =========================================================
    // 🔹 RETRY SYSTEM
    // =========================================================

    /// <summary>
    /// Keeps trying to register until successful or max retries reached.
    /// </summary>
    private IEnumerator RetryRegistration()
    {
        while (!hasRegistered && retryCount < maxRetries)
        {
            retryCount++;

            // Wait before retrying
            yield return new WaitForSeconds(retryInterval);

            // Try again
            if (TryRegister())
            {
                yield break; // success → exit coroutine
            }
        }

        // If we reach here, registration failed after multiple attempts
    //    if (!hasRegistered)
    //    {
   //         Debug.LogWarning(
   //             $"[LightAutoRegister] Failed to register {gameObject.name} after {maxRetries} attempts. " +
   //            $"Ensure LightingManager exists in the scene."
   //         );
   //     }
    }

    // =========================================================
    // 🔹 HANDLE ENABLE / DISABLE
    // =========================================================

    private void OnEnable()
    {
        // If object becomes active again and hasn't registered, try again
        if (!hasRegistered && cachedLight != null)
        {
            StartRegistrationProcess();
        }
    }

    // =========================================================
    // 🔹 OPTIONAL CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        // Optional future extension:
        // LightingManager.Instance?.UnregisterLight(cachedLight);
    }
}