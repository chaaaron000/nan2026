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

    [Header("격자 배경 및 프레임")]
    [SerializeField]
    private Sprite background5x5;

    [SerializeField]
    private Sprite background6x6;

    [SerializeField]
    private Sprite background7x7;

    [SerializeField]
    private Sprite frameSprite;

    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip("프레임 원본 이미지에서 보드가 들어가는 내부 개구부의 전체 폭 대비 비율")]
    private float frameInnerRatio = 0.68f;

    [SerializeField]
    private string backgroundSortingLayer = "GridBackground";

    [SerializeField]
    private string frameSortingLayer = "GridFrame";

    [SerializeField]
    private string cellSortingLayer = "GridCell";

    [SerializeField]
    private string wallSortingLayer = "GridWall";

    [SerializeField]
    private int backgroundSortingOrder;

    [SerializeField]
    private int frameSortingOrder = 10;

    [SerializeField]
    private int cellSortingOrder = 20;

    [SerializeField]
    private int wallSortingOrder = 30;

    [SerializeField]
    [Tooltip("불투명 셀 이미지의 깊이 버퍼보다 벽을 카메라 쪽에 배치하는 오프셋")]
    private float wallDepthOffset = -0.01f;

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

    private GameObject backgroundObject;

    private GameObject frameObject;

    // 생성된 격자의 크기, cell 좌표 계산에 사용
    private int gridWidth;
    private int gridHeight;

    /// <summary>현재 생성되는 셀 한 변의 월드 크기를 반환한다.</summary>
    public float CellSize => cellSize;

    /// <summary>
    /// 현재 격자에 적용 중인 색상 팔레트를 반환한다.
    /// </summary>
    public ColorPaletteSO ActivePalette => accessibilityDisplaySettings?.ActivePalette;

    /// <summary>논리 셀 좌표를 GridView 기준 로컬 위치로 변환한다.</summary>
    public Vector3 GetCellLocalPosition(Vector2Int gridPosition)
    {
        if (!GridIndexUtility.IsInside(gridPosition, gridWidth, gridHeight))
        {
            throw new ArgumentOutOfRangeException(nameof(gridPosition), gridPosition, "Position is outside the grid.");
        }

        return GridToLocalPosition(gridPosition);
    }

    /// <summary>
    /// 격자 상태에 따라 셀과 벽을 생성하고 상호작용 여부를 설정한다.
    /// </summary>
    /// <param name="gridState">생성할 격자의 크기와 벽 상태.</param>
    /// <param name="interactable">true면 생성된 셀이 클릭 이벤트를 전달한다.</param>
    public void CreateGrid(GridState gridState, bool interactable = true)
    {
        ClearGrid();

        SyncPositionToPlacementPanel();

        gridWidth = gridState.Width;
        gridHeight = gridState.Height;

        CreateBoardDecorations();

        cellViews = new CellView[gridState.Width * gridState.Height];

        for (int y = 0; y < gridState.Height; y++)
        {
            for (int x = 0; x < gridState.Width; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);

                CreateCell(gridState, gridPosition, interactable);
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

        if (backgroundObject != null)
        {
            Destroy(backgroundObject);
            backgroundObject = null;
        }

        if (frameObject != null)
        {
            Destroy(frameObject);
            frameObject = null;
        }
    }

    /// <summary>
    /// 셀 하나를 생성하는 함수
    /// </summary>
    private void CreateCell(GridState gridState, Vector2Int gridPosition, bool interactable)
    {
        CellView cellView = Instantiate(cellPrefab, transform);

        cellView.SetAccessibilityDisplaySettings(GetAccessibilityDisplaySettings());

        //셀 좌표에 해당하는 포지션값을 구해옴
        cellView.transform.localPosition = GridToLocalPosition(gridPosition);
        FitCellToCellSize(cellView);

        SpriteRenderer cellRenderer = cellView.GetComponent<SpriteRenderer>();
        SetSorting(cellRenderer, cellSortingLayer, cellSortingOrder);
        cellView.SetSymbolSorting(cellSortingLayer, cellSortingOrder + 1);

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
            throw new InvalidOperationException("GridView placement panel must be under a Canvas.");
        }

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            transform.position = new Vector3(
                placementPanel.position.x,
                placementPanel.position.y,
                transform.position.z
            );
            return;
        }

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null :
            canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        Camera worldCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        if (worldCamera == null)
        {
            throw new InvalidOperationException("GridView placement panel requires a world camera.");
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, placementPanel.position);

        float worldDepth = Vector3.Dot(
            transform.position - worldCamera.transform.position,
            worldCamera.transform.forward
        );

        Vector3 panelWorldPosition = worldCamera.ScreenToWorldPoint(
            new Vector3(screenPoint.x, screenPoint.y, worldDepth)
        );

        transform.position = new Vector3(panelWorldPosition.x, panelWorldPosition.y, transform.position.z);
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
            bool isVertical = GridLayoutUtility.IsVerticalWall(wallPosition);

            SpriteRenderer wallRenderer = wallObject.GetComponent<SpriteRenderer>();
            FitWallToCellSize(wallObject, wallRenderer, isVertical);
            SetSorting(wallRenderer, wallSortingLayer, wallSortingOrder);

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
        Vector2 localPosition = GridLayoutUtility.GetCellLocalPosition(gridPosition, gridWidth, gridHeight, cellSize);

        return new Vector3(localPosition.x, localPosition.y, 0f);
    }

    private Vector3 WallToLocalPosition(Vector2Int wallPosition)
    {
        Vector2 localPosition = GridLayoutUtility.GetWallLocalPosition(wallPosition, gridWidth, gridHeight, cellSize);

        // 셀 머티리얼이 깊이 버퍼를 기록해도 벽이 이미지 뒤로 가려지지 않게
        // 카메라 쪽으로 아주 조금 분리한다.
        return new Vector3(localPosition.x, localPosition.y, wallDepthOffset);
    }

    private void CreateBoardDecorations()
    {
        Sprite backgroundSprite = GetBackgroundSprite(gridWidth, gridHeight);

        if (backgroundSprite != null)
        {
            backgroundObject = CreateSpriteObject(
                "Grid Background",
                backgroundSprite,
                backgroundSortingLayer,
                backgroundSortingOrder);

            FitSpriteToSize(
                backgroundObject.GetComponent<SpriteRenderer>(),
                gridWidth * cellSize,
                gridHeight * cellSize,
                true);
        }

        if (frameSprite != null)
        {
            frameObject = CreateSpriteObject(
                "Grid Frame",
                frameSprite,
                frameSortingLayer,
                frameSortingOrder);

            // T_Frame의 중앙은 투명하지 않으며 실제 내부 개구부는 전체 폭의 약 68%다.
            // 보드 전체가 장식 안쪽에 들어가도록 개구부 비율을 역산해 프레임 크기를 정한다.
            float boardSize = Mathf.Max(gridWidth, gridHeight) * cellSize;
            float frameSize = boardSize / Mathf.Max(frameInnerRatio, 0.1f);

            FitSpriteToSize(
                frameObject.GetComponent<SpriteRenderer>(),
                frameSize,
                frameSize,
                true);
        }
    }

    private Sprite GetBackgroundSprite(int width, int height)
    {
        if (width == 5 && height == 5)
        {
            return background5x5;
        }

        if (width == 6 && height == 6)
        {
            return background6x6;
        }

        if (width == 7 && height == 7)
        {
            return background7x7;
        }

        return null;
    }

    private GameObject CreateSpriteObject(
        string objectName,
        Sprite sprite,
        string sortingLayer,
        int sortingOrder)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        SetSorting(renderer, sortingLayer, sortingOrder);
        return child;
    }

    private void FitCellToCellSize(CellView cellView)
    {
        SpriteRenderer renderer = cellView.GetComponent<SpriteRenderer>();

        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Vector2 currentSize = Vector2.Scale(
            renderer.sprite.bounds.size,
            new Vector2(
                Mathf.Abs(cellView.transform.localScale.x),
                Mathf.Abs(cellView.transform.localScale.y)));
        float largestSide = Mathf.Max(currentSize.x, currentSize.y);

        if (largestSide > 0f)
        {
            cellView.transform.localScale *= cellSize / largestSide;
        }

        BoxCollider2D hitCollider = cellView.GetComponent<BoxCollider2D>();

        if (hitCollider != null)
        {
            // 셀 Sprite가 교체돼도 클릭 영역이 렌더 영역과 정확히 일치하도록 맞춘다.
            // 빈 셀은 SpriteRenderer만 숨기므로 Collider는 계속 입력을 받을 수 있다.
            hitCollider.size = renderer.sprite.bounds.size;
            hitCollider.offset = renderer.sprite.bounds.center;
        }
    }

    private void FitWallToCellSize(
        GameObject wallObject,
        SpriteRenderer renderer,
        bool isVertical)
    {
        if (renderer == null || renderer.sprite == null)
        {
            wallObject.transform.localScale = isVertical
                ? new Vector3(wallThickness, cellSize, 1f)
                : new Vector3(cellSize, wallThickness, 1f);
            return;
        }

        // 프리팹 Transform 스케일이 적용된 bounds를 다시 스케일 기준으로 사용하면
        // Wall 길이가 약 두 배로 커진다. Sprite 원본의 실제 막대 크기를 기준으로 맞춘다.
        Vector2 spriteSize = renderer.sprite.bounds.size;
        float spriteWidth = Mathf.Max(spriteSize.x, 0.0001f);
        float spriteHeight = Mathf.Max(spriteSize.y, 0.0001f);

        wallObject.transform.localRotation = isVertical
            ? Quaternion.identity
            : Quaternion.Euler(0f, 0f, 90f);

        wallObject.transform.localScale = new Vector3(
            wallThickness / spriteWidth,
            cellSize / spriteHeight,
            1f);
    }

    private void FitSpriteToSize(
        SpriteRenderer renderer,
        float targetWidth,
        float targetHeight,
        bool preserveAspect)
    {
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Vector2 sourceSize = renderer.sprite.bounds.size;
        float scaleX = targetWidth / Mathf.Max(sourceSize.x, 0.0001f);
        float scaleY = targetHeight / Mathf.Max(sourceSize.y, 0.0001f);

        if (preserveAspect)
        {
            float scale = Mathf.Min(scaleX, scaleY);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            return;
        }

        renderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void SetSorting(
        Renderer renderer,
        string sortingLayer,
        int sortingOrder)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayer)
            ? "Default"
            : sortingLayer;
        renderer.sortingOrder = sortingOrder;
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

        int index = GridIndexUtility.ToIndex(position, gridWidth, gridHeight);

        cellViews[index].SetPaint(paintState);
    }

    private AccessibilityDisplaySettings GetAccessibilityDisplaySettings()
    {
        accessibilityDisplaySettings = AccessibilityDisplaySettings.Instance;

        if (accessibilityDisplaySettings == null)
        {
            throw new InvalidOperationException("GridView requires an AccessibilityDisplaySettings reference.");
        }

        if (accessibilityDisplaySettings.ActivePalette == null)
        {
            throw new InvalidOperationException("AccessibilityDisplaySettings requires an active palette.");
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
                nameof(paintStates)
            );
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
