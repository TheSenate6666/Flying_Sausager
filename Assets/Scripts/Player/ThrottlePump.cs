using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Pumping throttle mechanic.
/// The player drags a handle UP to pump. Resistance builds the further
/// the handle is from the top of its stroke, simulating a heavy pump.
/// Throttle decays automatically after a delay.
/// </summary>
public class ThrottlePump : MonoBehaviour
{
    
    
    
    public static ThrottlePump Instance { get; private set; }
    private void Awake() { Instance = this; }
    
    
    // ── References ───────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The RectTransform that the player actually drags (invisible or styled knob).")]
    public RectTransform pumpHandle;

    [Tooltip("The RectTransform that visually lags behind the handle (the pump rod visual).")]
    public RectTransform pumpHandleVisual;

    [Tooltip("The slider that displays 0-100% throttle.")]
    public Slider throttleSlider;

    [Tooltip("The PlaneController to write throttle into.")]
    public PlaneController planeController;

    // ── Pump geometry ────────────────────────────────────────────────────────
    [Header("Pump Geometry")]
    [Tooltip("Y position (local) of the very bottom of the pump stroke.")]
    public float strokeBottom = -200f;

    [Tooltip("Y position (local) of the very top of the pump stroke.")]
    public float strokeTop = 200f;

    // ── Resistance (mirrors DragLimiter logic) ───────────────────────────────
    [Header("Resistance")]
    [Tooltip("Zone from the top where there is NO fallback — free movement.")]
    public float allowableDistance = 40f;

    [Tooltip("Distance over which resistance ramps up after the free zone.")]
    public float resistanceDistance = 160f;

    [Tooltip("How much the visual lags behind as resistance builds.")]
    public AnimationCurve fallbackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Shake magnitude curve vs resistance (0-1 overage).")]
    public AnimationCurve vibrateCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Overall shake scale.")]
    public float shakingFactor = 5f;

    [Tooltip("How fast the visual snaps back when released.")]
    public float snapSpeed = 10f;

    // ── Throttle behaviour ───────────────────────────────────────────────────
    [Header("Throttle")]
    [Tooltip("Throttle gained per full pump (handle travels from bottom to top).")]
    public float throttlePerPump = 20f;

    [Tooltip("Seconds after the last pump before throttle starts decaying.")]
    public float decayDelay = 2f;

    [Tooltip("Throttle lost per second during decay.")]
    public float decayRate = 5f;

    // ── Runtime state ────────────────────────────────────────────────────────
    private float throttle = 0f;           // 0-100
    private float decayTimer = 0f;
    private bool isDragging = false;
    private float dragStartY;              // screen Y when drag began
    private float handleStartLocalY;      // handle localPos.Y when drag began
    private float lastHandleY;            // for delta tracking
    private bool  wasAboveMidpoint = false;

    // visual-lag state
    private float visualY;

    // ── Public accessor for other scripts ────────────────────────────────────
    public float Throttle => throttle;

    // ────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Start handle at the bottom
        SetHandleY(strokeBottom);
        visualY = strokeBottom;

        if (throttleSlider != null)
        {
            throttleSlider.minValue = 0f;
            throttleSlider.maxValue = 100f;
            throttleSlider.interactable = false; // display only
        }

        // Wire up pointer events on the handle
        SetupHandleEvents();
    }

    private void Update()
    {
        HandleDecay();
        UpdateSlider();

        // Push throttle into the plane controller
        if (planeController != null)
            planeController.SetThrottle(throttle);
    }

    // ── Drag wiring ──────────────────────────────────────────────────────────
    private void SetupHandleEvents()
    {
        if (pumpHandle == null) return;

        EventTrigger trigger = pumpHandle.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = pumpHandle.gameObject.AddComponent<EventTrigger>();

        AddTriggerEntry(trigger, EventTriggerType.PointerDown, OnPointerDown);
        AddTriggerEntry(trigger, EventTriggerType.Drag,        OnDrag);
        AddTriggerEntry(trigger, EventTriggerType.PointerUp,   OnPointerUp);
    }

    private void AddTriggerEntry(EventTrigger trigger, EventTriggerType type,
                                  UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    // ── Pointer callbacks ────────────────────────────────────────────────────
    private void OnPointerDown(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData)data;
        isDragging        = true;
        dragStartY        = ped.position.y;
        handleStartLocalY = pumpHandle.localPosition.y;
        lastHandleY       = handleStartLocalY;
        wasAboveMidpoint  = handleStartLocalY > (strokeBottom + strokeTop) * 0.5f;
    }

    private void OnDrag(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData)data;

        // Convert screen-space delta to local-space delta using canvas scale
        Canvas canvas = pumpHandle.GetComponentInParent<Canvas>();
        float scale   = canvas != null ? canvas.scaleFactor : 1f;

        float rawDelta = (ped.position.y - dragStartY) / scale;
        float rawY     = Mathf.Clamp(handleStartLocalY + rawDelta, strokeBottom, strokeTop);

        // ── Resistance logic (from DragLimiter) ─────────────────────────────
        float distFromTop  = strokeTop - rawY;   // 0 at the top, grows downward
        float shakingOffset = 0f;

        if (distFromTop > allowableDistance)
        {
            float overage = (distFromTop - allowableDistance) / resistanceDistance;
            overage = Mathf.Clamp01(overage);

            // Visual fallback on the VISUAL rod, not the logical handle
            float fallback    = fallbackCurve.Evaluate(overage) * resistanceDistance * 0.5f;
            float shakeMag    = vibrateCurve.Evaluate(overage) * shakingFactor;
            shakingOffset     = Random.Range(-shakeMag, shakeMag);

            visualY = rawY + fallback + shakingOffset;  // visual lags behind (lower than real handle)
        }
        else
        {
            visualY = rawY;
        }

        visualY = Mathf.Clamp(visualY, strokeBottom, strokeTop);

        // Move logical handle silently
        SetHandleY(rawY);

        // Move visual rod
        if (pumpHandleVisual != null)
        {
            Vector3 vp = pumpHandleVisual.localPosition;
            vp.y = visualY;
            pumpHandleVisual.localPosition = vp;
        }

        // ── Pump detection ───────────────────────────────────────────────────
        bool nowAboveMidpoint = rawY > (strokeBottom + strokeTop) * 0.5f;

        // A pump is counted when the handle crosses from bottom-half → top-half
        if (!wasAboveMidpoint && nowAboveMidpoint)
            RegisterPump(rawY);

        wasAboveMidpoint = nowAboveMidpoint;
        lastHandleY      = rawY;
    }

    private void OnPointerUp(BaseEventData data)
    {
        isDragging = false;
        // Snap handle back to bottom so the player must do a full stroke next time
        SetHandleY(strokeBottom);
    }

    // ── Visual snap-back when not dragging ───────────────────────────────────
    private void LateUpdate()
    {
        if (!isDragging && pumpHandleVisual != null)
        {
            Vector3 vp   = pumpHandleVisual.localPosition;
            Vector3 hp   = pumpHandle.localPosition;
            vp.y         = Mathf.Lerp(vp.y, hp.y, snapSpeed * Time.deltaTime);
            pumpHandleVisual.localPosition = vp;
        }
    }

    // ── Throttle helpers ─────────────────────────────────────────────────────
    private void RegisterPump(float handleY)
    {
        // Bonus: pump earns MORE throttle the higher the handle gets at the top
        float topness      = Mathf.InverseLerp(
                                 (strokeBottom + strokeTop) * 0.5f, strokeTop, handleY);
        float earned       = throttlePerPump * Mathf.Lerp(0.5f, 1f, topness);
        throttle           = Mathf.Clamp(throttle + earned, 0f, 100f);
        decayTimer         = decayDelay; // reset decay countdown on every pump
    }

    private void HandleDecay()
    {
        if (decayTimer > 0f)
        {
            decayTimer -= Time.deltaTime;
            return;
        }
        throttle = Mathf.Max(0f, throttle - decayRate * Time.deltaTime);
    }

    private void UpdateSlider()
    {
        if (throttleSlider != null)
            throttleSlider.value = throttle;
    }

    private void SetHandleY(float y)
    {
        if (pumpHandle == null) return;
        Vector3 p = pumpHandle.localPosition;
        p.y = y;
        pumpHandle.localPosition = p;
    }

    
}