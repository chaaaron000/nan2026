using UnityEngine;

/// <summary>
/// 테스트용 격자와 물감통을 초기화하고
/// 물감 사용 Command를 임시로 실행하는 테스트 클래스
/// </summary>
public sealed class GridTestController : MonoBehaviour
{
    // 테스트에 사용할 스테이지 데이터
    [SerializeField]
    private StageData stageData;

    // 테스트 격자를 생성하고 표시할 View
    [SerializeField]
    private GridView gridView;

    // 물감통 생성과 선택을 관리할 Controller
    [SerializeField]
    private PaintBucketController
        bucketController;
    
    // 커맨드 Controller
    [SerializeField]
    private CommandController commandController;

    // 현재 테스트 격자의 실제 논리 상태
    private GridState gridState;

    // 현재는 벽을 고려하지 않는 임시 확산 계산기
    private readonly PaintSpreadCalculator
        spreadCalculator = new();

    private void OnEnable()
    {
        bucketController.BucketUseRequested +=
            HandleBucketUseRequested;
    }

    private void OnDisable()
    {
        bucketController.BucketUseRequested -=
            HandleBucketUseRequested;
    }

    /// <summary>
    /// StageData를 바탕으로 테스트 격자와
    /// 물감통 목록을 초기화한다.
    /// </summary>
    public void CreateTestGrid()
    {
        commandController.ClearHistory();
        
        gridState =
            new GridState(
                stageData.Width,
                stageData.Height,
                stageData.WallPositions);

        // CellView가 먼저 생성되어야
        // 이후 물감 사용 결과를 화면에 표시할 수 있다.
        gridView.CreateGrid(gridState);

        bucketController.Initialize(
            stageData.PaintBuckets);
    }

    private void HandleBucketUseRequested(
        int bucketId,
        PaintBucket bucket,
        Vector2Int gridPosition)
    {
        if (gridState == null)
        {
            return;
        }

        ICommand command =
            new PaintBucketUseCommand(
                bucketId,
                bucket,
                gridPosition,
                gridState,
                gridView,
                bucketController,
                spreadCalculator);

        commandController.Execute(command);
    }
}
