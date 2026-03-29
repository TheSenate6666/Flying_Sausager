using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The name must match an entry in SoundManager's Library")]
    [SerializeField] private string soundName;

    [Range(0f, 1f)]
    public float volumeMultiplier = 1f;

    public void PlaySound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(soundName, volumeMultiplier);
        else
            Debug.LogWarning("SoundEmitter: No SoundManager in scene!");
    }

    public void PlayAsMusic()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayMusic(soundName);
        else
            Debug.LogWarning("SoundEmitter: No SoundManager in scene!");
    }

    public void SetSoundName(string newName)
    {
        soundName = newName;
    }
}