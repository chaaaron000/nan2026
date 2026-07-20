using System;
using UnityEngine;

/// <summary>
/// 셀 한칸의 시각적인 표현을 담당하는 클래스, 추후 paintstate에 따라 애니메이션 재생등을 담당
/// </summary>
public sealed class CellView : MonoBehaviour
{
    //셀의 grid 내에서의 논리 좌표
    public Vector2Int GridPosition { get; private set; }

    //추후 물감통을 들고 셀을 클릭할때 호출 될 수 있는 이벤트
    public event Action<Vector2Int> Clicked;

    /// <summary>
    /// 셀 초기화 함수, 논리좌표를 구하고, 셀 이름을 편집한다
    /// </summary>
    public void Initialize(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        gameObject.name =
            $"Cell ({gridPosition.x}, {gridPosition.y})";
    }

    private void OnMouseDown()
    {
        Clicked?.Invoke(GridPosition);
    }
}