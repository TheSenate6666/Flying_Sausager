using UnityEngine;

public class PlaneController : MonoBehaviour
{
    [Header("Player Settings")]
    public float maxThrottle = 100f;
    public float throttleIncrement = 10f;
    public float responsiveness = 10f;

    private float throttle;
    private float roll;
    private float pitch;
    private float yaw;

    private Rigidbody rb;

    private float responseModifier => (rb.mass / 10f) * responsiveness;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleInputs();
    }

    private void FixedUpdate()
    {
        rb.AddForce(transform.forward * maxThrottle * throttle);
        rb.AddTorque(transform.up * yaw * responseModifier);
        rb.AddTorque(transform.right * pitch * responseModifier);
        //rb.AddTorque(transform.forward * roll * responseModifier);
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
}