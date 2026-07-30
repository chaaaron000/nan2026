using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 벽을 고려한 최단 거리 BFS로 물감 확산 계획을 계산한다.
/// </summary>
public sealed class PaintSpreadCalculator
{
    private readonly struct SearchNode
    {
        public Vector2Int Position { get; }
        public int Distance { get; }

        public SearchNode(Vector2Int position, int distance)
        {
            Position = position;
            Distance = distance;
        }
    }

    private static readonly GridDirection[] Directions =
    {
        GridDirection.UP,
        GridDirection.RIGHT,
        GridDirection.DOWN,
        GridDirection.LEFT,
    };

    /// <summary>
    /// 시작 셀을 거리 0으로 두고 최대 range - 1회 이동한 거리별 불변 확산 계획을 만든다.
    /// 같은 최단 거리로 여러 경로가 도달하면 들어온 방향을 모두 누적한다.
    /// </summary>
    public PaintApplicationPlan Calculate(
        GridState gridState,
        Vector2Int origin,
        int range,
        PaintType paintType)
    {
        if (gridState == null)
        {
            throw new ArgumentNullException(nameof(gridState));
        }

        if (!gridState.IsInside(origin))
        {
            throw new ArgumentOutOfRangeException(nameof(origin), origin, "Origin is outside the grid.");
        }

        if (range < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(range), range, "Range must be at least one.");
        }

        int cellCount = gridState.Width * gridState.Height;
        int maxDistance = range - 1;
        int[] distances = new int[cellCount];
        PaintIncomingDirection[] incomingDirections = new PaintIncomingDirection[cellCount];
        List<Vector2Int>[] positionsByDistance = new List<Vector2Int>[maxDistance + 1];

        Array.Fill(distances, -1);
        for (int distance = 0; distance <= maxDistance; distance++)
        {
            positionsByDistance[distance] = new List<Vector2Int>();
        }

        Queue<SearchNode> queue = new();
        int originIndex = GridIndexUtility.ToIndex(origin, gridState.Width, gridState.Height);
        distances[originIndex] = 0;
        positionsByDistance[0].Add(origin);
        queue.Enqueue(new SearchNode(origin, 0));

        while (queue.Count > 0)
        {
            SearchNode current = queue.Dequeue();
            if (current.Distance >= maxDistance)
            {
                continue;
            }

            foreach (GridDirection direction in Directions)
            {
                if (!gridState.CanMove(current.Position, direction))
                {
                    continue;
                }

                Vector2Int next = current.Position + direction.ToOffset();
                int nextDistance = current.Distance + 1;
                int nextIndex = GridIndexUtility.ToIndex(next, gridState.Width, gridState.Height);
                PaintIncomingDirection incoming = ToIncomingDirection(direction);

                if (distances[nextIndex] < 0)
                {
                    distances[nextIndex] = nextDistance;
                    incomingDirections[nextIndex] = incoming;
                    positionsByDistance[nextDistance].Add(next);
                    queue.Enqueue(new SearchNode(next, nextDistance));
                    continue;
                }

                // 더 긴 경로는 버리고, 같은 최단 거리로 들어온 새 방향만 중첩한다.
                if (distances[nextIndex] == nextDistance)
                {
                    incomingDirections[nextIndex] |= incoming;
                }
            }
        }

        PaintState addedPaint = ToPaintState(paintType);
        List<PaintSpreadWave> waves = new(positionsByDistance.Length);

        for (int distance = 0; distance < positionsByDistance.Length; distance++)
        {
            List<PaintSpreadCellStep> steps = new(positionsByDistance[distance].Count);
            foreach (Vector2Int position in positionsByDistance[distance])
            {
                int index = GridIndexUtility.ToIndex(position, gridState.Width, gridState.Height);
                PaintState previous = gridState.GetPaint(position);
                PaintState result = paintType == PaintType.Clear
                    ? PaintState.Empty
                    : previous | addedPaint;

                steps.Add(new PaintSpreadCellStep(
                    position,
                    distance,
                    incomingDirections[index],
                    previous,
                    result));
            }

            waves.Add(new PaintSpreadWave(distance, steps));
        }

        return new PaintApplicationPlan(waves);
    }

    /// <summary>물감통 종류를 셀의 비트 플래그 물감 상태로 변환한다.</summary>
    public static PaintState ToPaintState(PaintType paintType)
    {
        return paintType switch
        {
            PaintType.Red => PaintState.Red,
            PaintType.Green => PaintState.Green,
            PaintType.Blue => PaintState.Blue,
            PaintType.Clear => PaintState.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(paintType), paintType, "Unsupported paint type."),
        };
    }

    private static PaintIncomingDirection ToIncomingDirection(GridDirection direction)
    {
        return direction switch
        {
            GridDirection.UP => PaintIncomingDirection.FromBelow,
            GridDirection.RIGHT => PaintIncomingDirection.FromLeft,
            GridDirection.DOWN => PaintIncomingDirection.FromAbove,
            GridDirection.LEFT => PaintIncomingDirection.FromRight,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported grid direction."),
        };
    }
}
