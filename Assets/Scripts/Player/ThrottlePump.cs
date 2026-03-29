using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ThrottlePump : MonoBehaviour
{
    public static ThrottlePump Instance { get; private set; }
    private void Awake() { Instance = this; }

    [Header("References")]
    public RectTransform pumpHandle;
    public RectTransform pumpHandleVisual;
    public Slider throttleSlider;
    public PlaneController planeController;

    [Header("Pump Geometry")]
    public float strokeBottom = -200f;
    public float strokeTop    =  200f;

    [Header("Resistance")]
    public float allowableDistance  = 40f;
    public float resistanceDistance = 160f;
    public AnimationCurve fallbackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve vibrateCurve  = AnimationCurve.Linear(0, 0, 1, 1);
    public float shakingFactor = 5f;
    public float snapSpeed     = 10f;

    [Header("Buoyancy Value")]
    [Tooltip("Buoyancy gained per full pump stroke (0–100 scale).")]
    public float throttlePerPump = 20f;

    [Tooltip("Seconds after last pump before buoyancy starts decaying.")]
    public float decayDelay = 2f;

    [Tooltip("Buoyancy lost per second during decay.")]
    public float decayRate = 5f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private float throttle   = 0f;
    private float decayTimer = 0f;

    private bool  isDragging       = false;
    private float dragStartY;
    private float handleStartLocalY;
    private float lastHandleY;
    private bool  wasAboveMidpoint = false;
    private float visualY;

    public float Throttle => throttle;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        SetHandleY(strokeBottom);
        visualY = strokeBottom;

        if (throttleSlider != null)
        {
            throttleSlider.minValue    = 0f;
            throttleSlider.maxValue    = 100f;
            throttleSlider.interactable = false;
        }

        SetupHandleEvents();
    }

    private void Update()
    {
        HandleDecay();
        UpdateSlider();

        // Push current buoyancy value to the plane every frame
        if (PlaneController.Instance != null)
            PlaneController.Instance.SetPumpValue(throttle);
    }

    private void LateUpdate()
    {
        if (!isDragging && pumpHandleVisual != null)
        {
            Vector3 vp = pumpHandleVisual.localPosition;
            Vector3 hp = pumpHandle.localPosition;
            vp.y = Mathf.Lerp(vp.y, hp.y, snapSpeed * Time.deltaTime);
            pumpHandleVisual.localPosition = vp;
        }
    }

    // ── Event wiring ──────────────────────────────────────────────────────────
    private void SetupHandleEvents()
    {
        if (pumpHandle == null) return;

        EventTrigger trigger = pumpHandle.gameObject.GetComponent<EventTrigger>()
                            ?? pumpHandle.gameObject.AddComponent<EventTrigger>();

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

    // ── Pointer handlers ──────────────────────────────────────────────────────
    private void OnPointerDown(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData)data;
        isDragging = true;
        dragStartY = ped.position.y;

        // Use the actual click Y position instead of the handle's center
        // This way it doesn't matter where on the track the player grabs
        Canvas canvas = pumpHandle.GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            pumpHandle.parent as RectTransform,
            ped.position,
            ped.pressEventCamera,
            out Vector2 localClick);

        handleStartLocalY = Mathf.Clamp(localClick.y, strokeBottom, strokeTop);
        lastHandleY       = handleStartLocalY;
        wasAboveMidpoint  = handleStartLocalY > (strokeBottom + strokeTop) * 0.5f;
    }

    private void OnDrag(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData)data;

        Canvas canvas = pumpHandle.GetComponentInParent<Canvas>();
        float  scale  = canvas != null ? canvas.scaleFactor : 1f;

        float rawDelta = (ped.position.y - dragStartY) / scale;
        float rawY     = Mathf.Clamp(handleStartLocalY + rawDelta, strokeBottom, strokeTop);

        // ── Resistance ────────────────────────────────────────────────────────
        float distFromTop   = strokeTop - rawY;
        float shakingOffset = 0f;

        if (distFromTop > allowableDistance)
        {
            float overage  = Mathf.Clamp01((distFromTop - allowableDistance) / resistanceDistance);
            float fallback = fallbackCurve.Evaluate(overage) * resistanceDistance * 0.5f;
            float shakeMag = vibrateCurve.Evaluate(overage) * shakingFactor;
            shakingOffset  = Random.Range(-shakeMag, shakeMag);
            visualY        = Mathf.Clamp(rawY + fallback + shakingOffset, strokeBottom, strokeTop);
        }
        else
        {
            visualY = rawY;
        }

        SetHandleY(rawY);

        if (pumpHandleVisual != null)
        {
            Vector3 vp = pumpHandleVisual.localPosition;
            vp.y = visualY;
            pumpHandleVisual.localPosition = vp;
        }

        // ── Pump detection ────────────────────────────────────────────────────
        bool nowAboveMidpoint = rawY > (strokeBottom + strokeTop) * 0.5f;
        if (!wasAboveMidpoint && nowAboveMidpoint)
            RegisterPump(rawY);

        wasAboveMidpoint = nowAboveMidpoint;
        lastHandleY      = rawY;
    }

    private void OnPointerUp(BaseEventData data)
    {
        isDragging = false;
        SetHandleY(strokeBottom);
    }

    // ── Buoyancy helpers ──────────────────────────────────────────────────────
    private void RegisterPump(float handleY)
    {
        float topness = Mathf.InverseLerp(
            (strokeBottom + strokeTop) * 0.5f, strokeTop, handleY);
        float earned  = throttlePerPump * Mathf.Lerp(0.5f, 1f, topness);

        throttle   = Mathf.Clamp(throttle + earned, 0f, 100f);
        decayTimer = decayDelay;

        // Tell the plane controller a pump just happened (resets its buoyancy timer)
        if (PlaneController.Instance != null)
            PlaneController.Instance.NotifyPump();
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
