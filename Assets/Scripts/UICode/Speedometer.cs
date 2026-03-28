using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// </summary>
public class Speedometer : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The empty RectTransform that the needle rotates around. " +
             "Needle should be a child of this with Pivot Y = 0.")]
    public RectTransform needlePivot;

    [Tooltip("Digital speed readout in km/h.")]
    public TMP_Text speedLabel;

    [Tooltip("Optional label showing max speed (e.g. '144 km/h'). Set once on Start.")]
    public TMP_Text maxSpeedLabel;

    [Tooltip("Optional label showing the unit (e.g. 'km/h').")]
    public TMP_Text unitLabel;

    // ── Needle angles ─────────────────────────────────────────────────────────
    [Header("Needle Angles")]
    [Tooltip("Z rotation of the needle at 0 km/h. " +
             "Tip: enter play mode, set speed to 0, then tweak until needle points to 0. " +
             "Typically around 130–145 for a left-starting gauge.")]
    public float angleAtZero = 135f;

    [Tooltip("Z rotation of the needle at max speed. " +
             "Typically around -135 to -45 for a right-ending gauge.")]
    public float angleAtMax = -135f;

    // ── Smoothing ─────────────────────────────────────────────────────────────
    [Header("Smoothing")]
    [Tooltip("How smoothly the needle follows speed changes. " +
             "Lower = snappier, higher = laggy/realistic. Try 0.1–0.4.")]
    public float needleSmoothTime = 0.15f;

    [Tooltip("If speed is below this (m/s), show exactly 0 to prevent needle jitter at rest.")]
    public float zeroThreshold = 0.05f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private float currentAngle       = 0f;
    private float angleSmoothVelocity = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Set the needle to zero position immediately (no lerp on start)
        if (needlePivot != null)
            needlePivot.localEulerAngles = new Vector3(0f, 0f, angleAtZero);

        currentAngle = angleAtZero;

        // Optional: display max speed once
        if (maxSpeedLabel != null && PlaneController.Instance != null)
        {
            float maxKmh = PlaneController.Instance.maxForwardSpeed * 3.6f;
            maxSpeedLabel.text = ((int)maxKmh).ToString();
        }

        if (unitLabel != null)
            unitLabel.text = "km/h";
    }

    private void Update()
    {
        if (PlaneController.Instance == null) return;

        // Get speed in km/h from the plane controller's public property
        float speedMs  = PlaneController.Instance.CurrentForwardSpeed;
        float speedKmh = speedMs * 3.6f;

        // Clamp jitter at rest
        if (Mathf.Abs(speedMs) < zeroThreshold)
            speedKmh = 0f;

        float maxKmh = PlaneController.Instance.maxForwardSpeed * 3.6f;

        // Map speed to needle angle
        float t           = Mathf.Clamp01(speedKmh / maxKmh);
        float targetAngle = Mathf.Lerp(angleAtZero, angleAtMax, t);

        // Smooth the needle so it swings naturally rather than snapping
        currentAngle = Mathf.SmoothDamp(
            currentAngle, targetAngle,
            ref angleSmoothVelocity, needleSmoothTime);

        // Apply rotation
        if (needlePivot != null)
            needlePivot.localEulerAngles = new Vector3(0f, 0f, currentAngle);

        // Update digital readout
        if (speedLabel != null)
            speedLabel.text = ((int)speedKmh).ToString();
    }
}
