using System;
using UnityEngine;

/// <summary>
/// 색각 보정 종류별로 적용할 색상 팔레트를 제공한다.
/// </summary>
[CreateAssetMenu(
    fileName = "AccessibilityDisplaySettingsCatalog",
    menuName = "NaN/Accessibility Display Settings Catalog"
)]
public sealed class AccessibilityDisplaySettingsCatalogSO : ScriptableObject
{
    [SerializeField]
    private ColorPaletteSO defaultPalette;

    [SerializeField]
    private ColorPaletteSO redGreenCompensationPalette;

    [SerializeField]
    private ColorPaletteSO blueYellowCompensationPalette;

    /// <summary>
    /// 지정한 색각 보정 종류에 대응하는 색상 팔레트를 반환한다.
    /// </summary>
    /// <param name="correction">조회할 색각 보정 종류.</param>
    /// <returns>색각 보정에 대응하는 색상 팔레트.</returns>
    public ColorPaletteSO GetPalette(ColorVisionCorrection correction)
    {
        return correction switch
        {
            ColorVisionCorrection.None => defaultPalette,
            ColorVisionCorrection.RedGreen => redGreenCompensationPalette,
            ColorVisionCorrection.BlueYellow => blueYellowCompensationPalette,
            _ => throw new ArgumentOutOfRangeException(nameof(correction), correction, null),
        };
    }

    /// <summary>
    /// 팔레트에 대응하는 색각 보정 종류를 반환한다.
    /// </summary>
    /// <param name="palette">색각 보정 종류를 찾을 팔레트.</param>
    /// <returns>팔레트에 대응하는 색각 보정 종류.</returns>
    public ColorVisionCorrection GetCorrection(ColorPaletteSO palette)
    {
        if (palette == defaultPalette)
        {
            return ColorVisionCorrection.None;
        }

        if (palette == redGreenCompensationPalette)
        {
            return ColorVisionCorrection.RedGreen;
        }

        if (palette == blueYellowCompensationPalette)
        {
            return ColorVisionCorrection.BlueYellow;
        }

        throw new ArgumentOutOfRangeException(
            nameof(palette),
            palette,
            "Palette is not registered in the accessibility display settings catalog."
        );
    }
}