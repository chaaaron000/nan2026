using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 생성된 물감통 한 개의 시각적 표현과 클릭 입력을 담당한다.
/// </summary>
public sealed class PaintBucketView : MonoBehaviour
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private Image bucketImage;

    [SerializeField]
    private TMP_Text rangeText;

    [SerializeField]
    private TMP_Text symbolText;

    private PaintType paintType;
    private AccessibilityDisplaySettings displaySettings;
    private bool isDisplaySettingsSubscribed;

    /// <summary>
    /// 플레이어가 이 물감통을 클릭했을 때 발생한다.
    /// </summary>
    public event Action<PaintBucketView> Clicked;

    private void Awake()
    {
        button.onClick.AddListener(HandleButtonClicked);
    }

    private void OnEnable()
    {
        SubscribeDisplaySettings();
        RefreshSymbol();
    }

    private void OnDisable()
    {
        UnsubscribeDisplaySettings();
    }

    /// <summary>
    /// 물감통 데이터와 Sprite를 바탕으로 최초 표시 상태를 설정한다.
    /// </summary>
    /// <param name="range">물감의 확산 범위.</param>
    /// <param name="bucketSprite">물감통에 표시할 스프라이트.</param>
    /// <param name="newPaintType">물감통이 사용하는 물감 종류.</param>
    public void Initialize(int range, Sprite bucketSprite, PaintType newPaintType)
    {
        if (bucketSprite == null)
        {
            throw new ArgumentNullException(nameof(bucketSprite));
        }

        bucketImage.sprite = bucketSprite;
        rangeText.text = range.ToString();
        paintType = newPaintType;
        RefreshSymbol();

        SetSelected(false);
        SetConsumed(false);
    }

    /// <summary>
    /// 물감통이 사용할 접근성 표시 설정을 지정하고 현재 심볼 표시를 갱신한다.
    /// </summary>
    /// <param name="settings">물감통 표시에 사용할 접근성 표시 설정.</param>
    public void SetAccessibilityDisplaySettings(AccessibilityDisplaySettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (displaySettings == settings)
        {
            RefreshSymbol();
            return;
        }

        UnsubscribeDisplaySettings();
        displaySettings = settings;
        SubscribeDisplaySettings();
        RefreshSymbol();
    }

    /// <summary>
    /// 물감통 선택 상태를 화면에 반영한다.
    /// </summary>
    /// <param name="selected">true면 선택 상태를 표시한다.</param>
    public void SetSelected(bool selected)
    {
    }

    /// <summary>
    /// 물감통의 소모 상태를 화면에 반영한다.
    /// </summary>
    /// <param name="consumed">true면 물감통을 숨기고 선택할 수 없게 한다.</param>
    public void SetConsumed(bool consumed)
    {
        button.interactable = !consumed;

        if (consumed)
        {
            SetSelected(false);
        }

        gameObject.SetActive(!consumed);
    }

    private void HandleButtonClicked()
    {
        Clicked?.Invoke(this);
    }

    private void RefreshSymbol()
    {
        if (symbolText == null || displaySettings == null)
        {
            return;
        }

        bool shouldShowSymbol = displaySettings.SymbolsEnabled;
        symbolText.gameObject.SetActive(shouldShowSymbol);

        if (!shouldShowSymbol || displaySettings.ActivePalette == null)
        {
            return;
        }

        symbolText.text = paintType == PaintType.Clear
            ? "X"
            : paintType.ToString()[0].ToString();

        // Clear 물감통은 현재 흰색 아이콘을 사용하므로, 팔레트의 빈 셀 색 대신 검정 심볼을 사용한다.
        symbolText.color = paintType == PaintType.Clear
            ? Color.black
            : displaySettings.ActivePalette.GetSymbolColor(paintType);
    }

    private void SubscribeDisplaySettings()
    {
        if (displaySettings == null || isDisplaySettingsSubscribed)
        {
            return;
        }

        displaySettings.PaletteChanged += HandlePaletteChanged;
        displaySettings.SymbolsEnabledChanged += HandleSymbolsEnabledChanged;
        isDisplaySettingsSubscribed = true;
    }

    private void UnsubscribeDisplaySettings()
    {
        if (displaySettings == null || !isDisplaySettingsSubscribed)
        {
            return;
        }

        displaySettings.PaletteChanged -= HandlePaletteChanged;
        displaySettings.SymbolsEnabledChanged -= HandleSymbolsEnabledChanged;
        isDisplaySettingsSubscribed = false;
    }

    private void HandlePaletteChanged(ColorPaletteSO palette)
    {
        RefreshSymbol();
    }

    private void HandleSymbolsEnabledChanged(bool enabled)
    {
        RefreshSymbol();
    }

    private void OnDestroy()
    {
        UnsubscribeDisplaySettings();

        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }
}
