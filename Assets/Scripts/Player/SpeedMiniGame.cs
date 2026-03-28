using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "Stop the bar" speed-setting minigame.
///
/// State machine:
///   IDLE    → player presses button → SWEEPING
///   SWEEPING → player presses button → LOCKED (applies speed, then back to IDLE)
///
/// The slider sweeps between 0 and 100 at sweepSpeed units/sec, ping-ponging.
/// Locking in writes the current slider value as a % of maxForwardSpeed
/// into PlaneController via SetTargetSpeed().
/// </summary>
public class SpeedMiniGame : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The slider that visually sweeps left to right.")]
    public Slider speedSlider;

    [Tooltip("The button the player presses to start and stop the sweep.")]
    public Button actionButton;

    [Tooltip("Text on the action button — changes based on state.")]
    public TMP_Text buttonLabel;

    [Tooltip("Optional: a parent panel to show/hide the whole minigame UI.")]
    public GameObject sliderPanel;

    // ── Tuning ────────────────────────────────────────────────────────────────
    [Header("Tuning")]
    [Tooltip("How fast the slider sweeps in units/sec (slider is 0–100). " +
             "50 = takes 2 seconds to cross the full bar. " +
             "100 = takes 1 second. Try 40–70 to start.")]
    public float sweepSpeed = 50f;

    [Tooltip("Text shown on the button when nothing is happening.")]
    public string labelIdle = "Set Speed";

    [Tooltip("Text shown on the button while the bar is sweeping.")]
    public string labelSweeping = "Stop!";

    // ── State ─────────────────────────────────────────────────────────────────
    private enum MinigameState { Idle, Sweeping }
    private MinigameState state = MinigameState.Idle;

    // Current slider value 0–100, driven by sweep or locked when stopped
    private float sliderValue   = 0f;

    // Direction the slider is currently moving (+1 = right, -1 = left)
    private float sweepDirection = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Configure the slider as display-only — we drive it manually
        if (speedSlider != null)
        {
            speedSlider.minValue     = 0f;
            speedSlider.maxValue     = 100f;
            speedSlider.interactable = false;
            speedSlider.value        = 0f;
        }

        // Wire the button
        if (actionButton != null)
            actionButton.onClick.AddListener(OnButtonPressed);

        // Start hidden if a panel reference was given
        if (sliderPanel != null)
            sliderPanel.SetActive(false);

        SetButtonLabel(labelIdle);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (state == MinigameState.Sweeping)
            UpdateSweep();
    }

    // ── Button handler ────────────────────────────────────────────────────────
    private void OnButtonPressed()
    {
        switch (state)
        {
            case MinigameState.Idle:
                StartSweep();
                break;

            case MinigameState.Sweeping:
                LockSpeed();
                break;
        }
    }

    // ── State transitions ─────────────────────────────────────────────────────
    private void StartSweep()
    {
        state          = MinigameState.Sweeping;
        sliderValue    = 0f;           // always start from the left
        sweepDirection = 1f;

        if (sliderPanel != null)
            sliderPanel.SetActive(true);

        SetButtonLabel(labelSweeping);
    }

    private void LockSpeed()
    {
        state = MinigameState.Idle;

        // Convert 0–100 slider value to 0–1 and multiply by max speed
        float speedPercent = sliderValue / 100f;

        if (PlaneController.Instance != null)
            PlaneController.Instance.SetTargetSpeed(speedPercent);

        // Freeze the slider visually at the locked value so player can see where they stopped
        // The panel stays visible — it fades/hides only when they next press Set Speed
        SetButtonLabel(labelIdle);
    }

    // ── Sweep logic ───────────────────────────────────────────────────────────
    private void UpdateSweep()
    {
        // Move slider value in current direction
        sliderValue += sweepDirection * sweepSpeed * Time.deltaTime;

        // Ping-pong: reverse direction at each end
        if (sliderValue >= 100f)
        {
            sliderValue    = 100f;
            sweepDirection = -1f;
        }
        else if (sliderValue <= 0f)
        {
            sliderValue    = 0f;
            sweepDirection = 1f;
        }

        // Push to the UI slider
        if (speedSlider != null)
            speedSlider.value = sliderValue;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetButtonLabel(string text)
    {
        if (buttonLabel != null)
            buttonLabel.text = text;
    }
}