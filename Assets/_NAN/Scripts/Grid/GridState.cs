using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 스테이지의 grid 상태 (크기, paintState 배열)을 담는 클래스
/// </summary>
public sealed class GridState
{
    private readonly PaintState[] cells;

    // 셀 좌표를 2배로 확장한 좌표계에서의 벽 위치
    private readonly HashSet<Vector2Int> walls;

    public int Width { get; }
    public int Height { get; }

    /// <summary>
    /// 셀 좌표를 2배로 확장한 좌표계에서
    /// 현재 격자에 등록된 벽 좌표를 반환한다.
    /// </summary>
    public IReadOnlyCollection<Vector2Int> WallPositions => walls;

    /// <summary>
    /// StageData SO의 width와 height를 전달받아 cell을 초기화하는 생성자
    /// </summary>
    public GridState(int width, int height)
        : this(width, height, Array.Empty<Vector2Int>()) { }

    /// <summary>
    /// 격자 크기와 셀 사이의 벽 좌표를 전달받아 초기화한다.
    /// 벽 좌표는 셀 좌표를 2배로 확장한 좌표계를 사용한다.
    /// </summary>
    public GridState(int width, int height, IEnumerable<Vector2Int> wallPositions)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "Grid width must be greater than zero."
            );
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "Grid height must be greater than zero."
            );
        }

        Width = width;
        Height = height;

        cells = new PaintState[width * height];
        walls = new HashSet<Vector2Int>();

        if (wallPositions == null)
        {
            throw new ArgumentNullException(nameof(wallPositions));
        }

        // HashSet으로 복사해 중복을 제거하고
        // CanMove에서 벽 존재 여부를 빠르게 조회한다.
        foreach (Vector2Int wallPosition in wallPositions)
        {
            if (!IsValidWallPosition(wallPosition))
            {
                throw new ArgumentException(
                    $"Wall position {wallPosition} is invalid " + $"for the {Width}x{Height} grid.",
                    nameof(wallPositions)
                );
            }

            walls.Add(wallPosition);
        }
    }

    /// <summary>
    /// pos에 칠해져 있는 색깔 paintstate를 반환하는 함수
    /// </summary>
    public PaintState GetPaint(Vector2Int position)
    {
        int index = GridIndexUtility.ToIndex(
            position,
            Width,
            Height);
        
        return cells[index];
    }

    /// <summary>
    /// pos에 해당하는 cell에 paint를 or연산으로 칠하는 함수
    /// </summary>
    public PaintState AddPaint(Vector2Int position, PaintState paint)
    {
        int index = GridIndexUtility.ToIndex(
            position,
            Width,
            Height);
        
        cells[index] |= paint;

        return cells[index];
    }

    /// <summary>
    /// pos에 해당하는 cell의 paintstate를 000으로 비우는 함수
    /// </summary>
    public PaintState ClearPaint(Vector2Int position)
    {
        int index = GridIndexUtility.ToIndex(
            position,
            Width,
            Height);

        cells[index] = PaintState.Empty;

        return cells[index];
    }

    public void ClearAll()
    {
        Array.Clear(cells, 0, cells.Length);
    }

    public bool IsInside(Vector2Int position)
    {
        return GridIndexUtility.IsInside(
            position,
            Width,
            Height);
    }

    /// <summary>
    /// 서로 인접한 두 셀 사이로 이동할 수 있는지 반환한다.
    /// 범위를 벗어나거나 인접하지 않은 좌표는 이동할 수 없다.
    /// </summary>
    public bool CanMove(Vector2Int from, Vector2Int to)
    {
        if (!IsInside(from) || !IsInside(to))
        {
            return false;
        }

        Vector2Int delta = to - from;

        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
        {
            return false;
        }

        // 인접한 두 셀 좌표의 합은
        // 2배 좌표계에서 두 셀 사이의 벽 위치가 된다.
        Vector2Int wallPosition = from + to;

        return !walls.Contains(wallPosition);
    }

    /// <summary>
    /// 지정한 셀에서 한 방향으로 이동할 수 있는지 반환한다.
    /// 실제 판정은 두 셀 좌표를 받는 CanMove에 위임한다.
    /// </summary>
    public bool CanMove(Vector2Int position, GridDirection direction)
    {
        Vector2Int destination = position + direction.ToOffset();

        return CanMove(position, destination);
    }

    private bool IsValidWallPosition(Vector2Int position)
    {
        int maxX = (Width - 1) * 2;
        int maxY = (Height - 1) * 2;

        if (position.x < 0 || position.x > maxX || position.y < 0 || position.y > maxY)
        {
            return false;
        }

        bool xIsOdd = position.x % 2 != 0;
        bool yIsOdd = position.y % 2 != 0;

        // 한 축만 홀수인 좌표만 두 셀 사이의 경계를 나타낸다.
        return xIsOdd != yIsOdd;
    }
}
