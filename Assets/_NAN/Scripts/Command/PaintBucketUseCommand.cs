using System;
using UnityEngine;

/// <summary>
/// 선택한 물감통의 불변 확산 계획을 만들고 논리 상태와 되돌리기 이력을 적용한다.
/// 화면 반영은 계획을 재생하는 연출 계층이 담당한다.
/// </summary>
public sealed class PaintBucketUseCommand : ICommand
{
    private readonly int bucketId;
    private readonly PaintBucket bucket;
    private readonly Vector2Int origin;
    private readonly GridState gridState;
    private readonly GridView gridView;
    private readonly PaintBucketController bucketController;
    private readonly PaintSpreadCalculator spreadCalculator;
    private bool isExecuted;

    /// <summary>실행 시 생성된 거리별 불변 확산 계획을 반환한다.</summary>
    public PaintApplicationPlan Plan { get; private set; }

    /// <summary>물감통 사용에 필요한 데이터와 실행 대상을 설정한다.</summary>
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
        this.bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
        this.origin = origin;
        this.gridState = gridState ?? throw new ArgumentNullException(nameof(gridState));
        this.gridView = gridView ?? throw new ArgumentNullException(nameof(gridView));
        this.bucketController = bucketController ?? throw new ArgumentNullException(nameof(bucketController));
        this.spreadCalculator = spreadCalculator ?? throw new ArgumentNullException(nameof(spreadCalculator));
    }

    /// <summary>
    /// 확산 계획을 만든 뒤 물감통을 소모하고 논리 GridState를 최종 결과로 변경한다.
    /// </summary>
    public bool Execute()
    {
        if (isExecuted)
        {
            return false;
        }

        PaintApplicationPlan calculatedPlan = spreadCalculator.Calculate(
            gridState,
            origin,
            bucket.Range,
            bucket.PaintType);

        if (!bucketController.Consume(bucketId))
        {
            return false;
        }

        Plan = calculatedPlan;
        foreach (PaintSpreadWave wave in Plan.Waves)
        {
            foreach (PaintSpreadCellStep step in wave.Steps)
            {
                gridState.SetPaint(step.Position, step.ResultState);
            }
        }

        isExecuted = true;
        return true;
    }

    /// <summary>계획에 저장된 이전 상태를 논리 상태와 화면에 복원하고 물감통을 되돌린다.</summary>
    public void Undo()
    {
        if (!isExecuted || Plan == null)
        {
            return;
        }

        foreach (PaintSpreadWave wave in Plan.Waves)
        {
            foreach (PaintSpreadCellStep step in wave.Steps)
            {
                gridState.SetPaint(step.Position, step.PreviousState);
                gridView.SetCellPaint(step.Position, step.PreviousState);
            }
        }

        bucketController.Restore(bucketId);
        isExecuted = false;
    }
}
