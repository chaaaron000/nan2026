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
    
    // 커맨드로 인해 영향받은 좌표들의 전 상태를 저장하기 위한 구조체
    private readonly struct PaintSnapshot
    {
        public Vector2Int Position { get; }
        public PaintState PaintState { get; }

        public PaintSnapshot(
            Vector2Int position,
            PaintState paintState)
        {
            Position = position;
            PaintState = paintState;
        }
    }

    // 커맨드로 인해 영향받은 좌표들 상태를 저장하는 리스트
    private readonly List<PaintSnapshot>
        previousPaintStates = new();

    // 이 커맨드의 실행 여부
    private bool isExecuted;

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
        if (isExecuted)
        {
            return false;
        }
        
        //칠해질 좌표들 계산
        IReadOnlyList<Vector2Int>
            affectedPositions =
                spreadCalculator.Calculate(
                    gridState,
                    origin,
                    bucket.Range);

        // 이전 상태 혹시 모르니 비워둠
        previousPaintStates.Clear();
        
        // 영향 받은 좌표들만 딕셔너리에 저장
        foreach (Vector2Int position in affectedPositions)
        {
            previousPaintStates.Add(
                new PaintSnapshot(
                    position,
                    gridState.GetPaint(position)));
        }
        
        // 같은 물감통으로 명령이 중복 실행되는 것을 막는다.
        if (!bucketController.Consume(bucketId))
        {
            previousPaintStates.Clear();
            return false;
        }
        
        //각 좌표별 색칠 명령을 gridstate에게 보냄.
        foreach (Vector2Int position
                 in affectedPositions)
        {
            PaintState result = ApplyPaint(
                position,
                ConvertToPaintState(bucket.PaintType));

            gridView.SetCellPaint(position, result);
        }

        isExecuted = true;
        return true;
    }

    /// <summary>
    /// 변경된 셀을 실행 전 상태로 되돌리고 사용한 물감통을 복원한다.
    /// </summary>
    public void Undo()
    {
        if (!isExecuted)
        {
            return;
        }

        foreach (PaintSnapshot snapshot
                 in previousPaintStates)
        {
            gridState.SetPaint(
                snapshot.Position,
                snapshot.PaintState);

            gridView.SetCellPaint(
                snapshot.Position,
                snapshot.PaintState);
        }

        bucketController.Restore(bucketId);
        isExecuted = false;
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