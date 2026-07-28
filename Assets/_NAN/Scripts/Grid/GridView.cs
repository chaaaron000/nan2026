using System;
using UnityEngine;

using System.Collections.Generic;

/// <summary>
/// 화면에 격자에 따라 셀을 만들고 표현하는 역할을 수행하는 클래스
/// </summary>
public sealed class GridView : MonoBehaviour
{
    [SerializeField]
    private CellView cellPrefab;

    [SerializeField]
    [Tooltip("생성된 격자의 중심으로 사용할 UI 패널")]
    private RectTransform placementPanel;

    [SerializeField]
    private float cellSize = 1f;

    [SerializeField]
    private GameObject wallPrefab;

    [SerializeField]
    [Min(0f)]
    private float wallThickness = 0.1f;

    [SerializeField]
    private AccessibilityDisplaySettings accessibilityDisplaySettings;

    //각 셀 클릭 이벤트를 중계하는 이벤트
    public event Action<Vector2Int> CellClicked;

    private CellView[] cellViews;

    private GameObject[] wallObjects;

    // 생성된 격자의 크기, cell 좌표 계산에 사용
    private int gridWidth;
    private int gridHeight;

    /// <summary>
    /// 격자 상태에 따라 셀과 벽을 생성하고 상호작용 여부를 설정한다.
    /// </summary>
    /// <param name="gridState">생성할 격자의 크기와 벽 상태.</param>
    /// <param name="interactable">true면 생성된 셀이 클릭 이벤트를 전달한다.</param>
    public void CreateGrid(
        GridState gridState,
        bool interactable = true)
    {
        ClearGrid();

        SyncPositionToPlacementPanel();

        gridWidth = gridState.Width;
        gridHeight = gridState.Height;

        cellViews = new CellView[gridState.Width * gridState.Height];

        for (int y = 0; y < gridState.Height; y++)
        {
            for (int x = 0; x < gridState.Width; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);

                CreateCell(
                    gridState,
                    gridPosition,
                    interactable);
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
    private void CreateCell(
        GridState gridState,
        Vector2Int gridPosition,
        bool interactable)
    {
        CellView cellView = Instantiate(cellPrefab, transform);

        cellView.SetAccessibilityDisplaySettings(
            GetAccessibilityDisplaySettings());

        //셀 좌표에 해당하는 포지션값을 구해옴
        cellView.transform.localPosition = GridToLocalPosition(gridPosition);

        cellView.Initialize(gridPosition);
        cellView.SetInteractable(interactable);

        if (interactable)
        {
            cellView.Clicked += HandleCellClicked;
        }

        int index = gridPosition.y * gridState.Width + gridPosition.x;

        //cellViews 배열에 만들어진 셀을 저장
        cellViews[index] = cellView;
    }

    /// <summary>
    /// UI 패널의 중심을 격자 오브젝트의 월드 위치로 변환한다.
    /// </summary>
    private void SyncPositionToPlacementPanel()
    {
        if (placementPanel == null)
        {
            return;
        }

        Canvas canvas = placementPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            throw new InvalidOperationException(
                "GridView placement panel must be under a Canvas.");
        }

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            transform.position = new Vector3(
                placementPanel.position.x,
                placementPanel.position.y,
                transform.position.z);
            return;
        }

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera != null
                ? canvas.worldCamera
                : Camera.main;
        Camera worldCamera = canvas.worldCamera != null
            ? canvas.worldCamera
            : Camera.main;

        if (worldCamera == null)
        {
            throw new InvalidOperationException(
                "GridView placement panel requires a world camera.");
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            eventCamera,
            placementPanel.position);

        float worldDepth = Vector3.Dot(
            transform.position - worldCamera.transform.position,
            worldCamera.transform.forward);

        Vector3 panelWorldPosition = worldCamera.ScreenToWorldPoint(
            new Vector3(
                screenPoint.x,
                screenPoint.y,
                worldDepth));

        transform.position = new Vector3(
            panelWorldPosition.x,
            panelWorldPosition.y,
            transform.position.z);
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
        // GridView의 Transform을 격자 전체의 중심으로 사용한다.
        // 홀수 크기에서는 중앙 셀이 원점에 오고,
        // 짝수 크기에서는 중앙 4개 셀의 중심이 원점에 오도록 반 칸을 허용한다.
        float centerX = (gridWidth - 1) * 0.5f;
        float centerY = (gridHeight - 1) * 0.5f;

        return new Vector3(
            (gridPosition.x - centerX) * cellSize,
            (gridPosition.y - centerY) * cellSize,
            0f);
    }

    private Vector3 WallToLocalPosition(Vector2Int wallPosition)
    {
        // 벽 좌표는 셀 좌표의 2배 단위이므로 0.5를 곱해
        // 두 셀 중심 사이의 실제 로컬 위치로 되돌린다.
        // 셀과 같은 중심 보정값을 적용해야 벽도 패널의 중앙 기준으로 정렬된다.
        float centerX = (gridWidth - 1) * 0.5f;
        float centerY = (gridHeight - 1) * 0.5f;

        return new Vector3(
            (wallPosition.x * 0.5f - centerX) * cellSize,
            (wallPosition.y * 0.5f - centerY) * cellSize,
            0f);
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

    private AccessibilityDisplaySettings GetAccessibilityDisplaySettings()
    {
        if (accessibilityDisplaySettings == null)
        {
            throw new InvalidOperationException(
                "GridView requires an AccessibilityDisplaySettings reference.");
        }

        if (accessibilityDisplaySettings.ActivePalette == null)
        {
            throw new InvalidOperationException(
                "AccessibilityDisplaySettings requires an active palette.");
        }

        return accessibilityDisplaySettings;
    }

    /// <summary>
    /// 행 우선 방식의 셀 상태 목록을 현재 생성된 모든 셀에 한 번에 표시한다.
    /// </summary>
    /// <param name="paintStates">격자 셀 수와 같은 길이의 행 우선 셀 상태 목록.</param>
    public void SetCellPaintStates(IReadOnlyList<PaintState> paintStates)
    {
        if (cellViews == null)
        {
            throw new InvalidOperationException("Grid has not been created.");
        }

        if (paintStates == null)
        {
            throw new ArgumentNullException(nameof(paintStates));
        }

        if (paintStates.Count != cellViews.Length)
        {
            throw new ArgumentException(
                $"Paint state count must be {cellViews.Length}, but was {paintStates.Count}.",
                nameof(paintStates));
        }

        for (int index = 0; index < cellViews.Length; index++)
        {
            cellViews[index].SetPaint(paintStates[index]);
        }
    }

    private void OnDestroy()
    {
        ClearGrid();
    }
}
