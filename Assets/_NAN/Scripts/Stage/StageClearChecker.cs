using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 격자의 모든 셀 상태가 스테이지 정답과 일치하는지 판정한다.
/// </summary>
public sealed class StageClearChecker
{
    private readonly IReadOnlyList<PaintState> answerPaintStates;

    /// <summary>
    /// 현재 스테이지에서 클리어 이벤트가 발생했는지 반환한다.
    /// </summary>
    public bool IsCleared { get; private set; }

    /// <summary>
    /// 플레이어 격자가 정답과 처음 일치했을 때 발생한다.
    /// </summary>
    public event Action StageCleared;

    /// <summary>
    /// 비교할 행 우선 방식의 정답 셀 상태 목록을 설정한다.
    /// </summary>
    /// <param name="answerPaintStates">스테이지 정답 셀 상태 목록.</param>
    public StageClearChecker(IReadOnlyList<PaintState> answerPaintStates)
    {
        this.answerPaintStates = answerPaintStates
            ?? throw new ArgumentNullException(nameof(answerPaintStates));
    }

    /// <summary>
    /// 현재 플레이어 격자가 정답과 일치하면 클리어 이벤트를 발생시킨다.
    /// </summary>
    /// <param name="gridState">비교할 플레이어 격자 상태.</param>
    /// <returns>현재 플레이어 격자가 정답과 일치하면 true.</returns>
    public bool Check(GridState gridState)
    {
        if (gridState == null)
        {
            throw new ArgumentNullException(nameof(gridState));
        }

        int cellCount = gridState.Width * gridState.Height;

        if (answerPaintStates.Count != cellCount)
        {
            throw new InvalidOperationException(
                "Answer paint state count does not match the grid size.");
        }

        for (int index = 0; index < cellCount; index++)
        {
            Vector2Int position = GridIndexUtility.ToPosition(
                index,
                gridState.Width,
                gridState.Height);

            if (gridState.GetPaint(position) != answerPaintStates[index])
            {
                return false;
            }
        }

        if (!IsCleared)
        {
            IsCleared = true;
            StageCleared?.Invoke();
        }

        return true;
    }
}
