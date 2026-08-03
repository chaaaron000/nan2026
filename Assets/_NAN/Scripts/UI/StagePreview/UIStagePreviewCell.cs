using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 스테이지 미리보기에서 물감 상태 하나를 UGUI 색상과 심볼로 표시한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    [DisallowMultipleComponent]
    public sealed class UIStagePreviewCell : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text symbolText;

        private Image backgroundImage;
        private PaintState paintState;

        /// <summary>
        /// 셀의 배치에 사용하는 RectTransform을 반환한다.
        /// </summary>
        public RectTransform RectTransform => (RectTransform)transform;

        private void Awake()
        {
            backgroundImage = GetComponent<Image>();
            backgroundImage.raycastTarget = false;
        }

        /// <summary>
        /// 셀이 보관하고 표시할 물감 상태를 설정한다.
        /// </summary>
        /// <param name="state">표시할 물감 상태.</param>
        public void SetPaintState(PaintState state)
        {
            paintState = state;
        }

        /// <summary>
        /// 현재 물감 상태를 지정한 접근성 표시 설정으로 갱신한다.
        /// </summary>
        /// <param name="settings">색상 팔레트와 심볼 표시 여부를 제공하는 설정.</param>
        public void RefreshVisual(AccessibilityDisplaySettings settings)
        {
            ColorPaletteSO palette = settings.ActivePalette;
            Material targetMaterial = palette.GetVisualSet(paintState)?.CellMaterial;

            // GridView와 동일하게 빈 셀은 격자 배경만 보이게 하고,
            // 물감이 칠해진 셀만 셀 아트와 물감 머티리얼을 표시한다.
            backgroundImage.enabled = paintState != PaintState.Empty || targetMaterial != null;
            backgroundImage.material = targetMaterial;
            backgroundImage.color = targetMaterial == null
                ? palette.GetColor(paintState)
                : Color.white;

            bool shouldShowSymbol = settings.SymbolsEnabled && paintState != PaintState.Empty;
            symbolText.gameObject.SetActive(shouldShowSymbol);

            if (!shouldShowSymbol)
            {
                return;
            }

            symbolText.text = PaintStateVisualUtility.GetSymbol(paintState);
            symbolText.color = palette.GetSymbolColor(paintState);
        }

    }
}
