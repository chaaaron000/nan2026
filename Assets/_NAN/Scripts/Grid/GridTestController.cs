using UnityEngine;
using TMPro;

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

    // 정답을 읽기 전용으로 표시하는 좌측 격자 View
    [SerializeField]
    private GridView answerGridView;

    // 물감통 생성과 선택을 관리할 Controller
    [SerializeField]
    private PaintBucketController
        bucketController;
    
    // 커맨드 Controller
    [SerializeField]
    private CommandController commandController;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    

    // 현재 테스트 격자의 실제 논리 상태
    private GridState gridState;

    // 현재 플레이 보드와 정답의 일치 여부를 확인하는 판정기
    private StageClearChecker stageClearChecker;

    // 현재는 벽을 고려하지 않는 임시 확산 계산기
    private readonly PaintSpreadCalculator
        spreadCalculator = new();

    private void Start()
    {
        StageData selectedStage = StageRunContext.Instance.SelectedStage;
        if (selectedStage != null)
        {
            stageData = selectedStage;
        }

        if (stageData == null)
        {
            DebugConsole.LogError(
                "[GridTestController] StageData is missing.",
                this);
            SceneTransitionManager.Instance.NotifySceneReady();
            enabled = false;
            return;
        }

        CreateTestGrid();
    }

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
        SoundManager.Instance?.PlayBgm(
            SoundKeys.StageBgm);

        commandController.ClearHistory();
        
        titleText.text = stageData.Title;
        descriptionText.text = stageData.Description;
        
        gridState =
            new GridState(
                stageData.Width,
                stageData.Height,
                stageData.WallPositions);

        // CellView가 먼저 생성되어야
        // 이후 물감 사용 결과를 화면에 표시할 수 있다.
        gridView.CreateGrid(gridState);

        GridState answerGridState =
            new GridState(
                stageData.Width,
                stageData.Height,
                stageData.WallPositions);

        answerGridView.CreateGrid(
            answerGridState,
            false);
        answerGridView.SetCellPaintStates(
            stageData.AnswerPaintStates);

        if (stageClearChecker != null)
        {
            stageClearChecker.StageCleared -= HandleStageCleared;
        }

        stageClearChecker = new StageClearChecker(
            stageData.AnswerPaintStates);
        stageClearChecker.StageCleared += HandleStageCleared;

        bucketController.Initialize(
            stageData.PaintBuckets);

        // 씬 활성화만으로는 퍼즐 View 생성 완료를 보장할 수 없으므로,
        // 모든 초기화가 끝난 뒤 전환 화면을 열 수 있도록 명시적으로 알린다.
        SceneTransitionManager.Instance.NotifySceneReady();
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

        // 선택된 물감통과 셀 입력이 이미 검증된 이벤트이므로,
        // 범위 계산과 화면 갱신보다 먼저 입력 피드백을 재생한다.
        SoundManager.Instance?.PlaySfx(
            SoundKeys.PaintBucketUse);

        if (commandController.Execute(command))
        {
            stageClearChecker.Check(gridState);
        }
    }

    private void HandleStageCleared()
    {
        DebugConsole.Log("Stage cleared.");
    }
}
