using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 게임 설정 팝업의 표시 및 숨김 연출을 담당합니다.
    /// </summary>
    public sealed class UIGameSettings : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private GameObject raycastBlocker;

        [SerializeField]
        private Image background;

        [SerializeField]
        private CanvasGroup rootPanel;

        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private Button cancelButton;

        [Header("Tween")]
        [SerializeField]
        [Range(0f, 1f)]
        private float tweenDuration = 0.5f;

        [SerializeField]
        private Ease tweenEase = Ease.OutCubic;

        private Color backgroundVisibleColor;
        private Sequence activeTween;
        private int transitionVersion;

        /// <summary>
        /// 사용자가 설정 확인을 요청할 때 발생합니다.
        /// </summary>
        public event Action ConfirmRequested;

        /// <summary>
        /// 사용자가 설정 취소를 요청할 때 발생합니다.
        /// </summary>
        public event Action CancelRequested;

        private void Awake()
        {
            confirmButton.onClick.AddListener(HandleConfirmButtonClicked);
            cancelButton.onClick.AddListener(HandleCancelButtonClicked);
        }

        /// <summary>
        /// 설정 팝업을 연출 없이 숨김 상태로 초기화합니다.
        /// </summary>
        public void InitializeHidden()
        {
            StopActiveTween();

            backgroundVisibleColor = background.color;
            transform.localScale = Vector3.one;
            rootPanel.transform.localScale = Vector3.zero;
            rootPanel.alpha = 0f;
            background.color = Color.clear;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            raycastBlocker.SetActive(false);
        }

        /// <summary>
        /// 설정 팝업을 표시합니다.
        /// </summary>
        /// <param name="cancellationToken">팝업이 파괴될 때 연출을 중단할 토큰입니다.</param>
        public async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            int version = BeginTransition();

            transform.localScale = Vector3.one;
            raycastBlocker.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;

            Sequence tween = DOTween.Sequence().SetUpdate(true);
            tween.Join(rootPanel.transform.DOScale(Vector3.one, tweenDuration).SetEase(tweenEase));
            tween.Join(rootPanel.DOFade(1f, tweenDuration).SetEase(Ease.Linear));
            tween.Join(
                background.DOColor(backgroundVisibleColor, tweenDuration).SetEase(Ease.Linear)
            );
            activeTween = tween;

            bool isCanceled = await WaitForTweenAsync(tween, cancellationToken);
            if (isCanceled || version != transitionVersion)
            {
                return;
            }

            canvasGroup.interactable = true;
        }

        /// <summary>
        /// 설정 팝업을 숨깁니다.
        /// </summary>
        /// <param name="cancellationToken">팝업이 파괴될 때 연출을 중단할 토큰입니다.</param>
        public async UniTask HideAsync(CancellationToken cancellationToken)
        {
            int version = BeginTransition();
            canvasGroup.interactable = false;

            Sequence tween = DOTween.Sequence().SetUpdate(true);
            tween.Join(rootPanel.transform.DOScale(Vector3.zero, tweenDuration).SetEase(tweenEase));
            tween.Join(rootPanel.DOFade(0f, tweenDuration).SetEase(Ease.Linear));
            tween.Join(background.DOColor(Color.clear, tweenDuration).SetEase(Ease.Linear));
            activeTween = tween;

            bool isCanceled = await WaitForTweenAsync(tween, cancellationToken);
            if (isCanceled || version != transitionVersion)
            {
                return;
            }

            raycastBlocker.SetActive(false);
            canvasGroup.blocksRaycasts = false;
        }

        private int BeginTransition()
        {
            StopActiveTween();
            return ++transitionVersion;
        }

        private async UniTask<bool> WaitForTweenAsync(
            Sequence tween,
            CancellationToken cancellationToken
        )
        {
            bool isCanceled = await tween
                .AsyncWaitForCompletion()
                .AsUniTask()
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            if (isCanceled)
            {
                tween.Kill();
            }

            if (activeTween == tween)
            {
                activeTween = null;
            }

            return isCanceled;
        }

        private void StopActiveTween()
        {
            activeTween?.Kill();
            activeTween = null;
        }

        private void HandleConfirmButtonClicked()
        {
            ConfirmRequested?.Invoke();
        }

        private void HandleCancelButtonClicked()
        {
            CancelRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelButtonClicked);
            }

            transitionVersion++;
            StopActiveTween();
        }
    }
}
