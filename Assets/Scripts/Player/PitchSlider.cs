using UnityEngine;
using UnityEngine.UI;

public class PitchSlider : MonoBehaviour
{
    public static PitchSlider Instance { get; private set; }

    [Header("References")]
    [Tooltip("A vertical Unity Slider. Min = 0, Max = 100, Start value = 50.")]
    public Slider pitchSlider;

    [Header("Snap To Neutral (on release)")]
    [Tooltip("If true, slider returns to 50 when the player lets go.")]
    public bool snapToNeutral = true;

    [Tooltip("How fast the slider springs back to neutral on release. Try 3–8.")]
    public float neutralSnapSpeed = 5f;

    [Header("Decay (after being set)")]
    [Tooltip("Seconds after the player releases before decay toward neutral begins.")]
    public float decayDelay = 2f;

    [Tooltip("How fast the slider creeps back to neutral per second during decay. " +
             "This is in slider units (0–100 scale). Try 5–15.")]
    public float decayRate = 8f;

    [Tooltip("How smoothly the decay is applied. Lower = snappier, higher = gradual. " +
             "Try 0.2–0.8.")]
    public float decaySmoothTime = 0.4f;

    [Tooltip("Distance from neutral (50) below which the value snaps exactly to 50 " +
             "to prevent endless micro-drift. Try 0.1–0.5.")]
    public float decayDeadzone = 0.2f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private bool  isHeld          = false;
    private float decayTimer      = 0f;       // counts down before decay starts
    private bool  isDecaying      = false;    // true once delay has passed
    private float smoothVelocity  = 0f;       // internal SmoothDamp state

    private const float Neutral   = 50f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (pitchSlider == null) return;

        pitchSlider.minValue = 0f;
        pitchSlider.maxValue = 100f;
        pitchSlider.value    = Neutral;

        pitchSlider.onValueChanged.AddListener(OnSliderValueChanged);
        AddPointerEvents();
    }

    private void Update()
    {
        if (pitchSlider == null) return;

        if (isHeld)
        {
            // Player is actively holding — reset the decay countdown
            decayTimer  = decayDelay;
            isDecaying  = false;
            smoothVelocity = 0f;

            // Snap-to-neutral on hold is disabled while held (obviously)
            return;
        }

        // ── Snap back immediately on release (optional) ───────────────────────
        if (snapToNeutral)
        {
            ApplySnapOrDecay();
            return;
        }

        // ── Decay after delay (only when snapToNeutral is false) ──────────────
        if (decayTimer > 0f)
        {
            decayTimer -= Time.deltaTime;
            return;                         // still in grace period, hold position
        }

        // Grace period over — start decaying
        isDecaying = true;
        ApplySnapOrDecay();
    }

    // ── Shared movement toward neutral ────────────────────────────────────────
    private void ApplySnapOrDecay()
    {
        float current = pitchSlider.value;

        if (Mathf.Abs(current - Neutral) <= decayDeadzone)
        {
            pitchSlider.value = Neutral;    // close enough — snap exactly to center
            isDecaying        = false;
            smoothVelocity    = 0f;
            return;
        }

        if (snapToNeutral)
        {
            // Spring back using Lerp — fast and responsive on release
            pitchSlider.value = Mathf.Lerp(current, Neutral, neutralSnapSpeed * Time.deltaTime);
        }
        else if (isDecaying)
        {
            // Gradual SmoothDamp decay — slow and weighted, like a heavy lever
            float target      = Neutral;
            float maxDelta    = decayRate * Time.deltaTime;
            float smoothed    = Mathf.SmoothDamp(
                                    current, target,
                                    ref smoothVelocity,
                                    decaySmoothTime,
                                    maxDelta);
            pitchSlider.value = smoothed;
        }
    }

    // ── Slider value → PlaneController ───────────────────────────────────────
    private void OnSliderValueChanged(float value)
    {
        if (PlaneController.Instance != null)
            PlaneController.Instance.SetPitchInput(value);
    }

    // ── Pointer events ────────────────────────────────────────────────────────
    private void AddPointerEvents()
    {
        var trigger = pitchSlider.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                   ?? pitchSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        AddEntry(trigger,
            UnityEngine.EventSystems.EventTriggerType.PointerDown,
            _ => isHeld = true);

        AddEntry(trigger,
            UnityEngine.EventSystems.EventTriggerType.PointerUp,
            _ =>
            {
                isHeld     = false;
                decayTimer = decayDelay;    // start the grace period on release
                isDecaying = false;
            });
    }

    private void AddEntry(
        UnityEngine.EventSystems.EventTrigger trigger,
        UnityEngine.EventSystems.EventTriggerType type,
        UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData> callback)
    {
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }
}
