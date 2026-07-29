using System;

/// <summary>
/// 물감 상태를 화면 표시에 필요한 공통 시각 정보로 변환한다.
/// </summary>
public static class PaintStateVisualUtility
{
    /// <summary>
    /// 지정한 물감 상태를 나타내는 RGB 조합 심볼을 반환한다.
    /// </summary>
    /// <param name="paintState">심볼로 변환할 물감 상태.</param>
    /// <returns>빈 상태는 빈 문자열, 나머지는 RGB 조합 문자열.</returns>
    public static string GetSymbol(PaintState paintState)
    {
        return paintState switch
        {
            PaintState.Red => "R",
            PaintState.Green => "G",
            PaintState.Blue => "B",
            PaintState.Yellow => "RG",
            PaintState.Cyan => "GB",
            PaintState.Magenta => "RB",
            PaintState.White => "RGB",
            PaintState.Empty => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(paintState), paintState, "Unsupported paint state."),
        };
    }
}