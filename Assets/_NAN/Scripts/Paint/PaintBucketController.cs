using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 인게임 물감통의 선택, 소모, 복원 및
/// 셀 클릭과의 상호작용을 관리한다.
/// </summary>
public sealed class PaintBucketController
    : MonoBehaviour
{
    // 셀 클릭 이벤트를 제공하는 플레이 격자 View
    [SerializeField]
    private GridView gridView;

    // 스테이지 데이터를 바탕으로 물감통을 생성하는 Generator
    [SerializeField]
    private PaintBucketGenerator bucketGenerator;

    /// <summary>
    /// 물감통 데이터, View와 소모 상태를 묶어 관리하는 내부 관리 항목
    /// </summary>
    private sealed class BucketEntry
    {
        //배치된 물감통 순서에 해당하는 id
        public int Id { get; }
        public PaintBucket Data { get; }
        public PaintBucketView View { get; }
        public bool IsConsumed { get; private set; }
        public bool IsReserved { get; private set; }

        public BucketEntry(
            int id,
            PaintBucket data,
            PaintBucketView view)
        {
            Id = id;
            Data = data;
            View = view;
        }

        public void SetConsumed(bool consumed)
        {
            IsConsumed = consumed;
            if (consumed)
            {
                IsReserved = false;
            }

            View.SetConsumed(consumed);
        }

        public void SetReserved(bool reserved)
        {
            IsReserved = reserved;
            View.SetReserved(reserved);
        }
    }

    // 현재 생성된 물감통 관리 항목 목록
    private readonly List<BucketEntry>
        bucketEntries = new();

    // View를 통해 관리 항목을 찾기 위한 테이블
    private readonly Dictionary<
        PaintBucketView,
        BucketEntry> entryByView = new();

    private readonly List<RaycastResult> raycastResults = new();

    // 현재 플레이어가 선택한 물감통
    private BucketEntry selectedBucket;

    private bool inputEnabled = true;

    /// <summary>
    /// 선택된 물감통을 특정 셀에 사용하려 할 때 발생한다.
    /// 물감통 ID, 물감통 데이터, 클릭한 셀 좌표를 전달한다.
    /// </summary>
    public event Action<
        int,
        PaintBucket,
        Vector2Int> BucketUseRequested;

    /// <summary>물감통 선택과 셀 사용 요청 입력의 허용 여부를 설정한다.</summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    /// <summary>
    /// 현재 스테이지의 물감통 데이터로
    /// Controller와 물감통 UI를 초기화한다.
    /// </summary>
    public void Initialize(
        IReadOnlyList<PaintBucket> bucketData)
    {
        if (bucketData == null)
        {
            throw new ArgumentNullException(
                nameof(bucketData));
        }

        ClearBuckets();

        //제네레이터를 이용해 bucketViews 오브젝트를 만듬
        IReadOnlyList<PaintBucketView> bucketViews =
            bucketGenerator.Generate(bucketData);

        //오브젝트마다 아이디를 붙여 관리항목으로 저장
        for (int id = 0;
             id < bucketViews.Count;
             id++)
        {
            PaintBucketView bucketView =
                bucketViews[id];

            BucketEntry entry =
                new BucketEntry(
                    id,
                    bucketData[id],
                    bucketView);

            bucketEntries.Add(entry);
            entryByView.Add(bucketView, entry);
            
            bucketView.Clicked +=
                HandleBucketClicked;
            bucketView.Dropped +=
                HandleBucketDropped;
        }

        //중복 구독 방어용 
        gridView.CellClicked -= HandleCellClicked;
        gridView.CellClicked += HandleCellClicked;
    }
    
    /// <summary>
    /// 기존의 bucket오브젝트들, 이벤트, list, dictionary, 선택된 버킷을 없애고 초기화하는 함수
    /// </summary>
    private void ClearBuckets()
    {
        foreach (BucketEntry entry
                 in bucketEntries)
        {
            if (entry.View == null)
            {
                continue;
            }

            entry.View.Clicked -=
                HandleBucketClicked;
            entry.View.Dropped -=
                HandleBucketDropped;

            Destroy(entry.View.gameObject);
        }

        bucketEntries.Clear();
        entryByView.Clear();
        selectedBucket = null;
    }

    /// <summary>
    /// 지정한 물감통을 소모 상태로 변경한다.
    /// 실제로 상태가 변경되었다면 true를 반환한다.
    /// </summary>
    public bool Consume(int bucketId)
    {
        BucketEntry entry =
            GetBucketEntry(bucketId);

        //중복 소모 방지
        if (entry.IsConsumed)
        {
            return false;
        }

        //물감통 선택 취소
        if (selectedBucket == entry)
        {
            selectedBucket.View.SetSelected(false);
            selectedBucket = null;
        }

        // 예약된 물감통은 예약 단계에서 이미 입력을 막았으므로
        // 실제 소모 처리 시에는 예약 상태만 해제하고 소모 상태로 확정한다.
        if (entry.IsReserved)
        {
            entry.SetReserved(false);
        }

        //물감통 소모
        entry.SetConsumed(true);

        return true;
    }

    /// <summary>지정한 물감통을 이후 실행될 사용 예약 상태로 전환한다.</summary>
    public bool Reserve(int bucketId)
    {
        BucketEntry entry =
            GetBucketEntry(bucketId);

        if (entry.IsConsumed || entry.IsReserved)
        {
            return false;
        }

        if (selectedBucket == entry)
        {
            selectedBucket.View.SetSelected(false);
            selectedBucket = null;
        }

        entry.SetReserved(true);
        return true;
    }

    /// <summary>지정한 물감통의 사용 예약 상태를 취소한다.</summary>
    public bool ReleaseReservation(int bucketId)
    {
        BucketEntry entry =
            GetBucketEntry(bucketId);

        if (!entry.IsReserved)
        {
            return false;
        }

        entry.SetReserved(false);
        return true;
    }

    /// <summary>
    /// 지정한 물감통의 소모 상태를 복원한다.
    /// 실제로 상태가 변경되었다면 true를 반환한다.
    /// </summary>
    public bool Restore(int bucketId)
    {
        BucketEntry entry =
            GetBucketEntry(bucketId);

        //소모되지 않은 물감통이 들어왔을 경우, false반환
        if (!entry.IsConsumed)
        {
            return false;
        }
        
        entry.SetConsumed(false);

        return true;
    }
    
    /// <summary>
    /// 물감통 클릭시에 호출되는 함수
    /// </summary>
    private void HandleBucketClicked(
        PaintBucketView clickedView)
    {
        if (!isActiveAndEnabled || !inputEnabled)
        {
            return;
        }
        
        if (!entryByView.TryGetValue(
                clickedView,
                out BucketEntry clickedEntry))
        {
            return;
        }

        if (clickedEntry.IsConsumed || clickedEntry.IsReserved)
        {
            return;
        }

        SoundManager.Instance?.PlaySfx(
            SoundKeys.PaintBucketSelect);

        // 선택된 물감통을 다시 누르면 선택을 해제한다.
        if (selectedBucket == clickedEntry)
        {
            selectedBucket.View.SetSelected(false);
            selectedBucket = null;

            return;
        }

        // 선택된 물감통이 있으면, 그 물감통의 선택을 취소한다.
        if (selectedBucket != null)
        {
            selectedBucket.View.SetSelected(false);
        }

        // 클릭 된 물감통을 선택한다.
        selectedBucket = clickedEntry;
        selectedBucket.View.SetSelected(true);
    }

    /// <summary>
    /// 셀 클릭시 호출되는 함수
    /// </summary>
    /// <param name="gridPosition">선택된 셀의 논리 좌표</param>
    private void HandleCellClicked(
        Vector2Int gridPosition)
    {
        if (!isActiveAndEnabled || !inputEnabled)
        {
            return;
        }
        
        if (selectedBucket == null
            || selectedBucket.IsConsumed
            || selectedBucket.IsReserved)
        {
            return;
        }

        // 실제 물감 적용과 소모는 이벤트를 받은
        // 커맨드 실행 계층에서 처리한다.
        BucketUseRequested?.Invoke(
            selectedBucket.Id,
            selectedBucket.Data,
            gridPosition);
    }

    /// <summary>
    /// 물감통을 셀 위에 드롭했을 때 해당 셀에 물감통 사용을 요청한다.
    /// </summary>
    /// <param name="droppedView">드롭된 물감통 View.</param>
    /// <param name="eventData">드롭 위치를 담고 있는 포인터 이벤트 데이터.</param>
    private void HandleBucketDropped(
        PaintBucketView droppedView,
        PointerEventData eventData)
    {
        if (!isActiveAndEnabled || !inputEnabled)
        {
            return;
        }

        if (!entryByView.TryGetValue(
                droppedView,
                out BucketEntry droppedEntry))
        {
            return;
        }

        if (droppedEntry.IsConsumed || droppedEntry.IsReserved)
        {
            return;
        }

        // 셀 바깥에 드롭한 경우에는 사용 요청을 보내지 않는다.
        // View가 이미 원래 슬롯 위치로 되돌아가므로 별도 복구 처리는 필요 없다.
        if (!TryGetDroppedCell(eventData, out CellView droppedCell))
        {
            return;
        }

        BucketUseRequested?.Invoke(
            droppedEntry.Id,
            droppedEntry.Data,
            droppedCell.GridPosition);
    }

    private bool TryGetDroppedCell(
        PointerEventData eventData,
        out CellView cellView)
    {
        cellView = null;

        if (eventData == null || EventSystem.current == null)
        {
            return false;
        }

        raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject == null)
            {
                continue;
            }

            CellView hitCell =
                result.gameObject.GetComponentInParent<CellView>();

            if (hitCell == null)
            {
                continue;
            }

            if (gridView == null || !hitCell.transform.IsChildOf(gridView.transform))
            {
                continue;
            }

            cellView = hitCell;
            return true;
        }

        return false;
    }

    /// <summary>
    /// id를 바탕으로 버킷 관리 항목을 찾아서 반납함
    /// </summary>
    private BucketEntry GetBucketEntry(int bucketId)
    {
        if (bucketId < 0
            || bucketId >= bucketEntries.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bucketId),
                bucketId,
                "Bucket ID is outside the valid range.");
        }

        return bucketEntries[bucketId];
    }
    
    private void OnDestroy()
    {
        if (gridView != null)
        {
            gridView.CellClicked -=
                HandleCellClicked;
        }

        ClearBuckets();
    }
}
