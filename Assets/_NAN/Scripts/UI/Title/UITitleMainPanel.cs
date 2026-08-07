using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 타이틀 메인 패널의 버튼 입력을 처리합니다.
    /// </summary>
    public sealed class UITitleMainPanel : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField]
        private Button startButton;

        [SerializeField]
        private Button settingsButton;

        private TitleUIController titleUIController;

        private void Awake()
        {
            titleUIController = GetComponentInParent<TitleUIController>();
            if (titleUIController == null)
            {
                DebugConsole.LogError("[UITitleMainPanel] TitleUIController was not found in parents", this);
                enabled = false;
                return;
            }

            if (startButton == null || settingsButton == null)
            {
                DebugConsole.LogError("[UITitleMainPanel] Required button is null", this);
                enabled = false;
                return;
            }

            startButton.onClick.AddListener(HandleStartButtonClicked);
            settingsButton.onClick.AddListener(HandleSettingsButtonClicked);
            UIButtonSound.Attach(startButton);
            UIButtonSound.Attach(settingsButton);
        }

        private void HandleStartButtonClicked()
        {
            titleUIController.ChangePanel(TitleUIController.PanelType.STAGE_SELECTION);
        }

        private void HandleSettingsButtonClicked()
        {
            GameSettingsService.Instance.ShowSettings();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartButtonClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsButtonClicked);
            }
        }
    }
}
