using UnityEngine;
using TMPro;

public class PlaneController : MonoBehaviour
{
    [Header("Player Settings")]
    public float maxThrottle = 100f;
    public float throttleIncrement = 10f;
    public float responsiveness = 10f;
    public float lift = 30f;

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
        rb.AddTorque(transform.up * yaw * responseModifier);
        rb.AddTorque(transform.right * pitch * responseModifier);
        rb.AddForce(Vector3.up * rb.linearVelocity.magnitude * lift);
    }

    private void HandleInputs()
    {
        roll = Input.GetAxis("Roll");
        yaw = Input.GetAxis("Yaw");
        pitch = Input.GetAxis("Pitch");

        if (Input.GetKey(KeyCode.Space))
            throttle += throttleIncrement * Time.deltaTime;
        else if (Input.GetKey(KeyCode.LeftShift))
            throttle -= throttleIncrement * Time.deltaTime;

        throttle = Mathf.Clamp(throttle, 0f, 100f);

        Debug.Log("Speed: " + throttle);
    }

    private void UpdateHud()
    {
        if (hud == null) return;

        hud.text = "Throttle: " + throttle.ToString("F0") + "%\n";
        hud.text += "Airspeed: " + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + " km/h";
        hud.text += "Altitude: " + (transform.position.y).ToString("F0") + " m";
    }
}