using System;
using UnityEngine;

/// <summary>
/// 2차원 격자 좌표와 1차원 배열 인덱스 사이의
/// 변환 및 범위 검사를 제공한다.
/// </summary>
public static class GridIndexUtility
{
    /// <summary>
    /// 지정한 좌표가 격자 범위 안에 있는지 반환한다.
    /// </summary>
    public static bool IsInside(
        Vector2Int position,
        int width,
        int height)
    {
        return position.x >= 0
               && position.x < width
               && position.y >= 0
               && position.y < height;
    }

    /// <summary>
    /// 2차원 격자 좌표를 행 우선 방식의
    /// 1차원 배열 인덱스로 변환한다.
    /// </summary>
    public static int ToIndex(
        Vector2Int position,
        int width,
        int height)
    {
        ValidateGridSize(
            width,
            height);

        if (!IsInside(
                position,
                width,
                height))
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                position,
                $"Position {position} is outside " +
                $"the {width}x{height} grid.");
        }

        return position.y * width
               + position.x;
    }

    /// <summary>
    /// 행 우선 방식의 1차원 배열 인덱스를
    /// 2차원 격자 좌표로 변환한다.
    /// </summary>
    public static Vector2Int ToPosition(
        int index,
        int width,
        int height)
    {
        ValidateGridSize(
            width,
            height);

        int cellCount =
            width * height;

        if (index < 0
            || index >= cellCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Index must be between 0 and " +
                $"{cellCount - 1}.");
        }

        return new Vector2Int(
            index % width,
            index / width);
    }

    private static void ValidateGridSize(
        int width,
        int height)
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
    }
}