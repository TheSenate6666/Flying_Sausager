using UnityEngine;

public class PlaneCameraController : MonoBehaviour
{
    [SerializeField] private Transform[] povs;
    [Tooltip("Camera Speed follows the Plane")]
    [SerializeField] private float Speed = 10f;

    [SerializeField] private int startIndex = 0;

    private int index;
    private Vector3 target;

    private void Start()
    {
        if (povs == null || povs.Length == 0)
        {
            enabled = false;
            return;
        }

        index = Mathf.Clamp(startIndex, 0, povs.Length - 1);
        target = povs[index].position;
    }

    private void Update()
    {
        if (povs == null || povs.Length == 0) return;
        target = povs[index].position;
    }

    private void FixedUpdate()
    {
        if (povs == null || povs.Length == 0) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            Time.fixedDeltaTime * Speed
        );

        transform.forward = povs[index].forward;
    }

    public void NextPov()
    {
        if (povs == null || povs.Length == 0) return;

        index = (index + 1) % povs.Length;
        target = povs[index].position;
    }

    public void PreviousPov()
    {
        if (povs == null || povs.Length == 0) return;

        index = (index - 1 + povs.Length) % povs.Length;
        target = povs[index].position;
    }
}