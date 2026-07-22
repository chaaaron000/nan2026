using System;
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

    [SerializeField]
    private GameObject wallPrefab;

    [SerializeField]
    [Min(0f)]
    private float wallThickness = 0.1f;

    //각 셀 클릭 이벤트를 중계하는 이벤트
    public event Action<Vector2Int> CellClicked;

    private CellView[] cellViews;

    private GameObject[] wallObjects;

    // 생성된 격자의 크기, cell 좌표 계산에 사용
    private int gridWidth;
    private int gridHeight;

    /// <summary>
    /// grid의 가로 세로에 따라 셀들을 만드는 함수
    /// </summary>
    public void CreateGrid(GridState gridState)
    {
        ClearGrid();

        gridWidth = gridState.Width;
        gridHeight = gridState.Height;

        cellViews = new CellView[gridState.Width * gridState.Height];

        for (int y = 0; y < gridState.Height; y++)
        {
            for (int x = 0; x < gridState.Width; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);

                CreateCell(gridState, gridPosition);
            }
        }

        CreateWalls(gridState);
    }

    /// <summary>
    /// 현재 화면에 생성된 셀들을 제거한다.
    /// 테스트 버튼을 여러 번 눌러도 격자가 중복 생성되지 않도록 사용한다.
    /// </summary>
    public void ClearGrid()
    {
        if (cellViews != null)
        {
            foreach (CellView cellView in cellViews)
            {
                if (cellView != null)
                {
                    cellView.Clicked -= HandleCellClicked;
                    Destroy(cellView.gameObject);
                }
            }

            cellViews = null;
        }

        if (wallObjects != null)
        {
            foreach (GameObject wallObject in wallObjects)
            {
                if (wallObject != null)
                {
                    Destroy(wallObject);
                }
            }

            wallObjects = null;
        }
    }

    /// <summary>
    /// 셀 하나를 생성하는 함수
    /// </summary>
    private void CreateCell(GridState gridState, Vector2Int gridPosition)
    {
        CellView cellView = Instantiate(cellPrefab, transform);

        //셀 좌표에 해당하는 포지션값을 구해옴
        cellView.transform.localPosition = GridToLocalPosition(gridPosition);

        cellView.Initialize(gridPosition);
        cellView.Clicked += HandleCellClicked;

        int index = gridPosition.y * gridState.Width + gridPosition.x;

        //cellViews 배열에 만들어진 셀을 저장
        cellViews[index] = cellView;
    }

    /// <summary>
    /// GridState의 2배 좌표계 벽 위치를 바탕으로
    /// 테스트용 벽 오브젝트를 생성한다.
    /// </summary>
    private void CreateWalls(GridState gridState)
    {
        wallObjects = new GameObject[gridState.WallPositions.Count];

        int index = 0;

        foreach (Vector2Int wallPosition in gridState.WallPositions)
        {
            GameObject wallObject = Instantiate(wallPrefab, transform);

            wallObject.name = $"Wall ({wallPosition.x}, " + $"{wallPosition.y})";

            wallObject.transform.localPosition = WallToLocalPosition(wallPosition);

            // 2배 좌표계에서 x가 홀수면 좌우 셀 사이의 세로 벽,
            // y가 홀수면 상하 셀 사이의 가로 벽이다.
            bool isVertical = wallPosition.x % 2 != 0;

            wallObject.transform.localScale = isVertical
                ? new Vector3(wallThickness, cellSize, 1f)
                : new Vector3(cellSize, wallThickness, 1f);

            wallObjects[index] = wallObject;
            index++;
        }
    }

    /// <summary>
    /// 셀 클릭시 해당하는 셀 좌표를 넘겨 이벤트 호출
    /// </summary>
    /// <param name="position">셀의 논리 좌표</param>
    private void HandleCellClicked(Vector2Int position)
    {
        CellClicked?.Invoke(position);
    }

    /// <summary>
    /// grid에 cell좌표에 대응하는 실제 transform position을 반환하는 함수
    /// </summary>
    private Vector3 GridToLocalPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * cellSize, gridPosition.y * cellSize, 0f);
    }

    private Vector3 WallToLocalPosition(Vector2Int wallPosition)
    {
        // 벽 좌표는 셀 좌표의 2배 단위이므로 0.5를 곱해
        // 두 셀 중심 사이의 실제 로컬 위치로 되돌린다.
        return new Vector3(wallPosition.x * 0.5f * cellSize, wallPosition.y * 0.5f * cellSize, 0f);
    }

    /// <summary>
    /// 지정한 좌표의 CellView에 PaintState를 표시한다.
    /// </summary>
    public void SetCellPaint(Vector2Int position, PaintState paintState)
    {
        if (cellViews == null)
        {
            throw new InvalidOperationException("Grid has not been created.");
        }

        int index = GridIndexUtility.ToIndex(
            position,
            gridWidth,
            gridHeight);

        cellViews[index].SetPaint(paintState);
    }

    private void OnDestroy()
    {
        ClearGrid();
    }
}
