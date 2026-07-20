using UnityEngine;

/// <summary>
/// 화면에 격자에 따라 셀을 만들고 표현하는 역할을 수행하는 클래스
/// </summary>
public sealed class GridView : MonoBehaviour
{
    [SerializeField]
    private CellView cellPrefab;

    [SerializeField]
    private float cellSize = 1f;

    private CellView[] cellViews;

    /// <summary>
    /// grid의 가로 세로에 따라 셀들을 만드는 함수
    /// </summary>
    public void CreateGrid(GridState gridState)
    {
        ClearGrid();

        cellViews = new CellView[
            gridState.Width * gridState.Height];

        for (int y = 0; y < gridState.Height; y++)
        {
            for (int x = 0; x < gridState.Width; x++)
            {
                Vector2Int gridPosition =
                    new Vector2Int(x, y);

                CreateCell(gridState, gridPosition);
            }
        }
    }

    /// <summary>
    /// 현재 화면에 생성된 셀들을 제거한다.
    /// 테스트 버튼을 여러 번 눌러도 격자가 중복 생성되지 않도록 사용한다.
    /// </summary>
    public void ClearGrid()
    {
        if (cellViews == null)
        {
            return;
        }

        foreach (CellView cellView in cellViews)
        {
            if (cellView != null)
            {
                Destroy(cellView.gameObject);
            }
        }

        cellViews = null;
    }

    /// <summary>
    /// 셀 하나를 생성하는 함수
    /// </summary>
    private void CreateCell(
        GridState gridState,
        Vector2Int gridPosition)
    {
        CellView cellView = Instantiate(
            cellPrefab,
            transform);

        //셀 좌표에 해당하는 포지션값을 구해옴
        cellView.transform.localPosition =
            GridToLocalPosition(gridPosition);

        cellView.Initialize(gridPosition);

        int index =
            gridPosition.y * gridState.Width
            + gridPosition.x;

        //cellViews 배열에 만들어진 셀을 저장
        cellViews[index] = cellView;
    }

    /// <summary>
    /// grid에 cell좌표에 대응하는 실제 transform position을 반환하는 함수
    /// </summary>
    private Vector3 GridToLocalPosition(
        Vector2Int gridPosition)
    {
        return new Vector3(
            gridPosition.x * cellSize,
            gridPosition.y * cellSize,
            0f);
    }
}
