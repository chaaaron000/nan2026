using System;
using UnityEngine;

/// <summary>
/// 플레이 격자의 논리 상태를 생성하고 GridView에 표시한다.
/// </summary>
public sealed class GridController : MonoBehaviour
{
    [SerializeField] private GridView gridView;

    /// <summary>현재 플레이 격자의 논리 상태를 반환한다.</summary>
    public GridState State { get; private set; }

    /// <summary>플레이 격자를 표시하는 View를 반환한다.</summary>
    public GridView View => gridView;

    /// <summary>
    /// 스테이지 데이터에 맞는 새 격자 상태와 화면을 생성한다.
    /// </summary>
    /// <param name="stageData">생성할 격자의 스테이지 데이터.</param>
    public void Initialize(StageData stageData)
    {
        if (stageData == null)
        {
            throw new ArgumentNullException(nameof(stageData));
        }

        State = new GridState(stageData.Width, stageData.Height, stageData.WallPositions);
        gridView.CreateGrid(State);
    }
}
