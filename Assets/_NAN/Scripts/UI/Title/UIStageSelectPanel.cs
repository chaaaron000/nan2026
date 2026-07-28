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
        [SerializeField]
        private Button backToTitleButton;

        private TitleUIController titleUIController;

        private void Awake()
        {
            if (backToTitleButton == null)
            {
                DebugConsole.LogError("[UIStageSelectPanel] Back button is null", this);
                enabled = false;
                return;
            }

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

            backToTitleButton.onClick.AddListener(HandleBackToTitleButtonClicked);
        }

        private void HandleBackToTitleButtonClicked()
        {
            titleUIController.ChangePanel(TitleUIController.PanelType.MAIN);
        }

        private void OnDestroy()
        {
            if (backToTitleButton != null)
            {
                backToTitleButton.onClick.RemoveListener(HandleBackToTitleButtonClicked);
            }
        }
    }
}
