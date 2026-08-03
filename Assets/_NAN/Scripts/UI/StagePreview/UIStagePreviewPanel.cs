using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 스테이지 제목, 설명과 정답 격자를 하나의 UGUI 미리보기로 표시한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public sealed class UIStagePreviewPanel : MonoBehaviour
    {
        [SerializeField]
        private RectTransform previewGridContainer;

        [SerializeField]
        private UIStagePreviewCell cellPrefab;

        [Header("Grid Decorations")]
        [SerializeField]
        private Image gridBackgroundImage;

        [SerializeField]
        private Image frameImage;

        [SerializeField]
        private Sprite background5x5;

        [SerializeField]
        private Sprite background6x6;

        [SerializeField]
        private Sprite background7x7;

        [SerializeField]
        [Range(0.1f, 1f)]
        private float frameInnerRatio = 0.68f;

        [SerializeField]
        private Sprite wallSprite;

        [SerializeField]
        [Min(0f)]
        private float wallThicknessRatio = 0.0882353f;

        [SerializeField]
        private TMP_Text stageNameLabel;

        [SerializeField]
        private TMP_Text stageDescriptionLabel;

        [SerializeField]
        [Min(0f)]
        private float gridPadding = 24f;

        private readonly List<UIStagePreviewCell> cellPool = new();
        private readonly List<Image> wallPool = new();

        private CanvasGroup canvasGroup;
        private AccessibilityDisplaySettings displaySettings;
        private int activeCellCount;
        private bool isDisplaySettingsSubscribed;

        /// <summary>
        /// 캐러셀 배치와 이동에 사용하는 패널 RectTransform을 반환한다.
        /// </summary>
        public RectTransform RectTransform => (RectTransform)transform;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            SubscribeDisplaySettings();
        }

        private void OnDisable()
        {
            UnsubscribeDisplaySettings();
        }

        /// <summary>
        /// 지정한 스테이지의 문구와 정답 격자를 현재 패널에 표시한다.
        /// </summary>
        /// <param name="stageData">미리보기에 표시할 스테이지 데이터.</param>
        /// <param name="settings">색상 팔레트와 심볼 표시 여부를 제공하는 설정.</param>
        public void Bind(StageData stageData, AccessibilityDisplaySettings settings)
        {
            if (stageData == null)
            {
                throw new ArgumentNullException(nameof(stageData));
            }

            SetDisplaySettings(settings);

            int cellCount = stageData.Width * stageData.Height;
            if (stageData.AnswerPaintStates.Count != cellCount)
            {
                throw new InvalidOperationException("Stage answer paint state count does not match the grid size.");
            }

            stageNameLabel.text = stageData.Title;
            stageDescriptionLabel.text = stageData.Description;

            float cellSize = CalculateCellSize(stageData.Width, stageData.Height);

            LayoutBoardDecorations(stageData, cellSize);
            LayoutCells(stageData, cellCount, cellSize);
            LayoutWalls(stageData, cellSize);
            RefreshCellVisuals();
        }

        /// <summary>
        /// 캐러셀 슬롯이 유효한 스테이지를 가리키는지에 따라 패널 표시 여부를 설정한다.
        /// </summary>
        /// <param name="visible">true면 패널을 표시하고, false면 숨긴다.</param>
        public void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }

        private void SetDisplaySettings(AccessibilityDisplaySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.ActivePalette == null)
            {
                throw new InvalidOperationException("Accessibility display settings requires an active palette.");
            }

            if (displaySettings == settings)
            {
                return;
            }

            UnsubscribeDisplaySettings();
            displaySettings = settings;
            SubscribeDisplaySettings();
        }

        private void LayoutCells(StageData stageData, int cellCount, float cellSize)
        {
            EnsureCellCapacity(cellCount);
            activeCellCount = cellCount;

            for (int index = 0; index < cellPool.Count; index++)
            {
                UIStagePreviewCell cell = cellPool[index];
                bool isActive = index < cellCount;
                cell.gameObject.SetActive(isActive);

                if (!isActive)
                {
                    continue;
                }

                Vector2Int gridPosition = GridIndexUtility.ToPosition(index, stageData.Width, stageData.Height);
                RectTransform cellRect = cell.RectTransform;
                cellRect.anchoredPosition = GridLayoutUtility.GetCellLocalPosition(
                    gridPosition,
                    stageData.Width,
                    stageData.Height,
                    cellSize
                );
                cellRect.sizeDelta = Vector2.one * cellSize;
                cell.SetPaintState(stageData.AnswerPaintStates[index]);
            }
        }

        private void LayoutWalls(StageData stageData, float cellSize)
        {
            int wallCount = stageData.WallPositions.Count;
            EnsureWallCapacity(wallCount);

            float wallThickness = cellSize * wallThicknessRatio;

            for (int index = 0; index < wallPool.Count; index++)
            {
                Image wallImage = wallPool[index];
                bool isActive = index < wallCount;
                wallImage.gameObject.SetActive(isActive);

                if (!isActive)
                {
                    continue;
                }

                Vector2Int wallPosition = stageData.WallPositions[index];
                RectTransform wallRect = wallImage.rectTransform;
                wallRect.anchoredPosition = GridLayoutUtility.GetWallLocalPosition(
                    wallPosition,
                    stageData.Width,
                    stageData.Height,
                    cellSize
                );

                wallRect.sizeDelta = GridLayoutUtility.IsVerticalWall(wallPosition)
                    ? new Vector2(wallThickness, cellSize)
                    : new Vector2(cellSize, wallThickness);
                wallRect.SetAsLastSibling();
            }
        }

        private void LayoutBoardDecorations(StageData stageData, float cellSize)
        {
            Sprite backgroundSprite = GetBackgroundSprite(stageData.Width, stageData.Height);

            gridBackgroundImage.sprite = backgroundSprite;
            gridBackgroundImage.enabled = backgroundSprite != null;

            if (backgroundSprite != null)
            {
                FitImageToSize(
                    gridBackgroundImage,
                    stageData.Width * cellSize,
                    stageData.Height * cellSize
                );
            }

            // GridView와 마찬가지로 프레임 중앙의 실제 개구부 비율을 기준으로
            // 보드 전체가 프레임 안에 들어가도록 프레임의 외곽 크기를 계산한다.
            float boardSize = Mathf.Max(stageData.Width, stageData.Height) * cellSize;
            float frameSize = boardSize / Mathf.Max(frameInnerRatio, 0.1f);
            FitImageToSize(frameImage, frameSize, frameSize);
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

        private static void FitImageToSize(
            Image image,
            float targetWidth,
            float targetHeight)
        {
            if (image == null || image.sprite == null)
            {
                return;
            }

            Vector2 sourceSize = image.sprite.rect.size;
            float scaleX = targetWidth / Mathf.Max(sourceSize.x, 0.0001f);
            float scaleY = targetHeight / Mathf.Max(sourceSize.y, 0.0001f);

            float scale = Mathf.Min(scaleX, scaleY);
            image.rectTransform.sizeDelta = sourceSize * scale;
        }

        private float CalculateCellSize(int width, int height)
        {
            Rect containerRect = previewGridContainer.rect;
            float availableWidth = containerRect.width - gridPadding * 2f;
            float availableHeight = containerRect.height - gridPadding * 2f;

            if (availableWidth <= 0f || availableHeight <= 0f)
            {
                throw new InvalidOperationException("Stage preview grid container has no usable area.");
            }

            return Mathf.Min(availableWidth / width, availableHeight / height);
        }

        private void EnsureCellCapacity(int requiredCount)
        {
            while (cellPool.Count < requiredCount)
            {
                UIStagePreviewCell cell = Instantiate(cellPrefab, previewGridContainer);
                cell.name = $"Preview Cell {cellPool.Count}";
                cellPool.Add(cell);
            }
        }

        private void EnsureWallCapacity(int requiredCount)
        {
            while (wallPool.Count < requiredCount)
            {
                GameObject wallObject = new GameObject(
                    $"Preview Wall {wallPool.Count}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );
                wallObject.transform.SetParent(previewGridContainer, false);

                Image wallImage = wallObject.GetComponent<Image>();
                wallImage.sprite = wallSprite;
                wallImage.color = Color.white;
                wallImage.raycastTarget = false;

                RectTransform wallRect = wallImage.rectTransform;
                wallRect.anchorMin = new Vector2(0.5f, 0.5f);
                wallRect.anchorMax = new Vector2(0.5f, 0.5f);
                wallRect.pivot = new Vector2(0.5f, 0.5f);

                wallPool.Add(wallImage);
            }
        }

        private void RefreshCellVisuals()
        {
            if (displaySettings == null || displaySettings.ActivePalette == null)
            {
                return;
            }

            for (int index = 0; index < activeCellCount; index++)
            {
                cellPool[index].RefreshVisual(displaySettings);
            }
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
            RefreshCellVisuals();
        }

        private void HandleSymbolsEnabledChanged(bool enabled)
        {
            RefreshCellVisuals();
        }
    }
}
