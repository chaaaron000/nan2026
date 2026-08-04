using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 셀 한 칸의 시각적 표현과 클릭 입력을 담당한다.
/// </summary>
public sealed class CellView : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// 셀이 격자 안에서 차지하는 논리 좌표.
    /// </summary>
    public Vector2Int GridPosition { get; private set; }

    /// <summary>
    /// 셀을 클릭했을 때 해당 논리 좌표와 함께 발생한다.
    /// </summary>
    public event Action<Vector2Int> Clicked;

    [SerializeField]
    private TMP_Text symbolText;

    [Header("아트 머티리얼")]
    // 셀의 물감 상태를 칠할 스프라이트 렌더러.
    private SpriteRenderer spriteRenderer;

    // 정답 패널처럼 셀 클릭을 무시해야 하는 View인지 나타낸다.
    private bool isInteractable = true;

    // 팔레트 또는 심볼 모드가 바뀌어도 동일한 물감 상태를 다시 표시하기 위해 보관한다.
    private PaintState currentPaintState = PaintState.Empty;

    private AccessibilityDisplaySettings displaySettings;
    private bool isDisplaySettingsSubscribed;
    private const float DefaultSymbolSizeRatio = 0.45f;
    private const float WhiteSymbolWidthRatio = 0.68f;
    private const float WhiteSymbolHeightRatio = 0.5f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetPaint(PaintState.Empty);
    }

    private void OnEnable()
    {
        SubscribeDisplaySettings();
        RefreshVisual();
    }

    private void OnDisable()
    {
        UnsubscribeDisplaySettings();
    }

    /// <summary>
    /// 셀을 격자 좌표로 초기화하고 GameObject 이름을 지정한다.
    /// </summary>
    /// <param name="gridPosition">셀의 논리 좌표.</param>
    public void Initialize(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        gameObject.name = $"Cell ({gridPosition.x}, {gridPosition.y})";
    }

    /// <summary>
    /// 셀 클릭 입력을 받을지 설정한다.
    /// </summary>
    /// <param name="interactable">true면 클릭 이벤트를 발생시킨다.</param>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
    }

    /// <summary>
    /// 셀 아트보다 심볼이 앞에 표시되도록 심볼 렌더러의 정렬 정보를 설정한다.
    /// </summary>
    /// <param name="sortingLayerName">심볼을 표시할 Sorting Layer 이름.</param>
    /// <param name="sortingOrder">셀 아트보다 큰 심볼 Sorting Order.</param>
    public void SetSymbolSorting(string sortingLayerName, int sortingOrder)
    {
        if (symbolText == null)
        {
            return;
        }

        Renderer symbolRenderer = symbolText.GetComponent<Renderer>();
        if (symbolRenderer == null)
        {
            return;
        }

        symbolRenderer.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayerName)
            ? "Default"
            : sortingLayerName;
        symbolRenderer.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// 마우스 클릭 또는 터치 입력을 받아 셀 클릭 이벤트를 발생시킨다.
    /// </summary>
    /// <param name="eventData">발생한 포인터 이벤트 데이터.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable)
        {
            return;
        }

        Clicked?.Invoke(GridPosition);
    }

    /// <summary>
    /// 지정한 물감 상태를 셀에 표시한다.
    /// </summary>
    /// <param name="paintState">셀에 표시할 물감 조합 상태.</param>
    public void SetPaint(PaintState paintState)
    {
        currentPaintState = paintState;
        RefreshVisual();
    }

    /// <summary>
    /// 셀이 사용할 접근성 표시 설정을 지정하고 현재 물감 상태를 다시 표시한다.
    /// </summary>
    /// <param name="settings">셀 표시에 사용할 접근성 표시 설정.</param>
    public void SetAccessibilityDisplaySettings(AccessibilityDisplaySettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (displaySettings == settings)
        {
            RefreshVisual();
            return;
        }

        UnsubscribeDisplaySettings();
        displaySettings = settings;
        SubscribeDisplaySettings();
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (displaySettings == null || displaySettings.ActivePalette == null)
        {
            return;
        }

        ColorPaletteSO palette = displaySettings.ActivePalette;
        PaintVisualSet visualSet = palette.GetVisualSet(currentPaintState);
        Material targetMaterial = visualSet?.CellMaterial;

        // 빈 셀은 배경 격자가 담당하므로 셀 SpriteRenderer를 숨긴다.
        // 칠해진 셀만 아트 머티리얼을 가진 SpriteRenderer를 표시한다.
        spriteRenderer.enabled = currentPaintState != PaintState.Empty || targetMaterial != null;

        // 공유 머티리얼만 교체하여 셀 수만큼 런타임 머티리얼 인스턴스가 생기지 않게 한다.
        if (targetMaterial != null && spriteRenderer.sharedMaterial != targetMaterial)
        {
            spriteRenderer.sharedMaterial = targetMaterial;
        }

        // 아트 머티리얼이 없는 Empty 상태나 접근성용 기본 프리팹은 기존 팔레트를 사용한다.
        spriteRenderer.color = targetMaterial == null
            ? palette.GetColor(currentPaintState)
            : Color.white;

        if (symbolText == null)
        {
            return;
        }

        bool shouldShowSymbol = displaySettings.SymbolsEnabled && currentPaintState != PaintState.Empty;
        symbolText.gameObject.SetActive(shouldShowSymbol);

        if (!shouldShowSymbol)
        {
            return;
        }

        symbolText.text = PaintStateVisualUtility.GetSymbol(currentPaintState);
        symbolText.color = palette.GetSymbolColor(currentPaintState);
        FitSymbolToCell();
    }

    private void SubscribeDisplaySettings()
    {
        if (displaySettings == null || isDisplaySettingsSubscribed)
        {
            return;
        }

        displaySettings.PaletteChanged += HandlePaletteChanged;
        displaySettings.SymbolsEnabledChanged += HandleSymbolsEnabledChanged;
        isDisplaySettingsSubscribed = true;
    }

    private void UnsubscribeDisplaySettings()
    {
        if (displaySettings == null || !isDisplaySettingsSubscribed)
        {
            return;
        }

        displaySettings.PaletteChanged -= HandlePaletteChanged;
        displaySettings.SymbolsEnabledChanged -= HandleSymbolsEnabledChanged;
        isDisplaySettingsSubscribed = false;
    }

    private void HandlePaletteChanged(ColorPaletteSO palette)
    {
        RefreshVisual();
    }

    private void HandleSymbolsEnabledChanged(bool enabled)
    {
        RefreshVisual();
    }

    private void FitSymbolToCell()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || symbolText == null)
        {
            return;
        }

        Renderer symbolRenderer = symbolText.GetComponent<Renderer>();
        if (symbolRenderer == null)
        {
            return;
        }

        // 부모 셀은 아트 Sprite의 원본 크기에 맞춰 계속 스케일이 바뀌므로,
        // 심볼의 로컬 스케일을 먼저 초기화한 뒤 현재 셀 크기에 맞춰 다시 계산한다.
        symbolText.transform.localScale = Vector3.one;
        symbolText.ForceMeshUpdate();

        Vector3 currentSymbolSize = symbolRenderer.bounds.size;

        if (currentSymbolSize.x <= 0.0001f || currentSymbolSize.y <= 0.0001f)
        {
            return;
        }

        Vector3 cellSize = spriteRenderer.bounds.size;
        Vector2 targetSymbolSize = GetTargetSymbolSize(cellSize);
        float scale = Mathf.Min(
            targetSymbolSize.x / currentSymbolSize.x,
            targetSymbolSize.y / currentSymbolSize.y);
        symbolText.transform.localScale = Vector3.one * scale;
    }

    private Vector2 GetTargetSymbolSize(Vector3 cellSize)
    {
        if (currentPaintState == PaintState.White)
        {
            return new Vector2(
                cellSize.x * WhiteSymbolWidthRatio,
                cellSize.y * WhiteSymbolHeightRatio);
        }

        float targetSize =
            Mathf.Max(cellSize.x, cellSize.y) * DefaultSymbolSizeRatio;

        return new Vector2(targetSize, targetSize);
    }

}
