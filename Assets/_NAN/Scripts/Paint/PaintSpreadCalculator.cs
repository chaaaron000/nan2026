using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물감통이 도달할 셀 좌표를 계산한다.
/// 현재는 벽이 없는 격자의 맨해튼 거리를 사용한다.
/// </summary>
public sealed class PaintSpreadCalculator
{
    /// <summary>
    /// 시작 셀로부터 물감 범위 안에 있는 모든 좌표를 반환한다.
    /// 현재는 임시 구현으로, O(n^2)로 가능한 칸을 순회하지만,
    /// 후에는 bfs를 이용해 벽에 막히는 걸 고려해서 수정해야한다.
    /// </summary>
    public IReadOnlyList<Vector2Int> Calculate(
        GridState gridState,
        Vector2Int origin,
        int range)
    {
        if (gridState == null)
        {
            throw new ArgumentNullException(
                nameof(gridState));
        }

        if (!gridState.IsInside(origin))
        {
            throw new ArgumentOutOfRangeException(
                nameof(origin),
                origin,
                "Origin is outside the grid.");
        }

        if (range < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(range),
                range,
                "Range must be at least one.");
        }

        List<Vector2Int> affectedPositions =
            new();

        int maxDistance = range - 1;

        for (int y = 0;
             y < gridState.Height;
             y++)
        {
            for (int x = 0;
                 x < gridState.Width;
                 x++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);

                // x이동 + y이동이 거리보다 넓으면 return
                int distance =
                    Mathf.Abs(position.x - origin.x)
                    + Mathf.Abs(position.y - origin.y);

                if (distance <= maxDistance)
                {
                    affectedPositions.Add(position);
                }
            }
        }

        return affectedPositions;
    }
}