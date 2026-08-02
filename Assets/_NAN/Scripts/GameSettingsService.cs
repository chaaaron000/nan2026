using System;
using Cysharp.Threading.Tasks;
using Nan.UI;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 게임 설정 기능의 전역 진입점과 설정 팝업의 생명주기를 관리합니다.
/// </summary>
public sealed class GameSettingsService : LazyPersistentSingleton<GameSettingsService>
{
    private const string SettingsUIResourcePath = "GameSettingsUI";
    private const string MasterVolumePreferenceKey = "sound.master_volume";
    private const string BgmVolumePreferenceKey = "sound.bgm_volume";
    private const string SfxVolumePreferenceKey = "sound.sfx_volume";
    private const string ColorVisionCorrectionPreferenceKey = "accessibility.color_vision_correction";
    private const string SymbolsEnabledPreferenceKey = "accessibility.symbols_enabled";

    private UIGameSettings settingsUI;
    private bool hasAudioVolumeSnapshot;
    private float savedMasterVolume;
    private float savedBgmVolume;
    private float savedSfxVolume;
    private bool hasColorVisionCorrectionSnapshot;
    private ColorVisionCorrection savedColorVisionCorrection;
    private bool hasSymbolsEnabledSnapshot;
    private bool savedSymbolsEnabled;
    private bool isSettingsOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        _ = Instance;
    }

    private void Start()
    {
        InitializeSoundVolumes();
        InitializeColorVisionCorrection();
        InitializeSymbolsEnabled();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (isSettingsOpen)
        {
            HandleCancelRequested();
            return;
        }

        ShowSettings();
    }

    /// <summary>
    /// 게임 설정 팝업을 생성하고 표시합니다.
    /// </summary>
    public void ShowSettings()
    {
        if (isSettingsOpen)
        {
            return;
        }

        if (!TryCreateSettingsUI())
        {
            return;
        }

        CaptureAudioVolumeSnapshot();
        CaptureColorVisionCorrectionSnapshot();
        CaptureSymbolsEnabledSnapshot();
        settingsUI.SetAudioVolumes(savedMasterVolume, savedBgmVolume, savedSfxVolume);
        settingsUI.SetAccessibilityDisplaySettings(AccessibilityDisplaySettings.Instance);
        settingsUI.SetColorVisionCorrection(AccessibilityDisplaySettings.Instance.ColorVisionCorrection);
        settingsUI.SetSymbolsEnabled(AccessibilityDisplaySettings.Instance.SymbolsEnabled);
        isSettingsOpen = true;
        settingsUI.ShowAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 생성된 게임 설정 팝업을 숨깁니다.
    /// </summary>
    public void HideSettings()
    {
        if (settingsUI == null || !isSettingsOpen)
        {
            return;
        }

        isSettingsOpen = false;
        settingsUI.HideAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 저장된 Master 볼륨을 반환하며, 설정이 없으면 지정한 기본값을 반환합니다.
    /// </summary>
    /// <param name="defaultVolume">저장된 설정이 없을 때 사용할 Master 기본 볼륨.</param>
    /// <returns>0부터 1 사이로 제한된 Master 볼륨.</returns>
    public float GetMasterVolume(float defaultVolume)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePreferenceKey, Mathf.Clamp01(defaultVolume)));
    }

    /// <summary>
    /// Master 볼륨을 저장하고 현재 사운드 출력에 적용합니다.
    /// </summary>
    /// <param name="volume">0부터 1 사이의 Master 볼륨.</param>
    public void SetMasterVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumePreferenceKey, clampedVolume);
        PlayerPrefs.Save();
        SoundManager.Instance.SetMasterVolume(clampedVolume);
    }

    /// <summary>
    /// 저장된 BGM 볼륨을 반환하며, 설정이 없으면 지정한 기본값을 반환합니다.
    /// </summary>
    /// <param name="defaultVolume">저장된 설정이 없을 때 사용할 BGM 기본 볼륨.</param>
    /// <returns>0부터 1 사이로 제한된 BGM 볼륨.</returns>
    public float GetBgmVolume(float defaultVolume)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumePreferenceKey, Mathf.Clamp01(defaultVolume)));
    }

    /// <summary>
    /// BGM 볼륨을 저장하고 현재 사운드 출력에 적용합니다.
    /// </summary>
    /// <param name="volume">0부터 1 사이의 BGM 볼륨.</param>
    public void SetBgmVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BgmVolumePreferenceKey, clampedVolume);
        PlayerPrefs.Save();
        SoundManager.Instance.SetBgmVolume(clampedVolume);
    }

    /// <summary>
    /// 저장된 효과음 볼륨을 반환하며, 설정이 없으면 지정한 기본값을 반환합니다.
    /// </summary>
    /// <param name="defaultVolume">저장된 설정이 없을 때 사용할 효과음 기본 볼륨.</param>
    /// <returns>0부터 1 사이로 제한된 효과음 볼륨.</returns>
    public float GetSfxVolume(float defaultVolume)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumePreferenceKey, Mathf.Clamp01(defaultVolume)));
    }

    /// <summary>
    /// 효과음 볼륨을 저장하고 현재 사운드 출력에 적용합니다.
    /// </summary>
    /// <param name="volume">0부터 1 사이의 효과음 볼륨.</param>
    public void SetSfxVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumePreferenceKey, clampedVolume);
        PlayerPrefs.Save();
        SoundManager.Instance.SetSfxVolume(clampedVolume);
    }

    private void InitializeSoundVolumes()
    {
        SoundManager soundManager = SoundManager.Instance;

        // SoundLibrary의 기본값은 설정이 아직 저장되지 않은 첫 실행에서만 사용한다.
        soundManager.SetMasterVolume(GetMasterVolume(soundManager.DefaultMasterVolume));
        soundManager.SetBgmVolume(GetBgmVolume(soundManager.DefaultBgmVolume));
        soundManager.SetSfxVolume(GetSfxVolume(soundManager.DefaultSfxVolume));
    }

    private void InitializeColorVisionCorrection()
    {
        int storedValue = PlayerPrefs.GetInt(ColorVisionCorrectionPreferenceKey, (int)ColorVisionCorrection.None);
        ColorVisionCorrection correction = Enum.IsDefined(typeof(ColorVisionCorrection), storedValue)
            ? (ColorVisionCorrection)storedValue
            : ColorVisionCorrection.None;

        AccessibilityDisplaySettings.Instance.SetColorVisionCorrection(correction);
    }

    private void InitializeSymbolsEnabled()
    {
        bool defaultValue = AccessibilityDisplaySettings.Instance.SymbolsEnabled;
        bool symbolsEnabled = PlayerPrefs.GetInt(SymbolsEnabledPreferenceKey, defaultValue ? 1 : 0) != 0;

        AccessibilityDisplaySettings.Instance.SetSymbolsEnabled(symbolsEnabled);
    }

    private bool TryCreateSettingsUI()
    {
        if (settingsUI != null)
        {
            return true;
        }

        UIGameSettings settingsPrefab = Resources.Load<UIGameSettings>(SettingsUIResourcePath);
        if (settingsPrefab == null)
        {
            DebugConsole.LogError(
                $"[GameSettingsService] Settings UI was not found in Resources: {SettingsUIResourcePath}",
                this
            );
            return false;
        }

        settingsUI = Instantiate(settingsPrefab, transform);
        settingsUI.name = settingsPrefab.name;
        settingsUI.ConfirmRequested += HandleConfirmRequested;
        settingsUI.CancelRequested += HandleCancelRequested;
        settingsUI.MasterVolumeChanged += HandleMasterVolumeChanged;
        settingsUI.BgmVolumeChanged += HandleBgmVolumeChanged;
        settingsUI.SfxVolumeChanged += HandleSfxVolumeChanged;
        settingsUI.ColorVisionCorrectionChanged += HandleColorVisionCorrectionChanged;
        settingsUI.SymbolsEnabledChanged += HandleSymbolsEnabledChanged;
        settingsUI.InitializeHidden();
        return true;
    }

    private void HandleConfirmRequested()
    {
        SaveAudioVolumePreferences();
        SaveColorVisionCorrectionPreference();
        SaveSymbolsEnabledPreference();
        hasAudioVolumeSnapshot = false;
        hasColorVisionCorrectionSnapshot = false;
        hasSymbolsEnabledSnapshot = false;
        HideSettings();
    }

    private void HandleCancelRequested()
    {
        RestoreAudioVolumeSnapshot();
        RestoreColorVisionCorrectionSnapshot();
        RestoreSymbolsEnabledSnapshot();
        hasAudioVolumeSnapshot = false;
        hasColorVisionCorrectionSnapshot = false;
        hasSymbolsEnabledSnapshot = false;
        HideSettings();
    }

    private void HandleMasterVolumeChanged(float volume)
    {
        SoundManager.Instance.SetMasterVolume(volume);
    }

    private void HandleBgmVolumeChanged(float volume)
    {
        SoundManager.Instance.SetBgmVolume(volume);
    }

    private void HandleSfxVolumeChanged(float volume)
    {
        SoundManager.Instance.SetSfxVolume(volume);
    }

    private void HandleColorVisionCorrectionChanged(ColorVisionCorrection correction)
    {
        AccessibilityDisplaySettings.Instance.SetColorVisionCorrection(correction);
    }

    private void HandleSymbolsEnabledChanged(bool enabled)
    {
        AccessibilityDisplaySettings.Instance.SetSymbolsEnabled(enabled);
    }

    private void CaptureAudioVolumeSnapshot()
    {
        if (hasAudioVolumeSnapshot)
        {
            return;
        }

        SoundManager soundManager = SoundManager.Instance;
        savedMasterVolume = soundManager.MasterVolume;
        savedBgmVolume = soundManager.BgmVolume;
        savedSfxVolume = soundManager.SfxVolume;
        hasAudioVolumeSnapshot = true;
    }

    private void RestoreAudioVolumeSnapshot()
    {
        if (!hasAudioVolumeSnapshot)
        {
            return;
        }

        SoundManager soundManager = SoundManager.Instance;
        soundManager.SetMasterVolume(savedMasterVolume);
        soundManager.SetBgmVolume(savedBgmVolume);
        soundManager.SetSfxVolume(savedSfxVolume);
        settingsUI.SetAudioVolumes(savedMasterVolume, savedBgmVolume, savedSfxVolume);
    }

    private void CaptureColorVisionCorrectionSnapshot()
    {
        if (hasColorVisionCorrectionSnapshot)
        {
            return;
        }

        savedColorVisionCorrection = AccessibilityDisplaySettings.Instance.ColorVisionCorrection;
        hasColorVisionCorrectionSnapshot = true;
    }

    private void RestoreColorVisionCorrectionSnapshot()
    {
        if (!hasColorVisionCorrectionSnapshot)
        {
            return;
        }

        AccessibilityDisplaySettings.Instance.SetColorVisionCorrection(savedColorVisionCorrection);
        settingsUI.SetColorVisionCorrection(savedColorVisionCorrection);
    }

    private void CaptureSymbolsEnabledSnapshot()
    {
        if (hasSymbolsEnabledSnapshot)
        {
            return;
        }

        savedSymbolsEnabled = AccessibilityDisplaySettings.Instance.SymbolsEnabled;
        hasSymbolsEnabledSnapshot = true;
    }

    private void RestoreSymbolsEnabledSnapshot()
    {
        if (!hasSymbolsEnabledSnapshot)
        {
            return;
        }

        AccessibilityDisplaySettings.Instance.SetSymbolsEnabled(savedSymbolsEnabled);
        settingsUI.SetSymbolsEnabled(savedSymbolsEnabled);
    }

    private void SaveAudioVolumePreferences()
    {
        SoundManager soundManager = SoundManager.Instance;
        PlayerPrefs.SetFloat(MasterVolumePreferenceKey, soundManager.MasterVolume);
        PlayerPrefs.SetFloat(BgmVolumePreferenceKey, soundManager.BgmVolume);
        PlayerPrefs.SetFloat(SfxVolumePreferenceKey, soundManager.SfxVolume);
        PlayerPrefs.Save();
    }

    private void SaveColorVisionCorrectionPreference()
    {
        PlayerPrefs.SetInt(
            ColorVisionCorrectionPreferenceKey,
            (int)AccessibilityDisplaySettings.Instance.ColorVisionCorrection
        );
        PlayerPrefs.Save();
    }

    private void SaveSymbolsEnabledPreference()
    {
        PlayerPrefs.SetInt(SymbolsEnabledPreferenceKey, AccessibilityDisplaySettings.Instance.SymbolsEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    protected override void OnDestroy()
    {
        if (settingsUI != null)
        {
            settingsUI.ConfirmRequested -= HandleConfirmRequested;
            settingsUI.CancelRequested -= HandleCancelRequested;
            settingsUI.MasterVolumeChanged -= HandleMasterVolumeChanged;
            settingsUI.BgmVolumeChanged -= HandleBgmVolumeChanged;
            settingsUI.SfxVolumeChanged -= HandleSfxVolumeChanged;
            settingsUI.ColorVisionCorrectionChanged -= HandleColorVisionCorrectionChanged;
            settingsUI.SymbolsEnabledChanged -= HandleSymbolsEnabledChanged;
        }

        base.OnDestroy();
    }
}