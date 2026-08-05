using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 생성된 물감통 한 개의 시각적 표현과 클릭 입력을 담당한다.
/// </summary>
public sealed class PaintBucketView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private RectTransform hitArea;

    [SerializeField]
    private Image bucketImage;

    [SerializeField]
    private TMP_Text rangeText;

    [SerializeField]
    private TMP_Text symbolText;

    [Header("World Visual")]
    [SerializeField]
    [Min(0.1f)]
    private float visualWorldHeight = 1.47f;

    [SerializeField]
    private float visualWorldZ;

    [SerializeField]
    [Range(0.1f, 1f)]
    private float draggingAlpha = 0.6f;

    [SerializeField]
    private string visualSortingLayer = "GridWall";

    [SerializeField]
    private int visualSortingOrder = 10000;

    [SerializeField]
    private int draggingSortingOrderBonus = 1000;

    [SerializeField]
    private Vector2 textSize = new(54f, 50f);

    [SerializeField]
    private Color textOutlineColor = Color.black;

    [SerializeField]
    [Range(0f, 1f)]
    private float textOutlineWidth = 0.22f;

    private readonly List<SpriteRenderer> visualSpriteRenderers = new();
    private readonly List<Renderer> visualRenderers = new();
    private readonly List<int> visualOriginalSortingOrders = new();
    private static readonly Vector2 RangeOnlyTextOffset = Vector2.zero;
    private static readonly Vector2 SymbolModeRangeTextOffset = new(19f, 0f);
    private static readonly Vector2 SymbolTextOffset = new(-19f, 0f);
    private static readonly Vector2 InteractionSize = new(126f, 96f);
    private const float HorizontalHitAreaExpansion = 5f;
    private const float BottomHitAreaExpansion = 9f;
    private const float TopHitAreaExpansion = 36f;
    private const float TextMinimumFontSize = 22f;
    private const float SymbolModeMaximumFontSize = 40f;
    private const float RangeOnlyMaximumFontSize = 44f;
    private const PaintType VisualScaleReferencePaintType = PaintType.Red;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private LayoutElement layoutElement;
    private Camera worldCamera;
    private GameObject visualInstance;
    private Vector2 originalAnchoredPosition;
    private bool originalIgnoreLayout;
    private bool wasRangeTextActiveBeforeDrag;
    private bool wasSymbolTextActiveBeforeDrag;
    private bool isDragging;
    private bool isConsumed;
    private bool isReserved;
    private PaintType paintType;
    private AccessibilityDisplaySettings displaySettings;
    private PaintBucketVisualData visualData;
    private bool isDisplaySettingsSubscribed;
    private int textStyleRefreshFramesRemaining;

    /// <summary>
    /// 플레이어가 이 물감통을 클릭했을 때 발생한다.
    /// </summary>
    public event Action<PaintBucketView> Clicked;

    /// <summary>
    /// 플레이어가 물감통을 드롭했을 때 드롭 위치와 함께 발생한다.
    /// </summary>
    public event Action<PaintBucketView, PointerEventData> Dropped;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>();
        layoutElement = GetComponent<LayoutElement>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        button.onClick.AddListener(HandleButtonClicked);
        ConfigureHitArea();
        ConfigureInteractionArea();
    }

    private void OnEnable()
    {
        if (visualInstance != null)
        {
            visualInstance.SetActive(!isConsumed);
        }

        SubscribeDisplaySettings();
        RefreshSymbol();
        QueueTextStyleRefresh();
        ConfigureInteractionArea();
        SyncVisualToSlot();
    }

    private void OnDisable()
    {
        UnsubscribeDisplaySettings();

        if (visualInstance != null)
        {
            visualInstance.SetActive(false);
        }
    }

    /// <summary>
    /// 물감통 데이터와 Sprite를 바탕으로 최초 표시 상태를 설정한다.
    /// </summary>
    /// <param name="range">물감의 확산 범위.</param>
    /// <param name="bucketSprite">물감통에 표시할 스프라이트.</param>
    /// <param name="newPaintType">물감통이 사용하는 물감 종류.</param>
    public void Initialize(int range, Sprite bucketSprite, PaintType newPaintType)
    {
        if (bucketSprite == null)
        {
            throw new ArgumentNullException(nameof(bucketSprite));
        }

        bucketImage.sprite = bucketSprite;
        rangeText.text = range.ToString();
        paintType = newPaintType;
        RefreshSymbol();
        QueueTextStyleRefresh();

        SetSelected(false);
        SetConsumed(false);
    }

    /// <summary>
    /// 물감통 UI 슬롯 위에 표시할 월드 프리팹을 생성하고 동일한 크기로 맞춘다.
    /// </summary>
    /// <param name="visualPrefab">표시할 색상별 물감통 프리팹.</param>
    /// <param name="visualParent">생성된 월드 물감통 오브젝트를 묶어둘 부모 Transform.</param>
    public void SetVisualPrefab(
        GameObject visualPrefab,
        Transform visualParent)
    {
        if (visualInstance != null)
        {
            Destroy(visualInstance);
            visualInstance = null;
        }

        visualSpriteRenderers.Clear();

        if (visualPrefab == null)
        {
            bucketImage.enabled = true;
            return;
        }

        bucketImage.enabled = true;
        bucketImage.color = new Color(1f, 1f, 1f, 0f);

        visualInstance = visualParent == null
            ? Instantiate(visualPrefab)
            : Instantiate(visualPrefab, visualParent);
        visualInstance.name = $"{visualPrefab.name} View";
        ApplyReferenceVisualScale(visualPrefab);
        visualSpriteRenderers.AddRange(visualInstance.GetComponentsInChildren<SpriteRenderer>(true));
        CacheVisualRenderers();

        ApplyVisualSorting(false);
        ApplyBucketMaterials();
        PlayLoopParticles();
        SetVisualAlpha(1f);
        SyncVisualToSlot();
    }

    /// <summary>
    /// 팔레트별 물감통 전용 Material을 조회할 시각 데이터 원본을 지정한다.
    /// </summary>
    /// <param name="newVisualData">물감통 시각 데이터.</param>
    public void SetVisualData(PaintBucketVisualData newVisualData)
    {
        visualData = newVisualData;
        ApplyBucketMaterials();
    }

    private void ConfigureHitArea()
    {
        if (hitArea == null)
        {
            return;
        }

        // 월드 비주얼의 중심이 UI 슬롯보다 위에 있으므로 윗부분의 판정 범위를 더 넉넉하게 확보한다.
        hitArea.offsetMin = new Vector2(
            -HorizontalHitAreaExpansion,
            -BottomHitAreaExpansion);
        hitArea.offsetMax = new Vector2(
            HorizontalHitAreaExpansion,
            TopHitAreaExpansion);
    }

    private void ConfigureInteractionArea()
    {
        if (rectTransform != null)
        {
            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                InteractionSize.x);
            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                InteractionSize.y);
        }

        if (layoutElement == null)
        {
            return;
        }

        layoutElement.minWidth = InteractionSize.x;
        layoutElement.minHeight = InteractionSize.y;
        layoutElement.preferredWidth = InteractionSize.x;
        layoutElement.preferredHeight = InteractionSize.y;
    }

    /// <summary>
    /// 물감통이 사용할 접근성 표시 설정을 지정하고 현재 심볼 표시를 갱신한다.
    /// </summary>
    /// <param name="settings">물감통 표시에 사용할 접근성 표시 설정.</param>
    public void SetAccessibilityDisplaySettings(AccessibilityDisplaySettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (displaySettings == settings)
        {
            RefreshSymbol();
            return;
        }

        UnsubscribeDisplaySettings();
        displaySettings = settings;
        SubscribeDisplaySettings();
        RefreshSymbol();
        QueueTextStyleRefresh();
        ApplyBucketMaterials();
    }

    /// <summary>
    /// 물감통 선택 상태를 화면에 반영한다.
    /// </summary>
    /// <param name="selected">true면 선택 상태를 표시한다.</param>
    public void SetSelected(bool selected)
    {
    }

    /// <summary>
    /// 물감통의 소모 상태를 화면에 반영한다.
    /// </summary>
    /// <param name="consumed">true면 물감통을 숨기고 선택할 수 없게 한다.</param>
    public void SetConsumed(bool consumed)
    {
        isConsumed = consumed;

        if (consumed)
        {
            isReserved = false;
            SetSelected(false);
        }

        RefreshAvailability();
    }

    /// <summary>물감통이 다음 사용 순서로 예약된 상태를 화면과 입력 상태에 반영한다.</summary>
    /// <param name="reserved">true이면 물감통을 예약 상태로 표시하고 추가 입력을 막는다.</param>
    public void SetReserved(bool reserved)
    {
        isReserved = reserved;

        if (reserved)
        {
            SetSelected(false);
        }

        RefreshAvailability();
    }

    private void RefreshAvailability()
    {
        button.interactable = !isConsumed && !isReserved;
        gameObject.SetActive(!isConsumed && !isReserved);

        if (visualInstance != null)
        {
            visualInstance.SetActive(!isConsumed && !isReserved);
        }
    }

    /// <summary>
    /// 물감통 드래그 시작 시 표시 오브젝트를 반투명하게 만들고 raycast 차단을 해제한다.
    /// </summary>
    /// <param name="eventData">드래그 시작 포인터 이벤트 데이터.</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!button.interactable || isConsumed)
        {
            return;
        }

        isDragging = true;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalIgnoreLayout = layoutElement != null && layoutElement.ignoreLayout;

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        SetDraggingTextVisible(false);
        canvasGroup.blocksRaycasts = false;
        ApplyVisualSorting(true);
        SetVisualAlpha(draggingAlpha);
        SyncVisualToPointer(eventData);
    }

    /// <summary>
    /// 드래그 중 물감통 표시 오브젝트를 포인터 위치로 이동한다.
    /// </summary>
    /// <param name="eventData">현재 포인터 이벤트 데이터.</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        SyncVisualToPointer(eventData);
        SyncSlotByPointerDelta(eventData);
    }

    /// <summary>
    /// 드래그 종료 시 드롭 이벤트를 전달하고 물감통 표시 상태를 원래 슬롯 위치로 되돌린다.
    /// </summary>
    /// <param name="eventData">드롭 위치가 포함된 포인터 이벤트 데이터.</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        ApplyVisualSorting(false);
        SetVisualAlpha(1f);
        Dropped?.Invoke(this, eventData);
        rectTransform.anchoredPosition = originalAnchoredPosition;
        SetDraggingTextVisible(true);

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = originalIgnoreLayout;
        }

        SyncVisualToSlot();
    }

    private void LateUpdate()
    {
        if (textStyleRefreshFramesRemaining > 0)
        {
            textStyleRefreshFramesRemaining--;
            RefreshSymbol();
        }

        if (!isDragging)
        {
            SyncVisualToSlot();
        }
    }

    private void HandleButtonClicked()
    {
        Clicked?.Invoke(this);
    }

    private void RefreshSymbol()
    {
        if (symbolText == null || displaySettings == null)
        {
            return;
        }

        bool shouldShowSymbol = displaySettings.SymbolsEnabled;
        Color textColor = GetBucketTextColor();
        symbolText.gameObject.SetActive(shouldShowSymbol);
        ApplyTextAnchors(shouldShowSymbol);
        ApplyTextStyle(
            rangeText,
            textColor,
            shouldShowSymbol
                ? SymbolModeMaximumFontSize
                : RangeOnlyMaximumFontSize);

        if (!shouldShowSymbol || displaySettings.ActivePalette == null)
        {
            return;
        }

        symbolText.text = paintType == PaintType.Clear
            ? "X"
            : paintType.ToString()[0].ToString();

        // Clear 물감통은 현재 흰색 아이콘을 사용하므로, 팔레트의 빈 셀 색 대신 검정 심볼을 사용한다.
        ApplyTextStyle(
            symbolText,
            textColor,
            SymbolModeMaximumFontSize);
    }

    private Color GetBucketTextColor()
    {
        return Color.black;
    }

    private void ApplyTextAnchors(bool symbolEnabled)
    {
        ApplyTextLayout(rangeText, symbolEnabled ? SymbolModeRangeTextOffset : RangeOnlyTextOffset);
        ApplyTextLayout(symbolText, SymbolTextOffset);
    }

    private void SetDraggingTextVisible(bool visible)
    {
        if (rangeText != null)
        {
            if (!visible)
            {
                wasRangeTextActiveBeforeDrag = rangeText.gameObject.activeSelf;
                rangeText.gameObject.SetActive(false);
            }
            else
            {
                rangeText.gameObject.SetActive(wasRangeTextActiveBeforeDrag);
            }
        }

        if (symbolText == null)
        {
            return;
        }

        if (!visible)
        {
            wasSymbolTextActiveBeforeDrag = symbolText.gameObject.activeSelf;
            symbolText.gameObject.SetActive(false);
            return;
        }

        symbolText.gameObject.SetActive(wasSymbolTextActiveBeforeDrag);
    }

    private void ApplyTextLayout(TMP_Text text, Vector2 offset)
    {
        if (text == null || text.transform is not RectTransform textTransform)
        {
            return;
        }

        textTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textTransform.pivot = new Vector2(0.5f, 0.5f);
        textTransform.anchoredPosition = offset;
        textTransform.sizeDelta = textSize;
    }

    private void ApplyTextStyle(
        TMP_Text text,
        Color textColor,
        float maximumFontSize)
    {
        if (text == null)
        {
            return;
        }

        ApplyFontMaterial(text);

        text.color = textColor;
        text.faceColor = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = TextMinimumFontSize;
        text.fontSizeMax = maximumFontSize;
        text.fontStyle = FontStyles.Bold;
        text.outlineColor = textColor;
        text.outlineWidth = textOutlineWidth;
        text.UpdateMeshPadding();
        text.ForceMeshUpdate();
    }

    private static void ApplyFontMaterial(TMP_Text text)
    {
        if (text.font == null || text.font.material == null)
        {
            return;
        }

        // 폰트를 교체한 프리팹에 이전 폰트 머티리얼 참조가 남아 있으면
        // Face/Outline 색이 뒤집혀 보일 수 있어 현재 폰트의 기본 머티리얼로 정렬한다.
        text.fontMaterial = text.font.material;
    }

    private void QueueTextStyleRefresh()
    {
        textStyleRefreshFramesRemaining = 2;
    }

    private Color GetReadableOutlineColor(Color textColor)
    {
        float luminance =
            (textColor.r * 0.299f)
            + (textColor.g * 0.587f)
            + (textColor.b * 0.114f);

        return luminance < 0.35f ? Color.white : textOutlineColor;
    }

    private void SyncVisualToSlot()
    {
        if (visualInstance == null || rectTransform == null || !isActiveAndEnabled)
        {
            return;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetEventCamera(), rectTransform.position);
        SetVisualScreenPosition(screenPoint);
    }

    private void SyncVisualToPointer(PointerEventData eventData)
    {
        if (visualInstance == null || eventData == null)
        {
            return;
        }

        SetVisualScreenPosition(eventData.position);
    }

    private void SyncSlotByPointerDelta(PointerEventData eventData)
    {
        if (rectTransform == null || eventData == null)
        {
            return;
        }

        float scaleFactor = rootCanvas != null
            ? Mathf.Max(rootCanvas.scaleFactor, 0.0001f)
            : 1f;
        rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    private void SetVisualScreenPosition(Vector2 screenPoint)
    {
        Camera camera = GetWorldCamera();

        if (camera == null)
        {
            return;
        }

        float worldDepth = visualWorldZ - camera.transform.position.z;
        Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, worldDepth));
        visualInstance.transform.position = new Vector3(worldPosition.x, worldPosition.y, visualWorldZ);
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    private Camera GetWorldCamera()
    {
        if (worldCamera != null)
        {
            return worldCamera;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        worldCamera = canvas != null && canvas.worldCamera != null
            ? canvas.worldCamera
            : Camera.main;

        return worldCamera;
    }

    private void CacheVisualRenderers()
    {
        visualRenderers.Clear();
        visualOriginalSortingOrders.Clear();

        foreach (Renderer renderer in visualInstance.GetComponentsInChildren<Renderer>(true))
        {
            visualRenderers.Add(renderer);
            visualOriginalSortingOrders.Add(renderer.sortingOrder);
        }
    }

    private void ApplyVisualSorting(bool dragging)
    {
        int bonus = dragging ? draggingSortingOrderBonus : 0;

        for (int index = 0; index < visualRenderers.Count; index++)
        {
            Renderer renderer = visualRenderers[index];
            renderer.sortingLayerName = string.IsNullOrWhiteSpace(visualSortingLayer)
                ? "Default"
                : visualSortingLayer;
            renderer.sortingOrder =
                visualOriginalSortingOrders[index] + visualSortingOrder + bonus;
        }
    }

    private void ApplyBucketMaterials()
    {
        if (visualInstance == null || displaySettings?.ActivePalette == null)
        {
            return;
        }

        Color bucketPaintColor =
            displaySettings.ActivePalette.GetColor(paintType);
        PaintState paintState = PaintSpreadCalculator.ToPaintState(paintType);
        PaintVisualSet visualSet =
            displaySettings.ActivePalette.GetVisualSet(paintState);

        foreach (Renderer renderer in visualRenderers)
        {
            if (renderer.gameObject.name == "Square")
            {
                // Square는 셀 이펙트가 아니라 물감통 전용 셰이더를 쓰므로
                // 팔레트 색에 맞춰 별도 bucketPaint Material만 교체한다.
                ApplyBucketPaintMaterial(renderer, bucketPaintColor);
                continue;
            }

            PaintEffectMaterialType? materialType =
                GetBucketMaterialType(renderer.gameObject.name);

            if (!materialType.HasValue)
            {
                continue;
            }

            Material material =
                visualSet?.GetEffectMaterial(materialType.Value);

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }

    private void ApplyBucketPaintMaterial(
        Renderer renderer,
        Color bucketPaintColor)
    {
        if (visualData == null)
        {
            return;
        }

        Material bucketPaintMaterial =
            visualData.GetBucketPaintMaterial(paintType, bucketPaintColor);

        if (bucketPaintMaterial != null)
        {
            renderer.sharedMaterial = bucketPaintMaterial;
        }
    }

    private static PaintEffectMaterialType? GetBucketMaterialType(
        string objectName)
    {
        return objectName switch
        {
            "paint" => PaintEffectMaterialType.Center,
            "Paint" => PaintEffectMaterialType.Edge,
            "bubble" => PaintEffectMaterialType.Bubble,
            "glow" => PaintEffectMaterialType.Glow,
            "glowSub" => PaintEffectMaterialType.GlowSub,
            _ => null,
        };
    }

    private void ApplyReferenceVisualScale(GameObject visualPrefab)
    {
        GameObject referencePrefab = GetVisualScaleReferencePrefab(visualPrefab);
        float referenceHeight = CalculateVisualBoundsHeight(referencePrefab);

        if (referenceHeight <= 0.0001f)
        {
            return;
        }

        // Clear 전용 장식 렌더러가 크기 기준에 섞이지 않도록 RGB 기준 프리팹에서 계산한 배율을 모든 물감통에 공통 적용한다.
        float scale = visualWorldHeight / referenceHeight;
        visualInstance.transform.localScale *= scale;
    }

    private GameObject GetVisualScaleReferencePrefab(GameObject fallbackPrefab)
    {
        if (visualData == null)
        {
            return fallbackPrefab;
        }

        GameObject referencePrefab =
            visualData.GetPrefab(VisualScaleReferencePaintType);

        return referencePrefab != null
            ? referencePrefab
            : fallbackPrefab;
    }

    private static float CalculateVisualBoundsHeight(GameObject prefab)
    {
        if (prefab == null)
        {
            return 0f;
        }

        Bounds bounds = new(prefab.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds ? bounds.size.y : 0f;
    }

    private void PlayLoopParticles()
    {
        foreach (ParticleSystem particleSystem in visualInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule mainModule = particleSystem.main;
            mainModule.loop = true;
            particleSystem.Play(true);
        }
    }

    private void SetVisualAlpha(float alpha)
    {
        foreach (SpriteRenderer spriteRenderer in visualSpriteRenderers)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        // 글씨는 드래그 중에도 명확히 읽혀야 하므로 CanvasGroup alpha는 건드리지 않는다.
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
        RefreshSymbol();
        QueueTextStyleRefresh();
        ApplyBucketMaterials();
    }

    private void HandleSymbolsEnabledChanged(bool enabled)
    {
        RefreshSymbol();
        QueueTextStyleRefresh();
    }

    private void OnDestroy()
    {
        UnsubscribeDisplaySettings();

        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }

        if (visualInstance != null)
        {
            Destroy(visualInstance);
        }
    }
}
