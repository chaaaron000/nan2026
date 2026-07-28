using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 선택 화면에서 사용할 스테이지와 표시 순서를 보관한다.
/// </summary>
[CreateAssetMenu(fileName = "StageCatalog", menuName = "NaN/Stage Catalog")]
public sealed class StageCatalog : ScriptableObject
{
    [SerializeField]
    private List<StageData> stages = new();

    /// <summary>
    /// 카탈로그에 등록된 스테이지 수를 반환한다.
    /// </summary>
    public int Count => stages.Count;

    /// <summary>
    /// 등록 순서대로 정렬된 스테이지 목록을 반환한다.
    /// </summary>
    public IReadOnlyList<StageData> Stages => stages;

    /// <summary>
    /// 지정한 순서의 스테이지 데이터를 반환한다.
    /// </summary>
    /// <param name="index">가져올 스테이지의 0부터 시작하는 순서.</param>
    /// <returns>지정한 순서에 등록된 스테이지 데이터.</returns>
    public StageData GetStage(int index)
    {
        return stages[index];
    }
}