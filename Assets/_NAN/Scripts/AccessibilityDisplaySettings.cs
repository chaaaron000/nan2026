using System;
using UnityEngine;

/// <summary>
/// 씬 전환 후에도 유지되는 색각 보정 및 심볼 표시 설정을 제공한다.
/// 설정 화면은 이 컴포넌트를 통해 모든 View의 표시 방식을 즉시 갱신한다.
/// </summary>
public sealed class AccessibilityDisplaySettings : MonoBehaviour
{
    private const string CatalogResourcePath = "AccessibilityDisplaySettingsCatalog";

    private static AccessibilityDisplaySettings instance;
    private static bool isCreatingInstance;

    [SerializeField]
    private ColorPaletteSO activePalette;

    [SerializeField]
    private bool symbolsEnabled;

    private AccessibilityDisplaySettingsCatalogSO catalog;
    private ColorVisionCorrection colorVisionCorrection;
    private bool isInitialized;

    /// <summary>
    /// 모든 씬이 공유하는 접근성 표시 설정 인스턴스.
    /// </summary>
    public static AccessibilityDisplaySettings Instance
    {
        get
        {
            if (instance == null)
            {
                isCreatingInstance = true;
                try
                {
                    GameObject settingsObject = new(nameof(AccessibilityDisplaySettings));
                    instance = settingsObject.AddComponent<AccessibilityDisplaySettings>();
                }
                finally
                {
                    isCreatingInstance = false;
                }
            }

            instance.Initialize();
            return instance;
        }
    }

    /// <summary>
    /// 현재 화면 표시에 사용 중인 색상 팔레트.
    /// </summary>
    public ColorPaletteSO ActivePalette => Instance.activePalette;

    /// <summary>
    /// 현재 적용 중인 색각 보정 종류.
    /// </summary>
    public ColorVisionCorrection ColorVisionCorrection => Instance.colorVisionCorrection;

    /// <summary>
    /// 물감 상태 심볼을 화면에 표시할지 여부.
    /// </summary>
    public bool SymbolsEnabled => Instance.symbolsEnabled;

    /// <summary>
    /// 활성 팔레트가 교체된 뒤 새 팔레트를 전달한다.
    /// </summary>
    public event Action<ColorPaletteSO> PaletteChanged;

    /// <summary>
    /// 심볼 표시 설정이 변경된 뒤 새 상태를 전달한다.
    /// </summary>
    public event Action<bool> SymbolsEnabledChanged;

    private void Awake()
    {
        if (!isCreatingInstance)
        {
            return;
        }

        instance = this;
        MakePersistent();
        Initialize();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// 활성 팔레트를 교체하고 모든 구독자에게 갱신을 요청한다.
    /// </summary>
    /// <param name="palette">새로 적용할 색상 팔레트.</param>
    public void SetPalette(ColorPaletteSO palette)
    {
        if (Instance != this)
        {
            Instance.SetPalette(palette);
            return;
        }

        if (palette == null)
        {
            throw new ArgumentNullException(nameof(palette));
        }

        if (activePalette == palette)
        {
            return;
        }

        activePalette = palette;
        colorVisionCorrection = catalog.GetCorrection(palette);
        PaletteChanged?.Invoke(activePalette);
    }

    /// <summary>
    /// 색각 보정 종류를 바꾸고 해당 팔레트를 즉시 적용한다.
    /// </summary>
    /// <param name="correction">적용할 색각 보정 종류.</param>
    public void SetColorVisionCorrection(ColorVisionCorrection correction)
    {
        if (Instance != this)
        {
            Instance.SetColorVisionCorrection(correction);
            return;
        }

        ColorPaletteSO palette = catalog.GetPalette(correction);
        if (palette == null)
        {
            throw new InvalidOperationException($"Color palette is missing for {correction} correction.");
        }

        if (colorVisionCorrection == correction && activePalette == palette)
        {
            return;
        }

        colorVisionCorrection = correction;
        activePalette = palette;
        PaletteChanged?.Invoke(activePalette);
    }

    /// <summary>
    /// 물감 상태 심볼의 표시 여부를 변경하고 모든 구독자에게 갱신을 요청한다.
    /// </summary>
    /// <param name="enabled">true면 심볼을 표시한다.</param>
    public void SetSymbolsEnabled(bool enabled)
    {
        if (Instance != this)
        {
            Instance.SetSymbolsEnabled(enabled);
            return;
        }

        if (symbolsEnabled == enabled)
        {
            return;
        }

        symbolsEnabled = enabled;
        SymbolsEnabledChanged?.Invoke(symbolsEnabled);
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        catalog = Resources.Load<AccessibilityDisplaySettingsCatalogSO>(CatalogResourcePath);
        if (catalog == null)
        {
            throw new InvalidOperationException(
                $"Accessibility display settings catalog was not found in Resources: {CatalogResourcePath}"
            );
        }

        colorVisionCorrection = ColorVisionCorrection.None;
        activePalette = catalog.GetPalette(colorVisionCorrection);
        if (activePalette == null)
        {
            throw new InvalidOperationException(
                "Default color palette is missing from the accessibility display settings catalog."
            );
        }

        isInitialized = true;
    }

    private void MakePersistent()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }
}

/// <summary>
/// 적용할 색각 보정 팔레트의 종류를 나타낸다.
/// </summary>
public enum ColorVisionCorrection
{
    None = 0,
    RedGreen = 1,
    BlueYellow = 2,
}