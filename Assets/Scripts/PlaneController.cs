using UnityEngine;
using TMPro;
using SimpleInputNamespace;

public class PlaneController : MonoBehaviour
{
    [Header("Player Settings")]
    public float maxThrottle = 100f;
    public float throttleIncrement = 10f;
    public float responsiveness = 10f;
    public float lift = 30f;

    [Header("System Responsive Modifier")]
    public float yawresponsiveness = 1f;
    public float pitchresponsiveness = 1f;

    [Header("Takeoff Boost")]
    [Tooltip("Extra upward force applied only while below takeoffSpeedThreshold km/h")]
    public float takeoffBoostForce = 60f;
    [Tooltip("Boost fades out completely once the plane reaches this speed (km/h)")]
    public float takeoffSpeedThreshold = 120f;

    private float throttle;
    private float roll;
    private float pitch;
    private float yaw;

    private Rigidbody rb;
    private float responseModifier => (rb.mass / 10f) * responsiveness;

    [SerializeField] private TextMeshProUGUI hud;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleInputs();
        UpdateHud();
    }

    private void FixedUpdate()
    {
        rb.AddForce(transform.forward * maxThrottle * throttle);
        rb.AddTorque(transform.up * yaw * responseModifier * yawresponsiveness);
        rb.AddTorque(transform.right * pitch * responseModifier * pitchresponsiveness);

        // ── Normal lift (unchanged) ──────────────────────────────────────────────
        rb.AddForce(Vector3.up * rb.linearVelocity.magnitude * lift);

        // ── Takeoff boost: extra lift that fades as speed builds ─────────────────
        // t goes from 1 (stationary) → 0 (at/above threshold), so boost is zero
        // during cruising and never interferes with your tuned lift value.
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        float boostT = Mathf.Clamp01(1f - (speedKmh / takeoffSpeedThreshold));
        rb.AddForce(Vector3.up * takeoffBoostForce * boostT);
    }

    private void HandleInputs()
    {
        roll  = Input.GetAxis("Roll");
        pitch = Input.GetAxis("Pitch");

        // ── Yaw: read from the steering wheel instead of the Yaw axis ────────────
        if (SteeringWheel.Instance != null)
            yaw = SteeringWheel.Instance.Value;
        else
            yaw = Input.GetAxis("Yaw");   // keyboard fallback (Q/E still work)

        if (Input.GetKey(KeyCode.Space))
            throttle += throttleIncrement * Time.deltaTime;
        else if (Input.GetKey(KeyCode.LeftShift))
            throttle -= throttleIncrement * Time.deltaTime;

        throttle = Mathf.Clamp(throttle, 0f, 100f);
    }

    private void UpdateHud()
    {
        if (hud == null) return;

        hud.text  = "Throttle: " + throttle.ToString("F0") + "%\n";
        hud.text += "Airspeed: " + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + " km/h\n";
        hud.text += "Altitude: " + transform.position.y.ToString("F0") + " m";
    }
}