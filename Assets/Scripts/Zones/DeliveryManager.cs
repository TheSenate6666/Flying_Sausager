using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Tracks sausage deliveries across all zones.
/// Ends the game and loads MainMenu when the target is reached.
/// </summary>
public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    // ── Settings ──────────────────────────────────────────────────────────────
    [Header("Win Condition")]
    [Tooltip("How many sausages must be delivered to win.")]
    public int deliveriesRequired = 3;

    [Tooltip("Delay in seconds after the final delivery before the scene loads.")]
    public float endGameDelay = 2f;

    [Tooltip("Exact name of the scene to load on game end. Must be in Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("UI")]
    [Tooltip("Shows current delivery progress, e.g. 'Deliveries: 1 / 3'")]
    public TMP_Text deliveryCountLabel;

    [Tooltip("Panel shown when the player wins (optional).")]
    public GameObject winPanel;

    [Tooltip("Text inside the win panel (optional).")]

    

    
    public GameObject playerVisuals;

    [Tooltip("VFX prefab spawned at the player's position on self destruct.")]
    public GameObject destructionVFX;

    [Tooltip("Where the player respawns after self destructing.")]
    public Transform respawnPoint;

    [Tooltip("Reference to the plane's Rigidbody to reset velocity on respawn.")]
    public Rigidbody planeRigidbody;

    [Tooltip("Seconds between explosion and respawn.")]
    public float respawnDelay = 2f;
        public TMP_Text winText;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private int  deliveryCount  = 0;
    private bool gameEnded      = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        RefreshUI();
    }

    // ── Called by SausageDeliveryZone ─────────────────────────────────────────
    public void OnSausageDelivered(SausageDeliveryZone zone)
    {
        if (gameEnded) return;

        deliveryCount++;
        RefreshUI();

        Debug.Log($"Delivery {deliveryCount} / {deliveriesRequired} complete!");

        if (deliveryCount >= deliveriesRequired)
            StartCoroutine(EndGame());
    }

    // ── Win sequence ──────────────────────────────────────────────────────────
    private System.Collections.IEnumerator EndGame()
    {
        gameEnded = true;

        // Show win screen
        if (winPanel != null)
            winPanel.SetActive(true);

        if (winText != null)
            winText.text = "All sausages delivered!\nReturning to menu...";

        yield return new WaitForSeconds(endGameDelay);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── UI ────────────────────────────────────────────────────────────────────
    private void RefreshUI()
    {
        if (deliveryCountLabel != null)
            deliveryCountLabel.text = $"Deliveries: {deliveryCount} / {deliveriesRequired}";
    }

    public void SelfDestruct()
    {
        if (gameEnded) return;
        StartCoroutine(SelfDestructSequence());
    }

    private System.Collections.IEnumerator SelfDestructSequence()
    {
        // ── Death ─────────────────────────────────────────────────────────────
        // Spawn explosion VFX at current player position
        if (destructionVFX != null && planeRigidbody != null)
            Instantiate(destructionVFX, planeRigidbody.transform.position, Quaternion.identity);

        // Hide visuals
        if (playerVisuals != null)
            playerVisuals.SetActive(false);

        // Kill all momentum so the rigidbody doesn't drift while hidden
        if (planeRigidbody != null)
        {
            planeRigidbody.linearVelocity        = Vector3.zero;
            planeRigidbody.angularVelocity   = Vector3.zero;
            planeRigidbody.isKinematic       = true;    // freeze physics while hidden
        }

        yield return new WaitForSeconds(respawnDelay);

        // ── Respawn ───────────────────────────────────────────────────────────
        if (planeRigidbody != null && respawnPoint != null)
        {
            // Teleport to respawn point
            planeRigidbody.transform.position = respawnPoint.position;
            planeRigidbody.transform.rotation = respawnPoint.rotation;

            // Re-enable physics and clear any residual velocity
            planeRigidbody.isKinematic      = false;
            planeRigidbody.linearVelocity       = Vector3.zero;
            planeRigidbody.angularVelocity  = Vector3.zero;
        }

        // Show visuals again
        if (playerVisuals != null)
            playerVisuals.SetActive(true);
    }
}