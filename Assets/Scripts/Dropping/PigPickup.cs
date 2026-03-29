using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the full pig → sausage gameplay loop:
///   1. Trigger box in front of plane catches pigs
///   2. Caught pigs fly to the shredder and add to inventory
///   3. Drop button spawns a sausage prefab and removes one pig
/// </summary>
public class PigPickup : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("Transforms")]
    [Tooltip("Child transform at the shredder — pigs fly to this point.")]
    public Transform shredderPoint;

    [Tooltip("Child transform at the rear — sausages spawn here.")]
    public Transform dropPoint;

    [Header("Prefabs")]
    [Tooltip("The sausage prefab that gets dropped.")]
    public GameObject sausagePrefab;

    [Header("UI")]
    [Tooltip("Displays current pig count, e.g. 'Pigs: 3'")]
    public TMP_Text pigCountLabel;

    [Tooltip("The drop button — gets hidden when inventory is empty.")]
    public Button dropButton;

    // ── Tuning ────────────────────────────────────────────────────────────────
    [Header("Tuning")]
    [Tooltip("How fast each pig flies toward the shredder.")]
    public float pigSuckSpeed = 6f;

    [Tooltip("Force applied to the sausage when dropped (shoots it downward/backward).")]
    public float sausageDropForce = 5f;

    [Tooltip("Label prefix shown before the pig count number.")]
    public string countPrefix = "Pigs: ";

    // ── Runtime ───────────────────────────────────────────────────────────────
    private int pigCount = 0;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Wire the drop button
        if (dropButton != null)
            dropButton.onClick.AddListener(DropSausage);

        RefreshUI();
    }

    // ── Trigger ───────────────────────────────────────────────────────────────
    // The child GameObject with the trigger collider needs this script
    // OR you can use OnTriggerEnter directly here if the collider is on the plane.
    // See setup guide below for how to connect the child trigger.

    public void OnIntakeTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pig")) return;

        Pig pig = other.GetComponent<Pig>();
        if (pig == null) return;

        pig.suckSpeed = pigSuckSpeed;
        pig.GetSuckedIn(shredderPoint, this);
    }

    // ── Called by Pig when it reaches the shredder ────────────────────────────
    public void OnPigCollected()
    {
        pigCount++;
        RefreshUI();
    }

    // ── Drop sausage ──────────────────────────────────────────────────────────
    private void DropSausage()
    {
        if (pigCount <= 0 || sausagePrefab == null) return;

        // Spawn at drop point
        GameObject sausage = Instantiate(
            sausagePrefab,
            dropPoint.position,
            dropPoint.rotation);

        // Give it a physics kick so it falls away from the plane
        Rigidbody sausageRb = sausage.GetComponent<Rigidbody>();
        if (sausageRb != null)
        {
            // Drop backward and downward relative to the plane
            Vector3 dropDirection = (-transform.forward + Vector3.down).normalized;
            sausageRb.AddForce(dropDirection * sausageDropForce, ForceMode.Impulse);
        }

        pigCount--;
        RefreshUI();
    }

    // ── UI ────────────────────────────────────────────────────────────────────
    private void RefreshUI()
    {
        if (pigCountLabel != null)
            pigCountLabel.text = countPrefix + pigCount.ToString();

        // Hide drop button when nothing to drop
        if (dropButton != null)
            dropButton.gameObject.SetActive(pigCount > 0);
    }
}
