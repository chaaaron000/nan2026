using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM과 효과음의 재생, 동시 재생, 개별 볼륨을 전역에서 관리한다.
/// </summary>
public sealed class SoundManager : MonoBehaviour
{
    private const string BgmVolumePreferenceKey = "sound.bgm_volume";
    private const string SfxVolumePreferenceKey = "sound.sfx_volume";

    /// <summary>
    /// 현재 씬에서 사용하는 사운드 매니저 인스턴스를 반환한다.
    /// </summary>
    public static SoundManager Instance { get; private set; }

    [SerializeField]
    private SoundLibrary soundLibrary;

    [SerializeField, Min(3)]
    private int sfxSourceCount = 3;

    [SerializeField, Range(0f, 1f)]
    private float defaultBgmVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float defaultSfxVolume = 1f;

    private AudioSource bgmSource;
    private readonly List<AudioSource> sfxSources = new();
    private int nextSfxSourceIndex;
    private AudioClip currentBgmClip;
    private float bgmVolume;
    private float sfxVolume;

    /// <summary>
    /// 현재 적용된 BGM 볼륨을 반환한다.
    /// </summary>
    public float BgmVolume => bgmVolume;

    /// <summary>
    /// 현재 적용된 효과음 볼륨을 반환한다.
    /// </summary>
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateAudioSources();
        LoadVolumes();
        ApplyVolumes();
    }

    /// <summary>
    /// 문자열 키에 연결된 BGM을 반복 재생한다.
    /// 이미 같은 곡이 재생 중이면 재생 위치를 유지한다.
    /// </summary>
    /// <param name="key">SoundLibrary에서 찾을 BGM 키.</param>
    public void PlayBgm(string key)
    {
        if (!TryGetClip(key, out AudioClip clip))
        {
            return;
        }

        if (currentBgmClip == clip && bgmSource.isPlaying)
        {
            return;
        }

        currentBgmClip = clip;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// 문자열 키에 연결된 효과음을 사용 가능한 SFX 소스에서 재생한다.
    /// 여러 효과음이 겹쳐도 기존 재생을 중단하지 않는다.
    /// </summary>
    /// <param name="key">SoundLibrary에서 찾을 효과음 키.</param>
    /// <param name="volumeScale">이번 재생에만 적용할 추가 볼륨 배율.</param>
    public void PlaySfx(string key, float volumeScale = 1f)
    {
        if (!TryGetClip(key, out AudioClip clip)
            || sfxSources.Count == 0)
        {
            return;
        }

        AudioSource source = sfxSources[nextSfxSourceIndex];
        nextSfxSourceIndex =
            (nextSfxSourceIndex + 1) % sfxSources.Count;

        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    /// <summary>
    /// 현재 재생 중인 BGM을 정지하고 재생 곡을 비운다.
    /// </summary>
    public void StopBgm()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgmClip = null;
    }

    /// <summary>
    /// BGM 볼륨을 설정하고 브라우저 저장소에 보존한다.
    /// </summary>
    /// <param name="volume">0부터 1 사이의 BGM 볼륨.</param>
    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BgmVolumePreferenceKey, bgmVolume);
        PlayerPrefs.Save();
        bgmSource.volume = bgmVolume;
    }

    /// <summary>
    /// 효과음 볼륨을 설정하고 브라우저 저장소에 보존한다.
    /// </summary>
    /// <param name="volume">0부터 1 사이의 효과음 볼륨.</param>
    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumePreferenceKey, sfxVolume);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    private void CreateAudioSources()
    {
        bgmSource = CreateAudioSource("BGM Source");
        bgmSource.loop = true;

        int sourceCount = Mathf.Max(3, sfxSourceCount);

        for (int index = 0; index < sourceCount; index++)
        {
            sfxSources.Add(CreateAudioSource($"SFX Source {index + 1}"));
        }
    }

    private AudioSource CreateAudioSource(string sourceName)
    {
        GameObject sourceObject = new(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.bypassReverbZones = true;
        return source;
    }

    private void LoadVolumes()
    {
        bgmVolume = PlayerPrefs.GetFloat(
            BgmVolumePreferenceKey,
            defaultBgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(
            SfxVolumePreferenceKey,
            defaultSfxVolume);
    }

    private void ApplyVolumes()
    {
        bgmSource.volume = bgmVolume;
        ApplySfxVolume();
    }

    private void ApplySfxVolume()
    {
        foreach (AudioSource source in sfxSources)
        {
            source.volume = sfxVolume;
        }
    }

    private bool TryGetClip(string key, out AudioClip clip)
    {
        clip = null;

        if (soundLibrary == null)
        {
            DebugConsole.LogWarning(
                "SoundManager has no SoundLibrary assigned.",
                this);
            return false;
        }

        if (!soundLibrary.TryGetClip(key, out clip))
        {
            DebugConsole.LogWarningFormat(
                "Sound key was not found: {0}",
                key);
            return false;
        }

        return true;
    }
}
