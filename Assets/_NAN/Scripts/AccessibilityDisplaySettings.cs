using System;
using UnityEngine;

/// <summary>
/// 색상 팔레트와 심볼 표시 여부를 포함한 접근성 표시 설정을 제공한다.
/// 설정 화면은 이 컴포넌트를 통해 모든 View의 표시 방식을 즉시 갱신한다.
/// </summary>
public sealed class AccessibilityDisplaySettings : MonoBehaviour
{
    [SerializeField]
    private ColorPaletteSO activePalette;

    [SerializeField]
    private bool symbolsEnabled;

    /// <summary>
    /// 현재 화면 표시에 사용 중인 색상 팔레트.
    /// </summary>
    public ColorPaletteSO ActivePalette => activePalette;

    /// <summary>
    /// 물감 상태 심볼을 화면에 표시할지 여부.
    /// </summary>
    public bool SymbolsEnabled => symbolsEnabled;

    /// <summary>
    /// 활성 팔레트가 교체된 뒤 새 팔레트를 전달한다.
    /// </summary>
    public event Action<ColorPaletteSO> PaletteChanged;

    /// <summary>
    /// 심볼 표시 설정이 변경된 뒤 새 상태를 전달한다.
    /// </summary>
    public event Action<bool> SymbolsEnabledChanged;

    /// <summary>
    /// 활성 팔레트를 교체하고 모든 구독자에게 갱신을 요청한다.
    /// </summary>
    /// <param name="palette">새로 적용할 색상 팔레트.</param>
    public void SetPalette(ColorPaletteSO palette)
    {
        if (palette == null)
        {
            throw new ArgumentNullException(nameof(palette));
        }

        if (activePalette == palette)
        {
            return;
        }

        activePalette = palette;
        PaletteChanged?.Invoke(activePalette);
    }

    /// <summary>
    /// 물감 상태 심볼의 표시 여부를 변경하고 모든 구독자에게 갱신을 요청한다.
    /// </summary>
    /// <param name="enabled">true면 심볼을 표시한다.</param>
    public void SetSymbolsEnabled(bool enabled)
    {
        if (symbolsEnabled == enabled)
        {
            return;
        }

        symbolsEnabled = enabled;
        SymbolsEnabledChanged?.Invoke(symbolsEnabled);
    }
}
