using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vertical UI slider controlling the plane's pitch.
///
/// Slider layout (vertical Unity slider):
///   Top    = 100 = nose DOWN  (-1 pitch torque)
///   Middle =  50 = neutral    (0)
///   Bottom =   0 = nose UP    (+1 pitch torque)
///
/// The slider snaps back to 50 (neutral) when released,
/// just like a joystick returning to center.
/// This can be disabled with snapToNeutral = false if you
/// prefer the pitch to hold its last value.
/// </summary>
public class PitchSlider : MonoBehaviour
{
    public static PitchSlider Instance { get; private set; }

    [Header("References")]
    [Tooltip("A vertical Unity Slider. Min = 0, Max = 100, Start value = 50.")]
    public Slider pitchSlider;

    [Header("Behaviour")]
    [Tooltip("If true, the slider smoothly returns to 50 (neutral) when released. " +
             "Feels like a spring-loaded joystick.")]
    public bool snapToNeutral = true;

    [Tooltip("How fast the slider returns to neutral when released. " +
             "Higher = snappier return. Try 3–8.")]
    public float neutralSnapSpeed = 5f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private bool isHeld = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (pitchSlider == null) return;

        // Configure slider
        pitchSlider.minValue = 0f;
        pitchSlider.maxValue = 100f;
        pitchSlider.value    = 50f;   // start at neutral center

        // Drive PlaneController whenever the value changes
        pitchSlider.onValueChanged.AddListener(OnSliderValueChanged);

        // Detect pointer hold/release for snap-back
        AddPointerEvents();
    }

    private void Update()
    {
        // Snap back to neutral (50) when not held
        if (snapToNeutral && !isHeld && pitchSlider != null)
        {
            float current = pitchSlider.value;
            float neutral = 50f;

            if (Mathf.Abs(current - neutral) > 0.01f)
            {
                pitchSlider.value = Mathf.Lerp(current, neutral,
                    neutralSnapSpeed * Time.deltaTime);
            }
            else
            {
                pitchSlider.value = neutral;
            }
        }
    }

    // ── Slider value → PlaneController ───────────────────────────────────────
    private void OnSliderValueChanged(float value)
    {
        if (PlaneController.Instance != null)
            PlaneController.Instance.SetPitchInput(value);
    }

    // ── Pointer held tracking (for snap-back) ─────────────────────────────────
    private void AddPointerEvents()
    {
        var trigger = pitchSlider.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                   ?? pitchSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        AddEntry(trigger,
            UnityEngine.EventSystems.EventTriggerType.PointerDown,
            _ => isHeld = true);

        AddEntry(trigger,
            UnityEngine.EventSystems.EventTriggerType.PointerUp,
            _ => isHeld = false);
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
