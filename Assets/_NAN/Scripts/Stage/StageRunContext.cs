using System;

/// <summary>
/// 현재 플레이 대상으로 선택한 스테이지 데이터를 씬 사이에서 보관합니다.
/// </summary>
public sealed class StageRunContext : LazyPersistentSingleton<StageRunContext>
{
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
}