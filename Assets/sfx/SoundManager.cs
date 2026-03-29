using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Setup")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AirshipGame.Sound[] soundLibrary;

    private Dictionary<string, AirshipGame.Sound> soundDictionary 
        = new Dictionary<string, AirshipGame.Sound>();

    [Header("Combo System")]
    [SerializeField] private string enemyDeathSoundName  = "EnemyDied";
    [SerializeField] private string damageTakenSoundName = "DamageTaken";

    private float pitchMlt        = 1f;
    private float maxPitchMult    = 4f;
    private float pitchStepMlt    = 1.25f;
    private float pitchResetTime  = 0.5f;
    private float timeSinceLastKill = 0f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
   private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    else
    {
        Debug.Log("SoundManager: Duplicate found, destroying this one.");
        Destroy(gameObject);
        return;
    }

    // Check music source is assigned
    if (musicSource == null)
        Debug.LogError("SoundManager: Music Source AudioSource is NOT assigned in the Inspector!");
    else
        Debug.Log("SoundManager: Music Source found OK.");

    // Check library
    if (soundLibrary == null || soundLibrary.Length == 0)
        Debug.LogError("SoundManager: Sound Library is empty! Add sounds in the Inspector.");
    else
        Debug.Log($"SoundManager: Library has {soundLibrary.Length} sounds. Initializing...");

    InitializeLibrary();

    Debug.Log($"SoundManager: Dictionary contains {soundDictionary.Count} entries:");
    foreach (var key in soundDictionary.Keys)
        Debug.Log($"  → '{key}'");

    PlayMusic("Music");
}

    private void Update()
    {
        timeSinceLastKill += Time.deltaTime;

        if (timeSinceLastKill > pitchResetTime)
            pitchMlt = 1f;
    }

    // ── Library ───────────────────────────────────────────────────────────────
    private void InitializeLibrary()
    {
        soundDictionary.Clear();

        foreach (AirshipGame.Sound s in soundLibrary)
        {
            if (!string.IsNullOrEmpty(s.name) && !soundDictionary.ContainsKey(s.name))
                soundDictionary.Add(s.name, s);
        }
    }

    // ── Music ─────────────────────────────────────────────────────────────────
    public void PlayMusic(string soundName)
{
    Debug.Log($"SoundManager: Trying to play music '{soundName}'...");

    if (musicSource == null)
    {
        Debug.LogError("SoundManager: Cannot play music — Music Source is null!");
        return;
    }

    if (!soundDictionary.TryGetValue(soundName, out AirshipGame.Sound s))
    {
        Debug.LogError($"SoundManager: Music '{soundName}' not found in dictionary. " +
                       $"Make sure an entry named exactly '{soundName}' exists in the Sound Library.");
        return;
    }

    if (s.clip == null)
    {
        Debug.LogError($"SoundManager: Sound '{soundName}' was found but its AudioClip is null! " +
                       "Assign a clip to this entry in the Inspector.");
        return;
    }

    if (musicSource.clip == s.clip && musicSource.isPlaying)
    {
        Debug.Log($"SoundManager: '{soundName}' is already playing, skipping.");
        return;
    }

    Debug.Log($"SoundManager: Playing music '{soundName}', clip: '{s.clip.name}', " +
              $"volume: {s.volume}, pitch: {s.pitch}");

    musicSource.clip   = s.clip;
    musicSource.volume = s.volume;
    musicSource.pitch  = s.pitch;
    musicSource.loop   = true;
    musicSource.Play();

    Debug.Log($"SoundManager: musicSource.isPlaying = {musicSource.isPlaying}");
}

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // ── SFX ───────────────────────────────────────────────────────────────────

    /// <summary>Play a sound from the library by name.</summary>
    public void PlaySFX(string soundName)
    {
        if (!soundDictionary.TryGetValue(soundName, out AirshipGame.Sound s))
        {
            Debug.LogWarning($"SoundManager: SFX '{soundName}' not found in library.");
            return;
        }

        Play2DClip(s.clip, s.volume, s.pitch);
    }

    /// <summary>Play a sound with optional volume scale and pitch override.</summary>
    public void PlaySFX(string soundName, float volumeScale, float pitchOverride = -1f)
    {
        if (!soundDictionary.TryGetValue(soundName, out AirshipGame.Sound s))
        {
            Debug.LogWarning($"SoundManager: SFX '{soundName}' not found in library.");
            return;
        }

        float finalPitch = pitchOverride > 0f ? pitchOverride : s.pitch;
        Play2DClip(s.clip, s.volume * volumeScale, finalPitch);
    }

    /// <summary>Play a sound at a world position (3D spatialised).</summary>
    public void PlaySFXAtPosition(string soundName, Vector3 worldPosition)
    {
        if (!soundDictionary.TryGetValue(soundName, out AirshipGame.Sound s))
        {
            Debug.LogWarning($"SoundManager: SFX '{soundName}' not found in library.");
            return;
        }

        Play3DClip(s.clip, s.volume, s.pitch, worldPosition);
    }

    // ── Combo ─────────────────────────────────────────────────────────────────
    public void PlayEnemyDiedSound()
    {
        if (!soundDictionary.TryGetValue(enemyDeathSoundName, out AirshipGame.Sound s))
        {
            Debug.LogWarning($"SoundManager: Enemy death sound '{enemyDeathSoundName}' not found.");
            return;
        }

        Play2DClip(s.clip, s.volume, pitchMlt);

        pitchMlt = Mathf.Min(pitchMlt * pitchStepMlt, maxPitchMult);
        timeSinceLastKill = 0f;
    }

    public void PlayDamageTakenSound()
    {
        PlaySFX(damageTakenSoundName);
    }

    // ── Internal playback ─────────────────────────────────────────────────────
    private void Play2DClip(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;

        GameObject go        = new GameObject($"SFX_2D_{clip.name}");
        AudioSource source   = go.AddComponent<AudioSource>();

        source.clip          = clip;
        source.volume        = volume;
        source.pitch         = pitch;
        source.spatialBlend  = 0f;      // fully 2D
        source.Play();

        Destroy(go, clip.length / Mathf.Abs(pitch) + 0.1f);
    }

    private void Play3DClip(AudioClip clip, float volume, float pitch, Vector3 position)
    {
        if (clip == null) return;

        GameObject go        = new GameObject($"SFX_3D_{clip.name}");
        go.transform.position = position;
        AudioSource source   = go.AddComponent<AudioSource>();

        source.clip          = clip;
        source.volume        = volume;
        source.pitch         = pitch;
        source.spatialBlend  = 1f;      // fully 3D
        source.rolloffMode   = AudioRolloffMode.Linear;
        source.maxDistance   = 50f;
        source.Play();

        Destroy(go, clip.length / Mathf.Abs(pitch) + 0.1f);
    }
}