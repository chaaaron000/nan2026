using System;

/// <summary>
/// 현재 플레이 대상으로 선택한 스테이지 데이터를 씬 사이에서 보관합니다.
/// </summary>
public sealed class StageRunContext : LazyPersistentSingleton<StageRunContext>
{
    private bool returnToStageSelectionRequested;

    /// <summary>
    /// 현재 플레이 대상으로 선택된 스테이지 데이터를 반환합니다.
    /// </summary>
    public StageData SelectedStage { get; private set; }

    /// <summary>
    /// 다음 스테이지 씬에서 사용할 스테이지 데이터를 선택합니다.
    /// </summary>
    /// <param name="stageData">플레이 대상으로 사용할 스테이지 데이터입니다.</param>
    public void SelectStage(StageData stageData)
    {
        SelectedStage = stageData ?? throw new ArgumentNullException(nameof(stageData));
    }

    /// <summary>
    /// 타이틀 씬 진입 시 현재 스테이지가 선택된 스테이지 선택 화면을 표시하도록 요청합니다.
    /// </summary>
    public void RequestReturnToStageSelection()
    {
        returnToStageSelectionRequested = true;
    }

    /// <summary>
    /// 스테이지 선택 화면 복귀 요청을 한 번만 소비합니다.
    /// </summary>
    /// <returns>소비할 복귀 요청이 있으면 true를 반환합니다.</returns>
    public bool ConsumeReturnToStageSelectionRequest()
    {
        bool wasRequested = returnToStageSelectionRequested;
        returnToStageSelectionRequested = false;
        return wasRequested;
    }
}
