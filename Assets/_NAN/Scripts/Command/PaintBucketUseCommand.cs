using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택한 물감통을 특정 셀에 사용해
/// 범위 안의 셀에 물감을 적용하는 명령.
/// </summary>
public sealed class PaintBucketUseCommand : ICommand
{
    // 사용할 물감통의 런타임 식별자
    private readonly int bucketId;

    // 물감통의 종류와 범위 데이터
    private readonly PaintBucket bucket;

    // 물감 확산을 시작할 셀 좌표
    private readonly Vector2Int origin;

    // 셀의 실제 물감 상태
    private readonly GridState gridState;

    // 변경된 셀 상태를 화면에 표시할 View
    private readonly GridView gridView;

    // 사용한 물감통을 소모 처리할 Controller
    private readonly PaintBucketController
        bucketController;

    // 물감통의 영향을 받는 셀 좌표 계산기
    private readonly PaintSpreadCalculator
        spreadCalculator;

    /// <summary>
    /// 물감통 사용에 필요한 데이터와 실행 대상을 설정한다.
    /// </summary>
    public PaintBucketUseCommand(
        int bucketId,
        PaintBucket bucket,
        Vector2Int origin,
        GridState gridState,
        GridView gridView,
        PaintBucketController bucketController,
        PaintSpreadCalculator spreadCalculator)
    {
        this.bucketId = bucketId;

        this.bucket = bucket
            ?? throw new ArgumentNullException(
                nameof(bucket));

        this.origin = origin;

        this.gridState = gridState
            ?? throw new ArgumentNullException(
                nameof(gridState));

        this.gridView = gridView
            ?? throw new ArgumentNullException(
                nameof(gridView));

        this.bucketController = bucketController
            ?? throw new ArgumentNullException(
                nameof(bucketController));

        this.spreadCalculator = spreadCalculator
            ?? throw new ArgumentNullException(
                nameof(spreadCalculator));
    }

    /// <summary>
    /// 물감 범위에 포함된 셀의 상태를 변경하고
    /// 사용한 물감통을 소모한다.
    /// </summary>
    /// <returns>
    /// 명령이 정상적으로 실행되었다면 true.
    /// 물감통이 이미 소모되어 실행하지 못했다면 false.
    /// </returns>
    public bool Execute()
    {
        //칠해질 좌표들 계산
        IReadOnlyList<Vector2Int>
            affectedPositions =
                spreadCalculator.Calculate(
                    gridState,
                    origin,
                    bucket.Range);

        PaintState paintState =
            ConvertToPaintState(
                bucket.PaintType);

        // 같은 물감통으로 명령이 중복 실행되는 것을 막는다.
        if (!bucketController.Consume(bucketId))
        {
            return false;
        }

        //각 좌표별 색칠 명령을 gridstate에게 보냄.
        foreach (Vector2Int position
                 in affectedPositions)
        {
            PaintState result =
                ApplyPaint(
                    position,
                    paintState);

            gridView.SetCellPaint(
                position,
                result);
        }

        return true;
    }

    /// <summary>
    /// 실행한 물감 사용 명령을 되돌린다.
    /// 현재는 임시 구현으로 아무 작업도 수행하지 않는다.
    /// </summary>
    public void Undo()
    {
        // 추후 Execute에서 변경 전 셀 상태를 기록하고,
        // 이곳에서 각 셀 상태와 물감통을 복원한다.
        //
        // bucketController.Restore(bucketId);
    }

    private PaintState ApplyPaint(
        Vector2Int position,
        PaintState paintState)
    {
        if (bucket.PaintType == PaintType.Clear)
        {
            return gridState.ClearPaint(position);
        }

        return gridState.AddPaint(
            position,
            paintState);
    }

    private PaintState ConvertToPaintState(
        PaintType paintType)
    {
        return paintType switch
        {
            PaintType.Red =>
                PaintState.Red,

            PaintType.Green =>
                PaintState.Green,

            PaintType.Blue =>
                PaintState.Blue,

            // Clear는 AddPaint에 사용하지 않지만
            // Execute의 공통 흐름을 위해 Empty를 반환한다.
            PaintType.Clear =>
                PaintState.Empty,

            _ => throw new ArgumentOutOfRangeException(
                nameof(paintType),
                paintType,
                "Unsupported paint type.")
        };
    }
}