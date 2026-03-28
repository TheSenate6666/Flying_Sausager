using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject controlsPanel;

    // ---- Button Callbacks ----

    // Play Button
    public void PlayGame()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextSceneIndex);
    }

    // Credits Button
    public void OpenCredits()
    {
        mainPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    // Controls Button
    public void OpenControls()
    {
        mainPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    // Back Button (from Credits or Controls)
    public void BackToMainMenu()
    {
        creditsPanel.SetActive(false);
        controlsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // Quit Button
    public void QuitGame()
    {
        Application.Quit();
    }
}