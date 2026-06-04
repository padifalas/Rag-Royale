using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Master Volumes")]
    [SerializeField, Range(0f, 1f)]
    private float masterVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float musicVolume = 0.7f;

    [SerializeField, Range(0f, 1f)]
    private float sfxVolume = 1f;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = value;
            RefreshMusicVolume();
        }
    }
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = value;
            RefreshMusicVolume();
        }
    }
    public float SFXVolume
    {
        get => sfxVolume;
        set => sfxVolume = value;
    }

    [Header("Music  for Round")]
    public AudioClip[] roundTracks;

    [SerializeField]
    private float crossfadeDuration = 1.5f;

    [Header("Combat SFX")]
    public AudioClip lightHit;
    public AudioClip heavyHit;
    public AudioClip blockImpact;

    [Header("Knockdown SFX")]
    public AudioClip knockdownFall;
    public AudioClip mashGetUp;
    public AudioClip standUpSuccess;

    [Header("Arm SFX")]
    public AudioClip armDetach;
    public AudioClip stringPullLoop;

    [Header("Killer Shot SFX")]
    public AudioClip killerShotTrigger;
    public AudioClip killerShotWin;
    public AudioClip killerShotEarly;
    public AudioClip killerShotPerfect;

    [Header("Round SFX")]
    public AudioClip roundStartBell;
    public AudioClip roundWinChant;
    public AudioClip matchWinChant;
    public AudioClip countdownTick;
    public AudioClip countdownFinalTick;

    [Header("Round 2 - Needle  Clips")]
    public AudioClip needleCollect;
    public AudioClip needleDeposit;
    public AudioClip needleStolen;

    [Header("Round 2 -Spatial Sources")]
    public AudioSource p1SpatialSource;

    [Tooltip("AudioSource positioned at P2's side of the arena (right).")]
    public AudioSource p2SpatialSource;

    [Header("UI SFX")]
    public AudioClip menuNavigate;
    public AudioClip menuConfirm;
    public AudioClip menuBack;

    [Header("Ambient")]
    public AudioClip ambientLoop;

    [Header("SFX Pool")]
    [SerializeField]
    private int sfxPoolSize = 16;

    private List<AudioSource> sfxPool = new List<AudioSource>();
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool musicOnA = true;
    private AudioSource ambientSource;
    private AudioSource stringLoopSource;

    private float musicTargetVolume = 1f;
    private Coroutine musicFadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSFXPool();
        BuildMusicSources();
        BuildAmbientSource();
        BuildStringLoopSource();
    }

    private void BuildSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sfxPool.Add(src);
        }
    }

    private void BuildMusicSources()
    {
        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceA.loop = true;
        musicSourceA.playOnAwake = false;
        musicSourceA.volume = 0f;

        musicSourceB = gameObject.AddComponent<AudioSource>();
        musicSourceB.loop = true;
        musicSourceB.playOnAwake = false;
        musicSourceB.volume = 0f;
    }

    private void BuildAmbientSource()
    {
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.clip = ambientLoop;
        ambientSource.loop = true;
        ambientSource.playOnAwake = false;
        ambientSource.volume = 0f;
    }

    private void BuildStringLoopSource()
    {
        stringLoopSource = gameObject.AddComponent<AudioSource>();
        stringLoopSource.loop = true;
        stringLoopSource.playOnAwake = false;
        stringLoopSource.volume = 0f;
    }

    public void CrossfadeToRound(int roundIndex)
    {
        if (roundTracks == null || roundIndex < 0 || roundIndex >= roundTracks.Length)
        {
            Debug.LogWarning($"[AudioManager] No track for round index {roundIndex}.");
            return;
        }

        AudioClip nextClip = roundTracks[roundIndex];
        if (nextClip == null)
            return;

        AudioSource fadeOut = musicOnA ? musicSourceA : musicSourceB;
        AudioSource fadeIn = musicOnA ? musicSourceB : musicSourceA;
        musicOnA = !musicOnA;

        fadeIn.clip = nextClip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(CrossfadeRoutine(fadeOut, fadeIn));
    }

    private IEnumerator CrossfadeRoutine(AudioSource fadeOut, AudioSource fadeIn)
    {
        float targetVol = masterVolume * musicVolume * musicTargetVolume;
        float elapsed = 0f;
        float startOut = fadeOut.volume;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);
            fadeIn.volume = Mathf.Lerp(0f, targetVol, t);
            fadeOut.volume = Mathf.Lerp(startOut, 0f, t);
            yield return null;
        }

        fadeIn.volume = targetVol;
        fadeOut.volume = 0f;
        fadeOut.Stop();
        fadeOut.clip = null;
        musicFadeCoroutine = null;
    }

    public void SetMusicVolume(float targetVolume, float fadeDuration = 1f)
    {
        musicTargetVolume = targetVolume;
        AudioSource active = musicOnA ? musicSourceB : musicSourceA;
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(
            FadeSource(active, masterVolume * musicVolume * targetVolume, fadeDuration)
        );
    }

    private void RefreshMusicVolume()
    {
        float vol = masterVolume * musicVolume * musicTargetVolume;
        musicSourceA.volume = musicSourceA.isPlaying ? vol : 0f;
        musicSourceB.volume = musicSourceB.isPlaying ? vol : 0f;
        ambientSource.volume = masterVolume * musicVolume * 0.35f;
    }

    public void StartAmbient(float fadeDuration = 2f)
    {
        if (ambientLoop == null)
            return;
        if (!ambientSource.isPlaying)
            ambientSource.Play();
        StartCoroutine(FadeSource(ambientSource, masterVolume * musicVolume * 0.35f, fadeDuration));
    }

    public void StopAmbient(float fadeDuration = 1.5f)
    {
        StartCoroutine(FadeSource(ambientSource, 0f, fadeDuration, stopOnComplete: true));
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariance = 0f)
    {
        if (clip == null)
            return;
        AudioSource src = GetFreeSFXSource();
        src.clip = clip;
        src.volume = masterVolume * sfxVolume * volume;
        src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        src.spatialBlend = 0f; // 2D
        src.Play();
    }

    public void Play(AudioClip clip, float volume = 1f, float pitchVariance = 0f) =>
        PlaySFX(clip, volume, pitchVariance);

    private AudioSource GetFreeSFXSource()
    {
        foreach (var s in sfxPool)
            if (!s.isPlaying)
                return s;
        return sfxPool[0];
    }

    public void PlayNeedleSFX(int playerID, AudioClip clip, float pitchVariance = 0.08f)
    {
        if (clip == null)
            return;

        AudioSource src = playerID == 1 ? p1SpatialSource : p2SpatialSource;
        if (src == null)
        {
            PlaySFX(clip, 1f, pitchVariance);
            return;
        }

        src.clip = clip;
        src.volume = masterVolume * sfxVolume;
        src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        src.Play();
    }

    public void PlayNeedleCollect(int playerID) => PlayNeedleSFX(playerID, needleCollect);

    public void PlayNeedleDeposit(int playerID) => PlayNeedleSFX(playerID, needleDeposit);

    public void PlayNeedleStolen()
    {
        PlayNeedleSFX(1, needleStolen, 0f);
        PlayNeedleSFX(2, needleStolen, 0f);
    }

    public void StartStringPullLoop()
    {
        if (stringPullLoop == null || stringLoopSource.isPlaying)
            return;
        stringLoopSource.clip = stringPullLoop;
        stringLoopSource.volume = masterVolume * sfxVolume * 0.55f;
        stringLoopSource.Play();
    }

    public void StopStringPullLoop() => stringLoopSource.Stop();

    private IEnumerator FadeSource(
        AudioSource src,
        float targetVol,
        float duration,
        bool stopOnComplete = false
    )
    {
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(startVol, targetVol, elapsed / duration);
            yield return null;
        }
        src.volume = targetVol;
        if (stopOnComplete)
            src.Stop();
    }

    public void RegisterSpatialSources(AudioSource p1, AudioSource p2)
    {
        p1SpatialSource = p1;
        p2SpatialSource = p2;
    }
}
