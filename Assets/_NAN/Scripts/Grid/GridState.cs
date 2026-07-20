using System;
using UnityEngine;

/// <summary>
/// 현재 스테이지의 grid 상태 (크기, paintState 배열)을 담는 클래스
/// </summary>
public sealed class GridState
{
    private readonly PaintState[] cells;

    public int Width { get; }
    public int Height { get; }

    /// <summary>
    /// StageData SO의 width와 height를 전달받아 cell을 초기화하는 생성자
    /// </summary>
    public GridState(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "Grid width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "Grid height must be greater than zero.");
        }

        Width = width;
        Height = height;

        cells = new PaintState[width * height];
    }

    /// <summary>
    /// pos에 칠해져 있는 색깔 paintstate를 반환하는 함수
    /// </summary>
    public PaintState GetPaint(Vector2Int position)
    {
        int index = PositionToIndex(position);
        return cells[index];
    }

    /// <summary>
    /// pos에 해당하는 cell에 paint를 or연산으로 칠하는 함수
    /// </summary>
    public PaintState AddPaint(
        Vector2Int position,
        PaintState paint)
    {
        int index = PositionToIndex(position);

        cells[index] |= paint;

        return cells[index];
    }

    /// <summary>
    /// pos에 해당하는 cell의 paintstate를 000으로 비우는 함수
    /// </summary>
    public PaintState ClearPaint(Vector2Int position)
    {
        int index = PositionToIndex(position);

        cells[index] = PaintState.Empty;

        return cells[index];
    }

    public void ClearAll()
    {
        Array.Clear(cells, 0, cells.Length);
    }

    /// <summary>
    /// Vector2형태의 position을 받아 cells[]에 넣을 인덱스로 변환해주는 함수
    /// </summary>
    private int PositionToIndex(Vector2Int position)
    {
        //position이 범위를 벗어나면 오류 반환
        if (!IsInside(position))
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                position,
                $"Position {position} is outside the " +
                $"{Width}x{Height} grid.");
        }

        return position.y * Width + position.x;
    }
    
    public bool IsInside(Vector2Int position)
    {
        return position.x >= 0
               && position.x < Width
               && position.y >= 0
               && position.y < Height;
    }
}