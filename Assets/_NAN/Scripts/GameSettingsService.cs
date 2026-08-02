using Cysharp.Threading.Tasks;
using Nan.UI;
using UnityEngine;

/// <summary>
/// 게임 설정 기능의 전역 진입점과 설정 팝업의 생명주기를 관리합니다.
/// </summary>
public sealed class GameSettingsService : LazyPersistentSingleton<GameSettingsService>
{
    private const string SettingsUIResourcePath = "GameSettingsUI";
    private const string MasterVolumePreferenceKey = "sound.master_volume";
    private const string BgmVolumePreferenceKey = "sound.bgm_volume";
    private const string SfxVolumePreferenceKey = "sound.sfx_volume";

    private UIGameSettings settingsUI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        _ = Instance;
    }

    private void Start()
    {
        InitializeSoundVolumes();
    }

    /// <summary>
    /// 게임 설정 팝업을 생성하고 표시합니다.
    /// </summary>
    public void ShowSettings()
    {
        if (!TryCreateSettingsUI())
        {
            return;
        }

        settingsUI.ShowAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 생성된 게임 설정 팝업을 숨깁니다.
    /// </summary>
    public void HideSettings()
    {
        if (settingsUI == null)
        {
            return;
        }

        settingsUI.HideAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 저장된 Master 볼륨을 반환하며, 설정이 없으면 지정한 기본값을 반환합니다.
    /// </summary>
    /// <param name="defaultVolume">저장된 설정이 없을 때 사용할 Master 기본 볼륨.</param>
    /// <returns>0부터 1 사이로 제한된 Master 볼륨.</returns>
    public float GetMasterVolume(float defaultVolume)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(
            MasterVolumePreferenceKey,
            Mathf.Clamp01(defaultVolume)));
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
        soundManager.SetMasterVolume(
            GetMasterVolume(soundManager.DefaultMasterVolume));
        soundManager.SetBgmVolume(
            GetBgmVolume(soundManager.DefaultBgmVolume));
        soundManager.SetSfxVolume(
            GetSfxVolume(soundManager.DefaultSfxVolume));
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
        settingsUI.InitializeHidden();
        return true;
    }

    private void HandleConfirmRequested()
    {
        HideSettings();
    }

    private void HandleCancelRequested()
    {
        HideSettings();
    }

    protected override void OnDestroy()
    {
        if (settingsUI != null)
        {
            settingsUI.ConfirmRequested -= HandleConfirmRequested;
            settingsUI.CancelRequested -= HandleCancelRequested;
        }

        base.OnDestroy();
    }
}
