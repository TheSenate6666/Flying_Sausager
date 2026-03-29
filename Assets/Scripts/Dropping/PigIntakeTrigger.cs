using UnityEngine;

/// <summary>
/// Sits on a child GameObject with a Trigger collider.
/// Forwards OnTriggerEnter up to PigPickup on the parent plane.
/// This way the intake trigger is completely separate from the
/// plane's own physics collider.
/// </summary>
public class PigIntakeTrigger : MonoBehaviour
{
    private PigPickup pigPickup;

    private void Awake()
    {
        // Walk up to find PigPickup on the parent
        pigPickup = GetComponentInParent<PigPickup>();

        if (pigPickup == null)
            Debug.LogError("PigIntakeTrigger: no PigPickup found in parent!", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        pigPickup?.OnIntakeTriggerEnter(other);
    }
}
