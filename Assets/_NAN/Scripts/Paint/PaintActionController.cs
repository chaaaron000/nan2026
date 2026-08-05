using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물감통 사용 요청, 커맨드 실행, 확산 연출, 입력 잠금과 예약 요청을 조정한다.
/// </summary>
public sealed class PaintActionController : MonoBehaviour
{
    [SerializeField] private GridView gridView;
    [SerializeField] private PaintBucketController bucketController;
    [SerializeField] private CommandController commandController;
    [SerializeField] private PaintEffectLibrary paintEffectLibrary = new();

    private readonly PaintSpreadCalculator spreadCalculator = new();
    private readonly Queue<PendingBucketUseRequest> pendingRequests = new();
    private PaintSpreadSequencePlayer sequencePlayer;
    private Coroutine paintSequenceCoroutine;
    private PaintApplicationPlan activePlan;
    private GridState gridState;

    /// <summary>대기 중인 요청을 포함한 모든 물감 액션이 완료될 때 발생한다.</summary>
    public event Action AllActionsCompleted;

    private readonly struct PendingBucketUseRequest
    {
        public int BucketId { get; }
        public PaintBucket Bucket { get; }
        public Vector2Int GridPosition { get; }

        public PendingBucketUseRequest(int bucketId, PaintBucket bucket, Vector2Int gridPosition)
        {
            BucketId = bucketId;
            Bucket = bucket;
            GridPosition = gridPosition;
        }
    }

    private void Awake()
    {
        sequencePlayer = new PaintSpreadSequencePlayer(gridView, paintEffectLibrary, () => gridView.ActivePalette);
    }

    private void OnEnable()
    {
        bucketController.BucketUseRequested += HandleBucketUseRequested;
    }

    private void OnDisable()
    {
        bucketController.BucketUseRequested -= HandleBucketUseRequested;
        CompleteImmediately();
        ClearPendingRequests();
    }

    /// <summary>
    /// 새 격자 상태를 사용하도록 액션 파이프라인과 커맨드 이력을 초기화한다.
    /// </summary>
    /// <param name="newGridState">새 물감 액션이 변경할 격자 상태.</param>
    public void Initialize(GridState newGridState)
    {
        gridState = newGridState ?? throw new ArgumentNullException(nameof(newGridState));
        CompleteImmediately();
        ClearPendingRequests();
        commandController.ClearHistory();
        SetInputEnabled(true, true);
    }

    /// <summary>진행 중인 연출을 최종 상태로 즉시 완료하고 예약 요청을 제거한다.</summary>
    public void ResetActions()
    {
        CompleteImmediately();
        ClearPendingRequests();
    }

    private void HandleBucketUseRequested(int bucketId, PaintBucket bucket, Vector2Int gridPosition)
    {
        if (gridState == null)
        {
            return;
        }

        if (activePlan != null)
        {
            Reserve(bucketId, bucket, gridPosition);
            return;
        }

        Execute(bucketId, bucket, gridPosition, true);
    }

    private bool Execute(int bucketId, PaintBucket bucket, Vector2Int gridPosition, bool playSound)
    {
        PaintBucketUseCommand command = new PaintBucketUseCommand(
            bucketId,
            bucket,
            gridPosition,
            gridState,
            gridView,
            bucketController,
            spreadCalculator);

        if (playSound)
        {
            SoundManager.Instance?.PlaySfx(SoundKeys.PaintBucketUse);
        }

        if (!commandController.Execute(command))
        {
            return false;
        }

        activePlan = command.Plan;
        SetInputEnabled(false, true);
        paintSequenceCoroutine = StartCoroutine(
            sequencePlayer.Play(activePlan, bucket.PaintType, HandleSequenceCompleted));
        return true;
    }

    private void Reserve(int bucketId, PaintBucket bucket, Vector2Int gridPosition)
    {
        if (!bucketController.Reserve(bucketId))
        {
            return;
        }

        pendingRequests.Enqueue(new PendingBucketUseRequest(bucketId, bucket, gridPosition));
        SoundManager.Instance?.PlaySfx(SoundKeys.PaintBucketUse);
    }

    private void HandleSequenceCompleted()
    {
        paintSequenceCoroutine = null;
        activePlan = null;
        SetInputEnabled(true, true);

        if (!TryExecuteNextRequest())
        {
            AllActionsCompleted?.Invoke();
        }
    }

    private bool TryExecuteNextRequest()
    {
        while (pendingRequests.Count > 0)
        {
            PendingBucketUseRequest request = pendingRequests.Dequeue();
            if (!Execute(request.BucketId, request.Bucket, request.GridPosition, false))
            {
                bucketController.ReleaseReservation(request.BucketId);
            }

            if (activePlan != null)
            {
                return true;
            }
        }

        return false;
    }

    private void CompleteImmediately()
    {
        if (activePlan == null)
        {
            return;
        }

        sequencePlayer?.CompleteImmediately();
        if (paintSequenceCoroutine != null)
        {
            StopCoroutine(paintSequenceCoroutine);
            paintSequenceCoroutine = null;
        }

        foreach (PaintSpreadWave wave in activePlan.Waves)
        {
            foreach (PaintSpreadCellStep step in wave.Steps)
            {
                gridView.SetCellPaint(step.Position, step.ResultState);
            }
        }

        activePlan = null;
        SetInputEnabled(true, true);
    }

    private void SetInputEnabled(bool commandInputEnabled, bool bucketInputEnabled)
    {
        commandController.SetInputEnabled(commandInputEnabled);
        bucketController.SetInputEnabled(bucketInputEnabled);
    }

    private void ClearPendingRequests()
    {
        while (pendingRequests.Count > 0)
        {
            PendingBucketUseRequest request = pendingRequests.Dequeue();
            bucketController.ReleaseReservation(request.BucketId);
        }
    }

    private void OnDestroy()
    {
        sequencePlayer?.Dispose();
    }
}
