using UnityEngine;

/// <summary>
/// Sprite와 UGUI 격자가 공유하는 셀 및 벽의 중앙 기준 위치를 계산한다.
/// </summary>
public static class GridLayoutUtility
{
    /// <summary>
    /// 지정한 셀의 격자 중심 기준 로컬 위치를 반환한다.
    /// </summary>
    /// <param name="gridPosition">위치를 계산할 셀 좌표.</param>
    /// <param name="gridWidth">격자의 가로 셀 수.</param>
    /// <param name="gridHeight">격자의 세로 셀 수.</param>
    /// <param name="cellSize">인접한 셀 중심 사이의 거리.</param>
    /// <returns>격자 중심을 원점으로 하는 셀의 로컬 위치.</returns>
    public static Vector2 GetCellLocalPosition(
        Vector2Int gridPosition,
        int gridWidth,
        int gridHeight,
        float cellSize)
    {
        float centerX = (gridWidth - 1) * 0.5f;
        float centerY = (gridHeight - 1) * 0.5f;

        return new Vector2(
            (gridPosition.x - centerX) * cellSize,
            (gridPosition.y - centerY) * cellSize);
    }

    /// <summary>
    /// 2배 좌표계 벽의 격자 중심 기준 로컬 위치를 반환한다.
    /// </summary>
    /// <param name="wallPosition">위치를 계산할 2배 좌표계 벽 좌표.</param>
    /// <param name="gridWidth">격자의 가로 셀 수.</param>
    /// <param name="gridHeight">격자의 세로 셀 수.</param>
    /// <param name="cellSize">인접한 셀 중심 사이의 거리.</param>
    /// <returns>격자 중심을 원점으로 하는 벽의 로컬 위치.</returns>
    public static Vector2 GetWallLocalPosition(
        Vector2Int wallPosition,
        int gridWidth,
        int gridHeight,
        float cellSize)
    {
        float centerX = (gridWidth - 1) * 0.5f;
        float centerY = (gridHeight - 1) * 0.5f;

        return new Vector2(
            (wallPosition.x * 0.5f - centerX) * cellSize,
            (wallPosition.y * 0.5f - centerY) * cellSize);
    }

    /// <summary>
    /// 2배 좌표계의 벽이 좌우 셀 사이를 가르는 세로 벽인지 반환한다.
    /// </summary>
    /// <param name="wallPosition">방향을 판정할 2배 좌표계 벽 좌표.</param>
    /// <returns>세로 벽이면 true, 가로 벽이면 false.</returns>
    public static bool IsVerticalWall(Vector2Int wallPosition)
    {
        return wallPosition.x % 2 != 0;
    }
}
