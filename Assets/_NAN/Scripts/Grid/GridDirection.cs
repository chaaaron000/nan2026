using System;
using UnityEngine;

/// <summary>
/// 격자에서 인접한 셀로 이동할 때 사용하는 방향.
/// </summary>
public enum GridDirection
{
    /// <summary>위쪽 인접 셀 방향.</summary>
    UP,

    /// <summary>오른쪽 인접 셀 방향.</summary>
    RIGHT,

    /// <summary>아래쪽 인접 셀 방향.</summary>
    DOWN,

    /// <summary>왼쪽 인접 셀 방향.</summary>
    LEFT,
}

/// <summary>
/// 격자 방향을 좌표 계산에 사용할 수 있도록 변환한다.
/// </summary>
public static class GridDirectionUtility
{
    /// <summary>
    /// 격자 방향을 한 칸 크기의 좌표 오프셋으로 변환한다.
    /// </summary>
    public static Vector2Int ToOffset(this GridDirection direction)
    {
        return direction switch
        {
            GridDirection.UP => Vector2Int.up,
            GridDirection.RIGHT => Vector2Int.right,
            GridDirection.DOWN => Vector2Int.down,
            GridDirection.LEFT => Vector2Int.left,

            _ => throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Unsupported grid direction."
            ),
        };
    }
}
