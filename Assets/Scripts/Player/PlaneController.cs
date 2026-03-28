using UnityEngine;
using TMPro;
using SimpleInputNamespace;

public class PlaneController : MonoBehaviour
{
    [Header("Forward Thrust")]
    [Tooltip("Maximum forward speed (m/s). 40 = ~144 km/h")]
    public float maxForwardSpeed = 40f;

    [Tooltip("Seconds after speed is set before it starts decaying.")]
    public float forwardDecayDelay = 3f;

    [Tooltip("m/s lost per second once decay kicks in.")]
    public float forwardDecayRate = 2f;

    [Tooltip("How smoothly actual velocity catches up to target speed. " +
             "Lower = snappier, higher = sluggish. 0.5–2.0 is a good range.")]
    public float speedSmoothTime = 0.8f;

    [Tooltip("Passive speed loss per second, always active regardless of decay delay. " +
             "Simulates air resistance. 0 = no passive loss. Try 0.5–2.")]
    public float passiveSpeedDrain = 0.5f;

    [Header("Buoyancy (Pump-controlled)")]
    [Tooltip("Downward force per second. Set this to rb.mass * 9.81. " +
             "Rigidbody > Use Gravity must be FALSE.")]
    public float gravityForce = 49f;

    [Tooltip("Maximum upward force at 100% pump. Set to 2x gravityForce so 50% = hover.")]
    public float maxPumpLift = 98f;

    [Tooltip("Dampens vertical velocity to prevent bouncing. " +
             "Lower = faster buoyancy response (try 1.5–2.5). Higher = more stable.")]
    public float verticalDamping = 2f;

    [Tooltip("How smoothly the buoyancy transitions when pump value changes. " +
             "Lower = instant response, higher = floaty lag. Try 0.1–0.5.")]
    public float buoyancySmoothTime = 0.2f;

    [Header("Steering")]
    public float responsiveness      = 10f;
    public float yawResponsiveness   = 0.5f;
    public float pitchResponsiveness = 1f;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI hud;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private float targetForwardSpeed  = 0f;
    private float currentForwardSpeed = 0f;
    private float speedSmoothVelocity = 0f;
    private float forwardDecayTimer   = 0f;

    private float rawPumpValue        = 0f;   // written directly by ThrottlePump
    private float smoothedPumpValue   = 0f;   // lerped version used in physics
    private float pumpSmoothVelocity  = 0f;

    private float pitchInput = 0f;            // written by PitchSlider or keyboard
    private float yaw        = 0f;

    private Rigidbody rb;
    private float responseModifier => (rb.mass / 10f) * responsiveness;

    public static PlaneController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleInputs();
        UpdateHud();
    }

    private void FixedUpdate()
    {
        // Smooth the pump value here so buoyancy transitions are weighted
        smoothedPumpValue = Mathf.SmoothDamp(
            smoothedPumpValue, rawPumpValue,
            ref pumpSmoothVelocity, buoyancySmoothTime);

        ApplyForwardThrust();
        ApplyBuoyancy();
        ApplySteering();
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    private void HandleInputs()
    {
        // Yaw from steering wheel or keyboard fallback
        if (SteeringWheel.Instance != null)
            yaw = SteeringWheel.Instance.Value;
        else
            yaw = Input.GetAxis("Yaw");

        // Pitch from keyboard only if no UI slider is connected
        // (PitchSlider calls SetPitchInput() directly)
        if (PitchSlider.Instance == null)
            pitchInput = Input.GetAxis("Pitch");

        // Editor speed shortcuts
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetTargetSpeed(0.2f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetTargetSpeed(0.4f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetTargetSpeed(0.6f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetTargetSpeed(0.8f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetTargetSpeed(1.0f);
        #endif
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void SetTargetSpeed(float speedPercent)
    {
        targetForwardSpeed = maxForwardSpeed * Mathf.Clamp01(speedPercent);
        forwardDecayTimer  = forwardDecayDelay;
    }

    public void SetPumpValue(float value0to100)
    {
        rawPumpValue = value0to100;
    }

    public void SetPitchInput(float value)
    {
        // value comes in as 0–100 from the slider, remapped to -1…+1
        // 0   = nose up   (+1)
        // 50  = neutral   (0)
        // 100 = nose down (-1)
        pitchInput = -(value / 50f - 1f);
    }

    public void NotifyPump() { }

    // ── Physics ───────────────────────────────────────────────────────────────
    private void ApplyForwardThrust()
    {
        // Passive drain — always bleeds a small amount regardless of delay
        targetForwardSpeed = Mathf.Max(0f,
            targetForwardSpeed - passiveSpeedDrain * Time.fixedDeltaTime);

        // Decay delay — additional faster bleed after the grace period
        if (forwardDecayTimer > 0f)
        {
            forwardDecayTimer -= Time.fixedDeltaTime;
        }
        else
        {
            targetForwardSpeed = Mathf.Max(0f,
                targetForwardSpeed - forwardDecayRate * Time.fixedDeltaTime);
        }

        // Smooth actual speed toward target
        currentForwardSpeed = Mathf.SmoothDamp(
            currentForwardSpeed, targetForwardSpeed,
            ref speedSmoothVelocity, speedSmoothTime);

        // Apply along flat horizontal forward only — never affects vertical
        Vector3 flatForward    = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float   actualFwdSpeed = Vector3.Dot(rb.linearVelocity, flatForward);
        float   correction     = currentForwardSpeed - actualFwdSpeed;
        rb.AddForce(flatForward * correction, ForceMode.VelocityChange);
    }

    private void ApplyBuoyancy()
    {
        // Manual gravity (Rigidbody > Use Gravity must be OFF)
        rb.AddForce(Vector3.down * gravityForce);

        // Pump lift using the SMOOTHED value for weighted transitions
        // 0% pump  = 0 upward force → ship sinks
        // 50% pump = gravityForce   → neutral hover
        // 100% pump = 2x gravityForce → rising
        float pumpT     = smoothedPumpValue / 100f;
        float liftForce = maxPumpLift * pumpT;
        rb.AddForce(Vector3.up * liftForce);

        // Vertical damping — lower this to get faster buoyancy response
        float verticalVel = rb.linearVelocity.y;
        rb.AddForce(Vector3.up * (-verticalVel * verticalDamping));
    }

    private void ApplySteering()
    {
        rb.AddTorque(transform.up    * yaw        * responseModifier * yawResponsiveness);
        rb.AddTorque(transform.right * pitchInput * responseModifier * pitchResponsiveness);
    }

    // ── HUD ───────────────────────────────────────────────────────────────────
    private void UpdateHud()
    {
        if (hud == null) return;
        hud.text  = "Speed:    " + (currentForwardSpeed * 3.6f).ToString("F0") + " km/h\n";
        hud.text += "Altitude: " + transform.position.y.ToString("F0") + " m\n";
        hud.text += "Lift:     " + rawPumpValue.ToString("F0") + "%";
    }
}
