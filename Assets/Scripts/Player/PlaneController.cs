using UnityEngine;
using TMPro;
using SimpleInputNamespace;

public class PlaneController : MonoBehaviour
{
    [Header("Forward Thrust")]
    [Tooltip("Target speed added per button press (m/s).")]
    public float thrustImpulse = 5f;

    [Tooltip("Maximum forward speed (m/s). 40 = ~144 km/h")]
    public float maxForwardSpeed = 40f;

    [Tooltip("Seconds after last button press before speed target starts decaying.")]
    public float forwardDecayDelay = 3f;

    [Tooltip("m/s lost per second once decay kicks in.")]
    public float forwardDecayRate = 2f;

    [Tooltip("How smoothly actual velocity catches up to target speed. " +
             "Lower = snappier, higher = sluggish. 0.5–2.0 is a good range.")]
    public float speedSmoothTime = 0.8f;

    [Header("Buoyancy (Pump-controlled)")]
    [Tooltip("Downward force per second. Set this to rb.mass * 9.81 to match Unity gravity. " +
             "The pump must overcome this to rise.")]
    public float gravityForce = 49f;

    [Tooltip("Maximum upward force at 100% pump. " +
             "Set to 2x gravityForce so that 50% pump = neutral hover.")]
    public float maxPumpLift = 98f;

    [Tooltip("Dampens vertical velocity so the ship doesn't bounce past target height. " +
             "3–6 is a good range.")]
    public float verticalDamping = 4f;

    [Header("Steering")]
    public float responsiveness     = 10f;
    public float yawResponsiveness  = 1f;
    public float pitchResponsiveness = 1f;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI hud;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private float targetForwardSpeed  = 0f;   // what we're aiming for
    private float currentForwardSpeed = 0f;   // smoothed actual speed
    private float speedSmoothVelocity = 0f;   // internal SmoothDamp state
    private float forwardDecayTimer   = 0f;

    private float pumpValue = 0f;             // 0–100, set by ThrottlePump
    private float pitch;
    private float yaw;

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
        ApplyForwardThrust();
        ApplyBuoyancy();
        ApplySteering();
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    private void HandleInputs()
    {
        pitch = Input.GetAxis("Pitch");

        if (SteeringWheel.Instance != null)
            yaw = SteeringWheel.Instance.Value;
        else
            yaw = Input.GetAxis("Yaw");

        if (ThrottlePump.Instance == null && Input.GetKeyDown(KeyCode.Space))
            AddThrust();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void AddThrust()
    {
        // Add to the TARGET, not the current speed — feels like a push, not a teleport
        targetForwardSpeed = Mathf.Min(targetForwardSpeed + thrustImpulse, maxForwardSpeed);
        forwardDecayTimer  = forwardDecayDelay;
    }

    public void SetPumpValue(float value0to100)
    {
        pumpValue = value0to100;
    }

    public void NotifyPump()
    {
        // ThrottlePump calls this on each stroke — could trigger effects here later
    }

    // ── Physics ───────────────────────────────────────────────────────────────
    private void ApplyForwardThrust()
    {
        // Tick decay delay
        if (forwardDecayTimer > 0f)
        {
            forwardDecayTimer -= Time.fixedDeltaTime;
        }
        else
        {
            // Bleed the TARGET down — smooth follow will naturally slow the ship
            targetForwardSpeed = Mathf.Max(0f,
                targetForwardSpeed - forwardDecayRate * Time.fixedDeltaTime);
        }

        // Smoothly move current speed toward the target
        currentForwardSpeed = Mathf.SmoothDamp(
            currentForwardSpeed,
            targetForwardSpeed,
            ref speedSmoothVelocity,
            speedSmoothTime);

        // Apply only along the flat horizontal forward axis
        // so this never interferes with vertical movement
        Vector3 flatForward     = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float   actualFwdSpeed  = Vector3.Dot(rb.linearVelocity, flatForward);
        float   correction      = currentForwardSpeed - actualFwdSpeed;

        rb.AddForce(flatForward * correction, ForceMode.VelocityChange);
    }

    private void ApplyBuoyancy()
    {
        // Always apply gravity manually (you can disable Unity's built-in gravity
        // on this Rigidbody in the Inspector and use this instead for full control,
        // or leave Unity gravity on and just tune gravityForce to 0 — see note below)
        //
        // ── IMPORTANT ────────────────────────────────────────────────────────
        // Set Rigidbody > Use Gravity = FALSE in the Inspector.
        // This script handles gravity manually so pump can cleanly fight it.
        // ─────────────────────────────────────────────────────────────────────
        rb.AddForce(Vector3.down * gravityForce);

        // Pump lift: at 0% pump = 0 upward force (gravity wins, ship sinks)
        //            at 50% pump = gravityForce upward (neutral hover)
        //            at 100% pump = 2x gravityForce (rising fast)
        float pumpT     = pumpValue / 100f;
        float liftForce = maxPumpLift * pumpT;          // maxPumpLift = 2 * gravityForce
        rb.AddForce(Vector3.up * liftForce);

        // Damp vertical velocity to prevent oscillation
        float verticalVel = rb.linearVelocity.y;
        rb.AddForce(Vector3.up * (-verticalVel * verticalDamping));
    }

    private void ApplySteering()
    {
        rb.AddTorque(transform.up    * yaw   * responseModifier * yawResponsiveness);
        rb.AddTorque(transform.right * pitch * responseModifier * pitchResponsiveness);
    }

    // ── HUD ───────────────────────────────────────────────────────────────────
    private void UpdateHud()
    {
        if (hud == null) return;
        hud.text  = "Speed:    " + (currentForwardSpeed * 3.6f).ToString("F0") + " km/h\n";
        hud.text += "Altitude: " + transform.position.y.ToString("F0") + " m\n";
        hud.text += "Lift:     " + pumpValue.ToString("F0") + "%";
    }
}
