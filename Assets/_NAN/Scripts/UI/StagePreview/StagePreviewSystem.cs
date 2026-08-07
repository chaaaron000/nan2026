using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 세 개의 미리보기 패널을 재사용해 스테이지 목록을 좌우로 탐색한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StagePreviewSystem : MonoBehaviour
    {
        private const int PreviewPanelCount = 3;
        private const int CenterSlotIndex = 1;

        [Header("References")]
        [SerializeField]
        private Button prevButton;

        [SerializeField]
        private Button nextButton;

        [SerializeField]
        private RectTransform previewPanelParent;

        [SerializeField]
        private UIStagePreviewPanel previewPrefab;

        [SerializeField]
        private StageCatalog stageCatalog;

        [SerializeField]
        private AccessibilityDisplaySettings accessibilityDisplaySettings;

        [Header("Slide")]
        [SerializeField]
        [Min(1f)]
        private float previewPanelSpace = 1920f;

        [SerializeField]
        [Range(0f, 2f)]
        private float slideDuration = 1f;

        [SerializeField]
        private Ease stageSlideEase = Ease.Linear;

        private readonly UIStagePreviewPanel[] previewPanels = new UIStagePreviewPanel[PreviewPanelCount];

        private int currentStageIndex;
        private bool isInitialized;
        private bool isSliding;
        private Sequence activeSlideTween;

        /// <summary>
        /// 프리뷰 슬라이드 진행 상태가 변경될 때 현재 진행 여부를 알린다.
        /// </summary>
        public event Action<bool> SlideStateChanged;

        /// <summary>
        /// 프리뷰가 현재 슬라이드 중인지 반환한다.
        /// </summary>
        public bool IsSliding => isSliding;

        /// <summary>
        /// 현재 화면 중앙에 선택된 스테이지 데이터를 가져옵니다.
        /// </summary>
        /// <param name="stageData">현재 선택된 스테이지 데이터입니다.</param>
        /// <returns>초기화가 끝났고 슬라이드 중이 아니면 true를 반환합니다.</returns>
        public bool TryGetCurrentStage(out StageData stageData)
        {
            if (!isInitialized || isSliding)
            {
                stageData = null;
                return false;
            }

            stageData = stageCatalog.GetStage(currentStageIndex);
            return true;
        }

        private void Awake()
        {
            accessibilityDisplaySettings = AccessibilityDisplaySettings.Instance;

            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            prevButton.onClick.AddListener(HandlePrevButtonClicked);
            nextButton.onClick.AddListener(HandleNextButtonClicked);
            UIButtonSound.Attach(prevButton);
            UIButtonSound.Attach(nextButton);

            RestoreSelectedStageIndex();

            for (int index = 0; index < PreviewPanelCount; index++)
            {
                previewPanels[index] = Instantiate(previewPrefab, previewPanelParent);
                previewPanels[index].name = $"Stage Preview Slot {index}";
            }

            Canvas.ForceUpdateCanvases();
            ArrangeAndBindAllPanels();
            isInitialized = true;
            UpdateNavigationButtons();
        }

        private void OnDisable()
        {
            if (!isInitialized || !isSliding)
            {
                return;
            }

            activeSlideTween?.Kill();
            activeSlideTween = null;
            SetSliding(false);
            ArrangeAndBindAllPanels();
            UpdateNavigationButtons();
        }

        private void OnDestroy()
        {
            activeSlideTween?.Kill();

            if (prevButton != null)
            {
                prevButton.onClick.RemoveListener(HandlePrevButtonClicked);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNextButtonClicked);
            }
        }

        private bool ValidateConfiguration()
        {
            if (prevButton == null
                || nextButton == null
                || previewPanelParent == null
                || previewPrefab == null
                || stageCatalog == null
                || accessibilityDisplaySettings == null)
            {
                DebugConsole.LogError("[StagePreviewSystem] Required reference is missing.", this);
                return false;
            }

            if (stageCatalog.Count == 0)
            {
                DebugConsole.LogError("[StagePreviewSystem] Stage catalog is empty.", this);
                return false;
            }

            for (int index = 0; index < stageCatalog.Count; index++)
            {
                if (stageCatalog.GetStage(index) != null)
                {
                    continue;
                }

                DebugConsole.LogError($"[StagePreviewSystem] Stage catalog entry {index} is null.", this);
                return false;
            }

            if (accessibilityDisplaySettings.ActivePalette == null)
            {
                DebugConsole.LogError("[StagePreviewSystem] Active color palette is missing.", this);
                return false;
            }

            return true;
        }

        private void RestoreSelectedStageIndex()
        {
            StageData selectedStage = StageRunContext.Instance.SelectedStage;
            if (selectedStage == null)
            {
                return;
            }

            for (int index = 0; index < stageCatalog.Count; index++)
            {
                if (stageCatalog.GetStage(index) != selectedStage)
                {
                    continue;
                }

                currentStageIndex = index;
                return;
            }
        }

        private void HandlePrevButtonClicked()
        {
            TryStartSlide(-1);
        }

        private void HandleNextButtonClicked()
        {
            TryStartSlide(1);
        }

        private void TryStartSlide(int stageIndexDelta)
        {
            int targetStageIndex = currentStageIndex + stageIndexDelta;
            if (!isInitialized || isSliding || targetStageIndex < 0 || targetStageIndex >= stageCatalog.Count)
            {
                return;
            }

            SetSliding(true);
            UpdateNavigationButtons();

            float movement = -stageIndexDelta * previewPanelSpace;
            Sequence sequence = DOTween.Sequence().Pause();

            foreach (UIStagePreviewPanel panel in previewPanels)
            {
                sequence.Join(
                    panel.RectTransform.DOAnchorPosX(panel.RectTransform.anchoredPosition.x + movement, slideDuration)
                        .SetEase(stageSlideEase)
                );
            }

            activeSlideTween = sequence;
            sequence.OnComplete(() => StartCoroutine(CompleteSlideAfterFrame(stageIndexDelta)));
            sequence.Play();
        }

        private IEnumerator CompleteSlideAfterFrame(int stageIndexDelta)
        {
            // 트윈 완료 프레임에는 목표 위치의 패널을 먼저 렌더링한 뒤 재활용한다.
            yield return null;
            CompleteSlide(stageIndexDelta);
        }

        private void CompleteSlide(int stageIndexDelta)
        {
            activeSlideTween = null;
            currentStageIndex += stageIndexDelta;

            UIStagePreviewPanel recycledPanel;
            if (stageIndexDelta > 0)
            {
                recycledPanel = previewPanels[0];
                previewPanels[0] = previewPanels[1];
                previewPanels[1] = previewPanels[2];
                previewPanels[2] = recycledPanel;
            }
            else
            {
                recycledPanel = previewPanels[2];
                previewPanels[2] = previewPanels[1];
                previewPanels[1] = previewPanels[0];
                previewPanels[0] = recycledPanel;
            }

            recycledPanel.SetVisible(false);
            ArrangePanelPositions();

            int recycledSlotIndex = stageIndexDelta > 0 ? 2 : 0;
            BindPanel(recycledSlotIndex);

            SetSliding(false);
            UpdateNavigationButtons();
        }

        private void SetSliding(bool sliding)
        {
            if (isSliding == sliding)
            {
                return;
            }

            isSliding = sliding;
            SlideStateChanged?.Invoke(isSliding);
        }

        private void ArrangeAndBindAllPanels()
        {
            ArrangePanelPositions();

            for (int slotIndex = 0; slotIndex < PreviewPanelCount; slotIndex++)
            {
                BindPanel(slotIndex);
            }
        }

        private void ArrangePanelPositions()
        {
            for (int slotIndex = 0; slotIndex < PreviewPanelCount; slotIndex++)
            {
                RectTransform panelRect = previewPanels[slotIndex].RectTransform;
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = new Vector2((slotIndex - CenterSlotIndex) * previewPanelSpace, 0f);
            }
        }

        private void BindPanel(int slotIndex)
        {
            int stageIndex = currentStageIndex + slotIndex - CenterSlotIndex;
            UIStagePreviewPanel panel = previewPanels[slotIndex];

            if (stageIndex < 0 || stageIndex >= stageCatalog.Count)
            {
                panel.SetVisible(false);
                return;
            }

            panel.Bind(stageCatalog.GetStage(stageIndex), accessibilityDisplaySettings);
            panel.SetVisible(true);
        }

        private void UpdateNavigationButtons()
        {
            prevButton.interactable = !isSliding && currentStageIndex > 0;
            nextButton.interactable = !isSliding && currentStageIndex < stageCatalog.Count - 1;
        }
    }
}
