using UnityEngine;

/// <summary>
/// Sits on each Pig GameObject.
/// When the plane's shredder trigger catches it, the pig gets
/// sucked toward the shredder transform and then disappears.
/// </summary>
public class Pig : MonoBehaviour
{
    [Tooltip("How fast the pig flies toward the shredder when caught.")]
    public float suckSpeed = 5f;

    [Tooltip("How close the pig needs to get before it counts as collected.")]
    public float collectDistance = 0.3f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Transform  target;          // the shredder transform on the plane
    private bool       isBeingSucked = false;
    private PigPickup  collector;

    /// <summary>Called by PigPickup when the pig enters the trigger.</summary>
    public void GetSuckedIn(Transform shredder, PigPickup pigPickup)
    {
        if (isBeingSucked) return;      // ignore double-triggers

        target       = shredder;
        collector    = pigPickup;
        isBeingSucked = true;

        // Disable physics so the pig doesn't fight the movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.isKinematic = true;
        }

        // Disable any pig AI or animation here if you have it
    }

    private void Update()
    {
        if (!isBeingSucked || target == null) return;

        // Fly toward the shredder
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            suckSpeed * Time.deltaTime);

        // Shrink as it gets closer — optional but looks great
        float dist        = Vector3.Distance(transform.position, target.position);
        float totalDist   = Mathf.Max(dist, 0.01f);    // avoid divide-by-zero
        float scaleFactor = Mathf.Clamp01(dist / 3f);  // shrinks over last 3 units
        transform.localScale = Vector3.one * scaleFactor;

        // Close enough — collected!
        if (dist <= collectDistance)
        {
            collector.OnPigCollected();
            Destroy(gameObject);
        }
    }


    private void OnDrawGizmosSelected()
{
    // Only draw when object is selected (less clutter)
    Gizmos.color = Color.yellow;

    // If we have a target, draw around it (more accurate during play)
    if (target != null)
    {
        Gizmos.DrawWireSphere(target.position, collectDistance);
    }
    else
    {
        // Fallback: draw around the pig itself in edit mode
        Gizmos.DrawWireSphere(transform.position, collectDistance);
    }
}
}
