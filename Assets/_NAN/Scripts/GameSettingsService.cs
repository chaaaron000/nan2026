using Cysharp.Threading.Tasks;
using Nan.UI;
using UnityEngine;

/// <summary>
/// 게임 설정 기능의 전역 진입점과 설정 팝업의 생명주기를 관리합니다.
/// </summary>
public sealed class GameSettingsService : LazyPersistentSingleton<GameSettingsService>
{
    private const string SettingsUIResourcePath = "GameSettingsUI";

    private UIGameSettings settingsUI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        _ = Instance;
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
