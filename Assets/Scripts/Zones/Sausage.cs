using UnityEngine;

/// <summary>
/// Sits on the sausage prefab.
/// Handles lifetime cleanup so missed sausages don't litter the world forever.
/// The actual delivery detection is handled by SausageDeliveryZone's trigger.
/// </summary>
public class Sausage : MonoBehaviour
{
    [Tooltip("Seconds before a sausage that never lands auto-destroys. " +
             "Prevents world clutter. Set to 0 to disable.")]
    public float lifetime = 15f;

    private void Start()
    {
        // Make sure the tag is set — belt and braces
        gameObject.tag = "Sausage";

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }
}
