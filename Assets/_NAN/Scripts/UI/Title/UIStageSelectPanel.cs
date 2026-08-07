using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 스테이지 선택 패널의 공통 입력을 처리합니다.
    /// </summary>
    public sealed class UIStageSelectPanel : MonoBehaviour
    {
        private const string StageSceneName = "StageScene";

        [SerializeField]
        private Button stageStartButton;

        [SerializeField]
        private Button backToTitleButton;

        private TitleUIController titleUIController;
        private StagePreviewSystem stagePreviewSystem;

        private void Awake()
        {
            if (stageStartButton == null || backToTitleButton == null)
            {
                DebugConsole.LogError("[UIStageSelectPanel] Required button is null", this);
                enabled = false;
                return;
            }

            stagePreviewSystem = GetComponent<StagePreviewSystem>();
            if (stagePreviewSystem == null)
            {
                DebugConsole.LogError(
                    "[UIStageSelectPanel] StagePreviewSystem was not found",
                    this);
                enabled = false;
                return;
            }

            stageStartButton.interactable = !stagePreviewSystem.IsSliding;

            titleUIController = GetComponentInParent<TitleUIController>();
            if (titleUIController == null)
            {
                DebugConsole.LogError(
                    "[UIStageSelectPanel] TitleUIController was not found in parents",
                    this
                );
                enabled = false;
                return;
            }

            stageStartButton.onClick.AddListener(HandleStageStartButtonClicked);
            backToTitleButton.onClick.AddListener(HandleBackToTitleButtonClicked);
            UIButtonSound.Attach(stageStartButton);
            UIButtonSound.Attach(backToTitleButton);
            stagePreviewSystem.SlideStateChanged += HandleSlideStateChanged;
        }

        private void HandleStageStartButtonClicked()
        {
            if (!stagePreviewSystem.TryGetCurrentStage(out StageData selectedStage))
            {
                return;
            }

            StageRunContext.Instance.SelectStage(selectedStage);
            SceneTransitionManager.Instance.LoadSceneAndWaitForReady(StageSceneName);
        }

        private void HandleBackToTitleButtonClicked()
        {
            titleUIController.ChangePanel(TitleUIController.PanelType.MAIN);
        }

        private void HandleSlideStateChanged(bool isSliding)
        {
            stageStartButton.interactable = !isSliding;
        }

        private void OnDestroy()
        {
            if (stageStartButton != null)
            {
                stageStartButton.onClick.RemoveListener(HandleStageStartButtonClicked);
            }

            if (backToTitleButton != null)
            {
                backToTitleButton.onClick.RemoveListener(HandleBackToTitleButtonClicked);
            }

            if (stagePreviewSystem != null)
            {
                stagePreviewSystem.SlideStateChanged -= HandleSlideStateChanged;
            }
        }
    }
}
