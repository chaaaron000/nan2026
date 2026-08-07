using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
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

        [Header("Audio")]
        [SerializeField]
        private Slider masterVolumeSlider;

        [SerializeField]
        private Slider bgmVolumeSlider;

        [SerializeField]
        private Slider sfxVolumeSlider;

        [Header("Accessibility")]
        [SerializeField]
        private TMP_Dropdown colorVisionCorrectionDropdown;

        [SerializeField]
        private Toggle colorSymbolDisplayToggle;

        [SerializeField]
        private UIRGBDiagram rgbDiagram;

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

        /// <summary>
        /// 사용자가 Master 볼륨을 변경할 때 발생합니다.
        /// </summary>
        public event Action<float> MasterVolumeChanged;

        /// <summary>
        /// 사용자가 BGM 볼륨을 변경할 때 발생합니다.
        /// </summary>
        public event Action<float> BgmVolumeChanged;

        /// <summary>
        /// 사용자가 효과음 볼륨을 변경할 때 발생합니다.
        /// </summary>
        public event Action<float> SfxVolumeChanged;

        /// <summary>
        /// 사용자가 색각 보정 종류를 변경할 때 발생합니다.
        /// </summary>
        public event Action<ColorVisionCorrection> ColorVisionCorrectionChanged;

        /// <summary>
        /// 사용자가 색상 심볼 표시 여부를 변경할 때 발생합니다.
        /// </summary>
        public event Action<bool> SymbolsEnabledChanged;

        private void Awake()
        {
            confirmButton.onClick.AddListener(HandleConfirmButtonClicked);
            cancelButton.onClick.AddListener(HandleCancelButtonClicked);
            UIButtonSound.Attach(confirmButton);
            UIButtonSound.Attach(cancelButton);
            masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            bgmVolumeSlider.onValueChanged.AddListener(HandleBgmVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            colorVisionCorrectionDropdown.onValueChanged.AddListener(HandleColorVisionCorrectionChanged);
            colorSymbolDisplayToggle.onValueChanged.AddListener(HandleSymbolsEnabledChanged);

            colorVisionCorrectionDropdown.ClearOptions();
            colorVisionCorrectionDropdown.AddOptions(
                new List<string>
                {
                    "Default",
                    "Red-Green",
                    "Blue-Yellow",
                }
            );
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
        /// 오디오 슬라이더에 현재 적용된 볼륨을 표시합니다.
        /// </summary>
        /// <param name="masterVolume">0부터 1 사이의 Master 볼륨.</param>
        /// <param name="bgmVolume">0부터 1 사이의 BGM 볼륨.</param>
        /// <param name="sfxVolume">0부터 1 사이의 효과음 볼륨.</param>
        public void SetAudioVolumes(float masterVolume, float bgmVolume, float sfxVolume)
        {
            masterVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(masterVolume));
            bgmVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(bgmVolume));
            sfxVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(sfxVolume));
        }

        /// <summary>
        /// 색각 보정 드롭다운에 현재 적용된 종류를 표시한다.
        /// </summary>
        /// <param name="correction">표시할 색각 보정 종류.</param>
        public void SetColorVisionCorrection(ColorVisionCorrection correction)
        {
            colorVisionCorrectionDropdown.SetValueWithoutNotify((int)correction);
            colorVisionCorrectionDropdown.RefreshShownValue();
        }

        /// <summary>
        /// 색상 심볼 표시 Toggle에 현재 적용된 상태를 표시한다.
        /// </summary>
        /// <param name="enabled">true면 색상 심볼 표시를 켠다.</param>
        public void SetSymbolsEnabled(bool enabled)
        {
            colorSymbolDisplayToggle.SetIsOnWithoutNotify(enabled);
        }

        /// <summary>
        /// RGB 다이어그램이 현재 접근성 표시 설정을 구독하도록 연결한다.
        /// </summary>
        /// <param name="settings">색각 보정 상태를 제공하는 전역 설정.</param>
        public void SetAccessibilityDisplaySettings(AccessibilityDisplaySettings settings)
        {
            rgbDiagram.SetAccessibilityDisplaySettings(settings);
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
            tween.Join(background.DOColor(backgroundVisibleColor, tweenDuration).SetEase(Ease.Linear));
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

        private async UniTask<bool> WaitForTweenAsync(Sequence tween, CancellationToken cancellationToken)
        {
            bool isCanceled = await tween.AsyncWaitForCompletion()
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

        private void HandleMasterVolumeChanged(float volume)
        {
            MasterVolumeChanged?.Invoke(volume);
        }

        private void HandleBgmVolumeChanged(float volume)
        {
            BgmVolumeChanged?.Invoke(volume);
        }

        private void HandleSfxVolumeChanged(float volume)
        {
            SfxVolumeChanged?.Invoke(volume);
        }

        private void HandleColorVisionCorrectionChanged(int value)
        {
            ColorVisionCorrectionChanged?.Invoke((ColorVisionCorrection)value);
        }

        private void HandleSymbolsEnabledChanged(bool enabled)
        {
            SymbolsEnabledChanged?.Invoke(enabled);
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

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(HandleBgmVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            }

            if (colorVisionCorrectionDropdown != null)
            {
                colorVisionCorrectionDropdown.onValueChanged.RemoveListener(HandleColorVisionCorrectionChanged);
            }

            if (colorSymbolDisplayToggle != null)
            {
                colorSymbolDisplayToggle.onValueChanged.RemoveListener(HandleSymbolsEnabledChanged);
            }

            transitionVersion++;
            StopActiveTween();
        }
    }
}
