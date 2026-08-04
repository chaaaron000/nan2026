using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM과 효과음의 재생, 동시 재생, 개별 볼륨을 전역에서 관리한다.
/// </summary>
public sealed class SoundManager : LazyPersistentSingleton<SoundManager>
{
    private const string SoundLibraryResourcePath = "SoundLibrary";
    private const string BgmMixerGroupPath = "Master/BGM";
    private const string SfxMixerGroupPath = "Master/SFX";
    private const string MasterVolumeParameterName = "MasterVolume";
    private const string BgmVolumeParameterName = "BgmVolume";
    private const string SfxVolumeParameterName = "SfxVolume";
    private const float MinimumMixerVolumeDecibels = -80f;
    private const float MinimumLinearVolume = 0.0001f;

    [SerializeField]
    private SoundLibrary soundLibrary;

    private AudioMixer audioMixer;
    private AudioMixerGroup bgmMixerGroup;
    private AudioMixerGroup sfxMixerGroup;
    private AudioSource bgmSource;
    private readonly List<AudioSource> sfxSources = new();
    private int nextSfxSourceIndex;
    private AudioClip currentBgmClip;
    private float masterVolume;
    private float bgmVolume;
    private float sfxVolume;

    /// <summary>
    /// 현재 적용된 Master 볼륨을 반환한다.
    /// </summary>
    public float MasterVolume => masterVolume;

    /// <summary>
    /// 현재 적용된 BGM 볼륨을 반환한다.
    /// </summary>
    public float BgmVolume => bgmVolume;

    /// <summary>
    /// 현재 적용된 효과음 볼륨을 반환한다.
    /// </summary>
    public float SfxVolume => sfxVolume;

    /// <summary>
    /// 저장된 사용자 설정이 없을 때 적용할 Master 기본 볼륨을 반환한다.
    /// </summary>
    public float DefaultMasterVolume => soundLibrary != null ? Mathf.Clamp01(soundLibrary.DefaultMasterVolume) : 1f;

    /// <summary>
    /// 저장된 사용자 설정이 없을 때 적용할 BGM 기본 볼륨을 반환한다.
    /// </summary>
    public float DefaultBgmVolume => soundLibrary != null ? soundLibrary.DefaultBgmVolume : 1f;

    /// <summary>
    /// 저장된 사용자 설정이 없을 때 적용할 효과음 기본 볼륨을 반환한다.
    /// </summary>
    public float DefaultSfxVolume => soundLibrary != null ? soundLibrary.DefaultSfxVolume : 1f;

    protected override void Awake()
    {
        base.Awake();

        // 부모 싱글톤이 중복 인스턴스를 파괴한 경우에는 오디오 소스를 만들지 않는다.
        if (Instance != this)
        {
            return;
        }

        if (!TryLoadSoundLibrary())
        {
            return;
        }

        if (!TryConfigureMixer())
        {
            return;
        }

        CreateAudioSources();
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
        if (!TryGetClip(key, out AudioClip clip) || sfxSources.Count == 0)
        {
            return;
        }

        AudioSource source = sfxSources[nextSfxSourceIndex];
        nextSfxSourceIndex = (nextSfxSourceIndex + 1) % sfxSources.Count;

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
    /// Master 볼륨을 즉시 적용한다.
    /// </summary>
    /// <param name="volume">0부터 1 사이의 Master 볼륨.</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SetMixerVolume(MasterVolumeParameterName, masterVolume);
    }

    /// <summary>
    /// BGM 볼륨을 즉시 적용한다.
    /// </summary>
    /// <param name="volume">0부터 1 사이의 BGM 볼륨.</param>
    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        SetMixerVolume(BgmVolumeParameterName, bgmVolume);
    }

    /// <summary>
    /// 효과음 볼륨을 즉시 적용한다.
    /// </summary>
    /// <param name="volume">0부터 1 사이의 효과음 볼륨.</param>
    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SetMixerVolume(SfxVolumeParameterName, sfxVolume);
    }

    private void CreateAudioSources()
    {
        bgmSource = CreateAudioSource("BGM Source", bgmMixerGroup);
        bgmSource.loop = true;

        int sourceCount = soundLibrary.SfxSourceCount;

        for (int index = 0; index < sourceCount; index++)
        {
            sfxSources.Add(CreateAudioSource($"SFX Source {index + 1}", sfxMixerGroup));
        }
    }

    private AudioSource CreateAudioSource(string sourceName, AudioMixerGroup mixerGroup)
    {
        GameObject sourceObject = new(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.volume = 1f;
        source.outputAudioMixerGroup = mixerGroup;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.bypassReverbZones = true;
        return source;
    }

    private bool TryConfigureMixer()
    {
        audioMixer = soundLibrary.AudioMixer;
        if (audioMixer == null)
        {
            DebugConsole.LogError("SoundLibrary has no AudioMixer assigned.", this);
            return false;
        }

        return TryFindMixerGroup(BgmMixerGroupPath, out bgmMixerGroup)
               && TryFindMixerGroup(SfxMixerGroupPath, out sfxMixerGroup);
    }

    private bool TryFindMixerGroup(string groupPath, out AudioMixerGroup mixerGroup)
    {
        AudioMixerGroup[] groups = audioMixer.FindMatchingGroups(groupPath);
        if (groups.Length == 1)
        {
            mixerGroup = groups[0];
            return true;
        }

        mixerGroup = null;
        DebugConsole.LogErrorFormat("AudioMixer group was not found or is ambiguous: {0}", groupPath);
        return false;
    }

    private void SetMixerVolume(string parameterName, float linearVolume)
    {
        if (audioMixer == null)
        {
            return;
        }

        if (audioMixer.SetFloat(parameterName, ConvertLinearVolumeToDecibels(linearVolume)))
        {
            return;
        }

        DebugConsole.LogErrorFormat("AudioMixer exposed parameter was not found: {0}", parameterName);
    }

    private static float ConvertLinearVolumeToDecibels(float linearVolume)
    {
        // 선형 슬라이더 값을 로그 스케일의 dB로 변환해야 체감 음량 변화가 자연스럽다.
        return Mathf.Max(MinimumMixerVolumeDecibels, Mathf.Log10(Mathf.Max(linearVolume, MinimumLinearVolume)) * 20f);
    }

    private bool TryLoadSoundLibrary()
    {
        if (soundLibrary != null)
        {
            return true;
        }

        soundLibrary = Resources.Load<SoundLibrary>(SoundLibraryResourcePath);
        if (soundLibrary != null)
        {
            return true;
        }

        DebugConsole.LogError($"SoundLibrary was not found in Resources: {SoundLibraryResourcePath}", this);
        return false;
    }

    private bool TryGetClip(string key, out AudioClip clip)
    {
        clip = null;

        if (soundLibrary == null)
        {
            DebugConsole.LogWarning("SoundManager has no SoundLibrary assigned.", this);
            return false;
        }

        if (!soundLibrary.TryGetClip(key, out clip))
        {
            DebugConsole.LogWarningFormat("Sound key was not found: {0}", key);
            return false;
        }

        return true;
    }
}