using System;
using UnityEngine;

/// <summary>
/// 물감 상태를 화면에 표시할 색상표를 보관한다.
/// 게임 규칙의 PaintState와 표시 색상을 분리하여 색각 보정 팔레트를 교체할 수 있게 한다.
/// </summary>
[CreateAssetMenu(
    fileName = "ColorPalette",
    menuName = "NaN/Color Palette")]
public sealed class ColorPaletteSO : ScriptableObject
{
    [SerializeField]
    private Color emptyColor = new(0.25f, 0.25f, 0.25f, 0f);

    [SerializeField]
    private Color redColor = Color.red;

    [SerializeField]
    private Color greenColor = Color.green;

    [SerializeField]
    private Color blueColor = Color.blue;

    [SerializeField]
    private Color yellowColor = Color.yellow;

    [SerializeField]
    private Color cyanColor = Color.cyan;

    [SerializeField]
    private Color magentaColor = Color.magenta;

    [SerializeField]
    private Color whiteColor = Color.white;

    [Header("Paint Visual Set")]
    [SerializeField]
    private PaintVisualSet redVisualSet;

    [SerializeField]
    private PaintVisualSet greenVisualSet;

    [SerializeField]
    private PaintVisualSet blueVisualSet;

    [SerializeField]
    private PaintVisualSet yellowVisualSet;

    [SerializeField]
    private PaintVisualSet cyanVisualSet;

    [SerializeField]
    private PaintVisualSet magentaVisualSet;

    [SerializeField]
    private PaintVisualSet whiteVisualSet;

    /// <summary>
    /// 지정한 PaintState에 대응하는 시각 리소스 묶음을 반환한다.
    /// </summary>
    /// <param name="paintState">조회할 페인트 상태.</param>
    /// <returns>상태에 대응하는 VisualSet. Empty는 null이다.</returns>
    public PaintVisualSet GetVisualSet(PaintState paintState)
    {
        return paintState switch
        {
            PaintState.Red => redVisualSet,
            PaintState.Green => greenVisualSet,
            PaintState.Blue => blueVisualSet,
            PaintState.Yellow => yellowVisualSet,
            PaintState.Cyan => cyanVisualSet,
            PaintState.Magenta => magentaVisualSet,
            PaintState.White => whiteVisualSet,
            PaintState.Empty => null,
            _ => throw new ArgumentOutOfRangeException(
                nameof(paintState),
                paintState,
                "Unsupported paint state."),
        };
    }

    /// <summary>
    /// 지정한 셀 물감 상태에 대응하는 표시 색상을 반환한다.
    /// </summary>
    /// <param name="paintState">표시할 셀의 물감 조합 상태.</param>
    /// <returns>팔레트에 정의된 표시 색상.</returns>
    public Color GetColor(PaintState paintState)
    {
        return paintState switch
        {
            PaintState.Empty => emptyColor,
            PaintState.Red => redColor,
            PaintState.Green => greenColor,
            PaintState.Blue => blueColor,
            PaintState.Yellow => yellowColor,
            PaintState.Cyan => cyanColor,
            PaintState.Magenta => magentaColor,
            PaintState.White => whiteColor,
            _ => throw new ArgumentOutOfRangeException(
                nameof(paintState),
                paintState,
                "Unsupported paint state."),
        };
    }

    /// <summary>
    /// 지정한 물감통 종류에 대응하는 기본 물감 색상을 반환한다.
    /// Clear 물감통은 빈 셀 색상을 반환한다.
    /// </summary>
    /// <param name="paintType">표시할 물감통의 종류.</param>
    /// <returns>팔레트에 정의된 기본 물감 색상.</returns>
    public Color GetColor(PaintType paintType)
    {
        return paintType switch
        {
            PaintType.Red => redColor,
            PaintType.Green => greenColor,
            PaintType.Blue => blueColor,
            PaintType.Clear => emptyColor,
            _ => throw new ArgumentOutOfRangeException(
                nameof(paintType),
                paintType,
                "Unsupported paint type."),
        };
    }

    /// <summary>
    /// 지정한 셀 물감 상태의 배경색 위에서 가장 읽기 쉬운 심볼 색상을 반환한다.
    /// </summary>
    /// <param name="paintState">심볼을 표시할 셀의 물감 상태.</param>
    /// <returns>검정 또는 흰색의 심볼 색상.</returns>
    public Color GetSymbolColor(PaintState paintState)
    {
        return GetReadableTextColor(GetColor(paintState));
    }

    /// <summary>
    /// 지정한 물감통 종류의 배경색 위에서 가장 읽기 쉬운 심볼 색상을 반환한다.
    /// </summary>
    /// <param name="paintType">심볼을 표시할 물감통의 종류.</param>
    /// <returns>검정 또는 흰색의 심볼 색상.</returns>
    public Color GetSymbolColor(PaintType paintType)
    {
        return GetReadableTextColor(GetColor(paintType));
    }

    private static Color GetReadableTextColor(Color backgroundColor)
    {
        float luminance = 0.2126f * ToLinear(backgroundColor.r)
                          + 0.7152f * ToLinear(backgroundColor.g)
                          + 0.0722f * ToLinear(backgroundColor.b);
        float blackContrast = (luminance + 0.05f) / 0.05f;
        float whiteContrast = 1.05f / (luminance + 0.05f);

        return blackContrast >= whiteContrast
            ? Color.black
            : Color.white;
    }

    private static float ToLinear(float channel)
    {
        return channel <= 0.04045f
            ? channel / 12.92f
            : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
    }
}
