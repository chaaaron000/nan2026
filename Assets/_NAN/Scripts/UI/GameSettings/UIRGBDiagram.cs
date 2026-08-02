using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 현재 색각 보정 팔레트로 RGB 혼합 관계를 표시한다.
    /// </summary>
    public sealed class UIRGBDiagram : MonoBehaviour
    {
        [SerializeField]
        private Image red;

        [SerializeField]
        private Image green;

        [SerializeField]
        private Image blue;

        [SerializeField]
        private Image yellow;

        [SerializeField]
        private Image cyan;

        [SerializeField]
        private Image magenta;

        [SerializeField]
        private Image white;

        private AccessibilityDisplaySettings displaySettings;
        private bool isDisplaySettingsSubscribed;

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
        /// 다이어그램이 구독할 전역 접근성 표시 설정을 지정한다.
        /// </summary>
        /// <param name="settings">색각 보정 팔레트를 제공하는 설정.</param>
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
            red.color = palette.GetColor(PaintState.Red);
            green.color = palette.GetColor(PaintState.Green);
            blue.color = palette.GetColor(PaintState.Blue);
            yellow.color = palette.GetColor(PaintState.Yellow);
            cyan.color = palette.GetColor(PaintState.Cyan);
            magenta.color = palette.GetColor(PaintState.Magenta);
            white.color = palette.GetColor(PaintState.White);
        }

        private void SubscribeDisplaySettings()
        {
            if (displaySettings == null || isDisplaySettingsSubscribed)
            {
                return;
            }

            displaySettings.PaletteChanged += HandlePaletteChanged;
            isDisplaySettingsSubscribed = true;
        }

        private void UnsubscribeDisplaySettings()
        {
            if (displaySettings == null || !isDisplaySettingsSubscribed)
            {
                return;
            }

            displaySettings.PaletteChanged -= HandlePaletteChanged;
            isDisplaySettingsSubscribed = false;
        }

        private void HandlePaletteChanged(ColorPaletteSO palette)
        {
            RefreshVisual();
        }
    }
}