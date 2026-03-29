using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A world-space zone that waits for a sausage to land in it.
/// When triggered, notifies the DeliveryManager and deactivates itself.
/// </summary>
public class SausageDeliveryZone : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Optional indicator shown above the zone (arrow, ring, etc). " +
             "Gets hidden when zone is deactivated.")]
    public GameObject zoneIndicator;

    [Tooltip("Optional particle effect played on successful delivery.")]
    public GameObject deliveryEffect;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private bool hasBeenDelivered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenDelivered) return;
        if (!other.CompareTag("Sausage")) return;

        hasBeenDelivered = true;

        // Play effect at delivery point before destroying
        if (deliveryEffect != null)
            Instantiate(deliveryEffect, transform.position, Quaternion.identity);

        // Destroy the sausage
        Destroy(other.gameObject);

        // Tell the manager
        DeliveryManager.Instance.OnSausageDelivered(this);

        // Deactivate this zone
        Deactivate();
    }

    public void Deactivate()
    {
        // Hide the indicator and disable the collider
        if (zoneIndicator != null)
            zoneIndicator.SetActive(false);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Optionally dim the zone visuals
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Color dimmed = rend.material.color;
            dimmed.a = 0.2f;
            rend.material.color = dimmed;
        }
    }
}