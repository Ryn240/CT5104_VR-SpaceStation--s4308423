using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// ARScannerDevice
/// ---------------
/// Controls player scanning system.
///
/// FEATURES:
/// - Auto input (mouse + XR trigger)
/// - Raycast detection
/// - Soft lock-on
/// - Anchor-based targeting
/// - Smooth beam
/// - Highlight triggering
/// </summary>
public class ARScannerDevice : MonoBehaviour
{
    [Header("Input")]
    public InputAction scanAction;

    [Header("Scan Settings")]
    public Transform scanOrigin;
    public float scanDistance = 8f;
    public LayerMask scannableLayers;

    [Header("Lock-On")]
    public float lockHoldTime = 0.5f;
    public float beamSmoothSpeed = 15f;

    [Header("UI")]
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI statusText;

    [Header("Visuals")]
    public LineRenderer scanLine;
    public GameObject scannerGlow;

    private ARScannable currentTarget;
    private float scanTimer;
    private float lockTimer;
    private Vector3 currentBeamEnd;

    // =========================================================
    // 🔹 AUTO INPUT
    // =========================================================

    void Awake()
    {
        if (scanAction == null)
        {
            scanAction = new InputAction("Scan", InputActionType.Button);
            scanAction.AddBinding("<Mouse>/leftButton");
            scanAction.AddBinding("<XRController>{RightHand}/trigger");
        }
    }

    private void OnEnable() => scanAction.Enable();
    private void OnDisable() => scanAction.Disable();

    // =========================================================
    // 🔹 INIT
    // =========================================================

    void Start()
    {
        ResetUI();

        if (scanLine != null)
            scanLine.enabled = false;

        if (scannerGlow != null)
            scannerGlow.SetActive(false);

        currentBeamEnd = scanOrigin.position;
    }

    // =========================================================
    // 🔹 UPDATE
    // =========================================================

    void Update()
    {
        if (scanAction.IsPressed())
            Scan();
        else
            StopScan();
    }

    // =========================================================
    // 🔹 SCAN LOGIC
    // =========================================================

    void Scan()
    {
        if (scannerGlow != null)
            scannerGlow.SetActive(true);

        Ray ray = new Ray(scanOrigin.position, scanOrigin.forward);
        RaycastHit hit;

        ARScannable detectedTarget = null;

        if (Physics.Raycast(ray, out hit, scanDistance, scannableLayers))
        {
            detectedTarget = hit.collider.GetComponentInParent<ARScannable>();
        }

        // LOCK-ON
        if (detectedTarget != null)
        {
            if (currentTarget != detectedTarget)
            {
                ClearTarget();

                currentTarget = detectedTarget;
                currentTarget.SetHighlight(true);

                scanTimer = 0f;
            }

            lockTimer = lockHoldTime;
        }
        else
        {
            lockTimer -= Time.deltaTime;

            if (lockTimer <= 0f)
            {
                LoseTarget();
                return;
            }
        }

        // TARGET POSITION
        Vector3 targetPoint;

        if (currentTarget.scanTargetAnchor != null)
            targetPoint = currentTarget.scanTargetAnchor.position;
        else if (currentTarget.overlayAnchor != null)
            targetPoint = currentTarget.overlayAnchor.position;
        else
            targetPoint = currentTarget.transform.position;

        // SMOOTH BEAM
        currentBeamEnd = Vector3.Lerp(
            currentBeamEnd,
            targetPoint,
            Time.deltaTime * beamSmoothSpeed
        );

        if (scanLine != null)
        {
            scanLine.enabled = true;
            scanLine.SetPosition(0, scanOrigin.position);
            scanLine.SetPosition(1, currentBeamEnd);
        }

        // PROGRESS
        scanTimer += Time.deltaTime;

        headerText.text = "SCANNING";
        targetNameText.text = currentTarget.displayName;

        float progress = Mathf.Clamp01(scanTimer / currentTarget.requiredScanTime);
        statusText.text = "Progress: " + Mathf.RoundToInt(progress * 100f) + "%";

        if (scanTimer >= currentTarget.requiredScanTime)
        {
            headerText.text = "SCAN COMPLETE";
            statusText.text = currentTarget.description;

            currentTarget.ShowScanVisuals();
        }
    }

    // =========================================================
    // 🔹 RESET
    // =========================================================

    void StopScan()
    {
        DisableVisuals();
        ClearTarget();
        ResetUI();
    }

    void LoseTarget()
    {
        DisableVisuals();
        ClearTarget();

        headerText.text = "NO TARGET";
        targetNameText.text = "---";
        statusText.text = "Aim at a scannable object.";
    }

    void DisableVisuals()
    {
        if (scanLine != null)
            scanLine.enabled = false;

        if (scannerGlow != null)
            scannerGlow.SetActive(false);
    }

    void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.SetHighlight(false);
            currentTarget.HideScanVisuals();
            currentTarget = null;
        }

        scanTimer = 0f;
    }

    void ResetUI()
    {
        headerText.text = "SCANNER READY";
        targetNameText.text = "---";
        statusText.text = "Hold trigger to scan.";
    }
}