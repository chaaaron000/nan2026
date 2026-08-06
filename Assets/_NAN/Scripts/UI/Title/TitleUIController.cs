using System;
using AYellowpaper.SerializedCollections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Nan.UI
{
    /// <summary>
    /// 타이틀 화면의 패널 표시 상태와 패널 간 이동을 관리합니다.
    /// </summary>
    public sealed class TitleUIController : MonoBehaviour
    {
        [System.Serializable]
        public enum PanelType
        {
            MAIN,
            STAGE_SELECTION,
        }

        [System.Serializable]
        public struct PanelData
        {
            public CanvasGroup canvasGroup;
            public Vector2 tweenTargetPosition;
        }

        [Header("Resources")]
        [SerializeField]
        private RectTransform root;

        [SerializeField]
        [SerializedDictionary]
        private SerializedDictionary<PanelType, PanelData> panels = new();

        [Header("Tween")]
        [SerializeField]
        [Range(0f, 1f)]
        private float swapTweenDuration = 0.5f;

        [SerializeField]
        private Ease swapEase = Ease.OutQuart;

        private PanelType currentPanelType = PanelType.MAIN;
        private PanelType initialPanelType = PanelType.MAIN;
        private bool isTweening = false;
        private bool isInitialized = false;

        private void Awake()
        {
            if (root == null)
            {
                DebugConsole.LogError("[TitleUIController] Root is null", this);
                enabled = false;
                return;
            }

            if (!panels.ContainsKey(PanelType.MAIN))
            {
                DebugConsole.LogError("야 임마 Main 패널 설정해라", this);
                enabled = false;
                return;
            }

            foreach (var panel in panels.Values)
            {
                var canvasGroup = panel.canvasGroup;
                if (canvasGroup == null)
                {
                    DebugConsole.LogError("[TitleUIController] Panel CanvasGroup is null", this);
                    enabled = false;
                    return;
                }

                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            initialPanelType = StageRunContext.Instance.ConsumeReturnToStageSelectionRequest()
                ? PanelType.STAGE_SELECTION
                : PanelType.MAIN;

            if (!panels.ContainsKey(initialPanelType))
            {
                DebugConsole.LogError($"[TitleUIController] Panel is not configured: {initialPanelType}", this);
                enabled = false;
                return;
            }

            currentPanelType = initialPanelType;

            var currentPanel = panels[currentPanelType];
            var currentPanelCanvasGroup = currentPanel.canvasGroup;
            currentPanelCanvasGroup.alpha = 1f;
            currentPanelCanvasGroup.blocksRaycasts = true;
            currentPanelCanvasGroup.interactable = true;

            isInitialized = true;
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                return;
            }

            foreach (var panel in panels.Values)
            {
                panel.canvasGroup.alpha = 0f;
                panel.canvasGroup.blocksRaycasts = false;
                panel.canvasGroup.interactable = false;
            }

            isTweening = false;
            currentPanelType = initialPanelType;

            var currentPanel = panels[currentPanelType];
            var currentPanelCanvasGroup = currentPanel.canvasGroup;
            currentPanelCanvasGroup.alpha = 1f;
            currentPanelCanvasGroup.blocksRaycasts = true;
            currentPanelCanvasGroup.interactable = true;

            // 패널은 레이아웃 안의 양의 좌표에 배치되므로, 해당 패널을 화면 원점에 맞추려면 루트를 반대로 이동해야 합니다.
            root.anchoredPosition = -currentPanel.tweenTargetPosition;
            initialPanelType = PanelType.MAIN;
        }

        private void Start()
        {
            SoundManager.Instance?.PlayBgm(SoundKeys.TitleBgm);
        }

        /// <summary>
        /// 지정한 타이틀 패널이 화면에 오도록 루트를 이동합니다.
        /// </summary>
        /// <param name="type">표시할 패널 종류입니다.</param>
        public void ChangePanel(PanelType type)
        {
            if (!isInitialized || !isActiveAndEnabled || isTweening || type == currentPanelType)
            {
                return;
            }

            if (!panels.ContainsKey(type))
            {
                DebugConsole.LogError($"[TitleUIController] Panel is not configured: {type}", this);
                return;
            }

            ChangePanelAsync(type).Forget();
        }

        private async UniTask ChangePanelAsync(PanelType type)
        {
            isTweening = true;

            var currentPanel = panels[currentPanelType];
            var targetPanel = panels[type];

            currentPanel.canvasGroup.blocksRaycasts = false;
            currentPanel.canvasGroup.interactable = false;
            targetPanel.canvasGroup.alpha = 1f;

            var tween = root.DOAnchorPos(-targetPanel.tweenTargetPosition, swapTweenDuration).SetEase(swapEase);

            var isCanceled = await tween.AsyncWaitForCompletion()
                .AsUniTask()
                .AttachExternalCancellation(this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();

            if (isCanceled)
            {
                tween.Kill();
                isTweening = false;
                return;
            }

            currentPanel.canvasGroup.alpha = 0f;
            targetPanel.canvasGroup.blocksRaycasts = true;
            targetPanel.canvasGroup.interactable = true;
            currentPanelType = type;
            isTweening = false;
        }
    }
}