using UnityEngine;

/// <summary>
/// 현재 스테이지 데이터와 스테이지 시작·교체·완료 검사 흐름을 조정한다.
/// </summary>
public sealed class StageSessionController : MonoBehaviour
{
    [SerializeField] private StageData stageData;
    [SerializeField] private GridController gridController;
    [SerializeField] private StagePresentationController presentationController;
    [SerializeField] private PaintActionController paintActionController;
    [SerializeField] private PaintBucketController bucketController;
    [SerializeField] private StageClearController stageClearController;

    private StageClearChecker clearChecker;

    /// <summary>현재 플레이 중인 스테이지 데이터를 반환한다.</summary>
    public StageData CurrentStage => stageData;

    private void Start()
    {
        StageData selectedStage = StageRunContext.Instance.SelectedStage;
        if (selectedStage != null)
        {
            stageData = selectedStage;
        }

        if (stageData == null)
        {
            DebugConsole.LogError("[StageSessionController] StageData is missing.", this);
            SceneTransitionManager.Instance.NotifySceneReady();
            enabled = false;
            return;
        }

        LoadStageInternal(stageData, true);
    }

    private void OnEnable()
    {
        paintActionController.AllActionsCompleted += HandleAllActionsCompleted;
    }

    private void OnDisable()
    {
        paintActionController.AllActionsCompleted -= HandleAllActionsCompleted;
    }

    /// <summary>
    /// 씬을 전환하지 않고 지정한 스테이지를 새로 시작한다.
    /// </summary>
    /// <param name="nextStage">새로 시작할 스테이지 데이터.</param>
    public void LoadStage(StageData nextStage)
    {
        if (nextStage == null)
        {
            return;
        }

        LoadStageInternal(nextStage, false);
    }

    private void LoadStageInternal(StageData nextStage, bool notifySceneReady)
    {
        stageData = nextStage;
        StageRunContext.Instance.SelectStage(stageData);
        stageClearController.Hide();
        gridController.Initialize(stageData);
        presentationController.Show(stageData);
        bucketController.Initialize(stageData.PaintBuckets);
        paintActionController.Initialize(gridController.State);

        clearChecker = new StageClearChecker(stageData.AnswerPaintStates);
        SoundManager.Instance?.PlayBgm(SoundKeys.StageBgm);

        if (notifySceneReady)
        {
            SceneTransitionManager.Instance.NotifySceneReady();
        }
    }

    private void HandleAllActionsCompleted()
    {
        if (clearChecker.Check(gridController.State))
        {
            DebugConsole.Log("Stage cleared.");
            stageClearController.Show(stageData);
        }
    }
}
