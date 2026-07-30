using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// 한 셀에 적용될 물감 확산 결과와 연출 정보를 담는 불변 단계다.
/// </summary>
public sealed class PaintSpreadCellStep
{
    /// <summary>대상 셀의 논리 좌표를 반환한다.</summary>
    public Vector2Int Position { get; }

    /// <summary>확산 시작 셀부터의 최단 이동 거리를 반환한다.</summary>
    public int Distance { get; }

    /// <summary>같은 최단 거리로 대상 셀에 들어온 모든 방향을 반환한다.</summary>
    public PaintIncomingDirection IncomingDirections { get; }

    /// <summary>명령 실행 전 셀의 물감 상태를 반환한다.</summary>
    public PaintState PreviousState { get; }

    /// <summary>명령 실행 후 셀의 물감 상태를 반환한다.</summary>
    public PaintState ResultState { get; }

    /// <summary>셀 한 칸의 확산 단계 정보를 생성한다.</summary>
    public PaintSpreadCellStep(
        Vector2Int position,
        int distance,
        PaintIncomingDirection incomingDirections,
        PaintState previousState,
        PaintState resultState)
    {
        Position = position;
        Distance = distance;
        IncomingDirections = incomingDirections;
        PreviousState = previousState;
        ResultState = resultState;
    }
}

/// <summary>
/// 같은 거리에서 동시에 연출되고 적용될 셀 단계 묶음이다.
/// </summary>
public sealed class PaintSpreadWave
{
    private readonly ReadOnlyCollection<PaintSpreadCellStep> steps;

    /// <summary>확산 시작 셀부터의 최단 이동 거리를 반환한다.</summary>
    public int Distance { get; }

    /// <summary>이 거리에서 동시에 처리할 셀 단계 목록을 반환한다.</summary>
    public IReadOnlyList<PaintSpreadCellStep> Steps => steps;

    /// <summary>같은 거리의 셀 단계들을 불변 wave로 묶는다.</summary>
    public PaintSpreadWave(int distance, IList<PaintSpreadCellStep> steps)
    {
        if (steps == null)
        {
            throw new ArgumentNullException(nameof(steps));
        }

        Distance = distance;
        this.steps = Array.AsReadOnly(ToArray(steps));
    }

    private static PaintSpreadCellStep[] ToArray(IList<PaintSpreadCellStep> source)
    {
        PaintSpreadCellStep[] result = new PaintSpreadCellStep[source.Count];
        source.CopyTo(result, 0);
        return result;
    }
}

/// <summary>
/// 한 물감통 사용으로 발생하는 모든 거리별 확산 단계와 상태 변화를 보관한다.
/// </summary>
public sealed class PaintApplicationPlan
{
    private readonly ReadOnlyCollection<PaintSpreadWave> waves;

    /// <summary>거리 오름차순으로 정렬된 불변 확산 wave 목록을 반환한다.</summary>
    public IReadOnlyList<PaintSpreadWave> Waves => waves;

    /// <summary>거리별 확산 wave 목록을 불변 계획으로 고정한다.</summary>
    public PaintApplicationPlan(IList<PaintSpreadWave> waves)
    {
        if (waves == null)
        {
            throw new ArgumentNullException(nameof(waves));
        }

        PaintSpreadWave[] copy = new PaintSpreadWave[waves.Count];
        waves.CopyTo(copy, 0);
        this.waves = Array.AsReadOnly(copy);
    }
}
