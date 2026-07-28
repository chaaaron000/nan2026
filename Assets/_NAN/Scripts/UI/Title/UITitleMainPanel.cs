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

        [SerializeField]
        private Button quitButton;

        private TitleUIController titleUIController;

        private void Awake()
        {
            if (startButton == null || settingsButton == null || quitButton == null)
            {
                DebugConsole.LogError("[UITitleMainPanel] Required button is null", this);
                enabled = false;
                return;
            }

            titleUIController = GetComponentInParent<TitleUIController>();
            if (titleUIController == null)
            {
                DebugConsole.LogError(
                    "[UITitleMainPanel] TitleUIController was not found in parents",
                    this
                );
                enabled = false;
                return;
            }

            startButton.onClick.AddListener(HandleStartButtonClicked);
            settingsButton.onClick.AddListener(HandleSettingsButtonClicked);
            quitButton.onClick.AddListener(HandleQuitButtonClicked);
        }

        private void HandleStartButtonClicked()
        {
            titleUIController.ChangePanel(TitleUIController.PanelType.STAGE_SELECTION);
        }

        private void HandleQuitButtonClicked()
        {
            Application.Quit();
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

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitButtonClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsButtonClicked);
            }
        }
    }
}
