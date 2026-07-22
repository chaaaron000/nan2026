using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물감통이 도달할 셀 좌표를 계산한다.
/// 현재는 벽이 없는 격자의 맨해튼 거리를 사용한다.
/// </summary>
public sealed class PaintSpreadCalculator
{
    // BFS Queue에 저장할 셀 좌표와 최단 거리
    private readonly struct SearchNode
    {
        public Vector2Int Position { get; }
        public int Distance { get; }

        public SearchNode(
            Vector2Int position,
            int distance)
        {
            Position = position;
            Distance = distance;
        }
    }
    
    // 모든 셀에서 검사할 네 방향
    private static readonly GridDirection[] Directions =
    {
        GridDirection.UP,
        GridDirection.RIGHT,
        GridDirection.DOWN,
        GridDirection.LEFT
    };

    /// <summary>
    /// 시작 셀을 포함해 최대 range - 1회 이동하여
    /// 도달할 수 있는 모든 셀 좌표를 반환한다.
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

        int cellCount =
            gridState.Width * gridState.Height;

        // 영향받는 좌표들을 담아 반환할 리스트
        List<Vector2Int> affectedPositions =
            new();

        // 순회용 큐 생성
        Queue<SearchNode> queue =
            new();

        // Vector2Int HashSet보다 셀 인덱스 배열을 사용하는 편이
        // 현재처럼 고정 크기 격자에서는 조회와 메모리 면에서 단순하다.
        bool[] visited =
            new bool[cellCount];

        int maxDistance =
            range - 1;

        int originIndex =
            GridIndexUtility.ToIndex(
                origin,
                gridState.Width,
                gridState.Height);

        visited[originIndex] = true;

        queue.Enqueue(
            new SearchNode(
                origin,
                0));

        // BFS, 상하좌우 방향으로 포지션을 queue에 넣어가며 순회
        while (queue.Count > 0)
        {
            SearchNode current =
                queue.Dequeue();

            affectedPositions.Add(
                current.Position);

            // 현재 셀이 최대 거리에 도달했다면
            // 결과에는 포함하되 이웃 셀로 더 확장하지 않는다.
            if (current.Distance >= maxDistance)
            {
                continue;
            }

            // 네 방향으로 순회
            foreach (GridDirection direction
                     in Directions)
            {
                // 벽에 막히면 continue
                if (!gridState.CanMove(
                        current.Position,
                        direction))
                {
                    continue;
                }
                
                Vector2Int nextPosition =
                    current.Position
                    + direction.ToOffset();

                int nextIndex =
                    GridIndexUtility.ToIndex(
                        nextPosition,
                        gridState.Width,
                        gridState.Height);

                if (visited[nextIndex])
                {
                    continue;
                }

                // 같은 셀이 여러 경로를 통해 Queue에
                // 중복 추가되지 않도록 삽입 시점에 방문 처리한다.
                visited[nextIndex] = true;

                queue.Enqueue(
                    new SearchNode(
                        nextPosition,
                        current.Distance + 1));
            }
        }

        return affectedPositions;
    }
}