using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 현재 스테이지를 유지한 채 타이틀의 스테이지 선택 화면으로 돌아갑니다.
    /// </summary>
    public sealed class HUDToTitleButton : MonoBehaviour
    {
        private const string TitleSceneName = "Title";

        [SerializeField]
        private Button button;

        private void Awake()
        {
            if (button == null)
            {
                DebugConsole.LogError("[HUDToTitleButton] Button is null.", this);
                enabled = false;
                return;
            }

            button.onClick.AddListener(HandleButtonClicked);
            UIButtonSound.Attach(button);
        }

        private void HandleButtonClicked()
        {
            if (SceneTransitionManager.Instance.LoadScene(TitleSceneName))
            {
                StageRunContext.Instance.RequestReturnToStageSelection();
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleButtonClicked);
            }
        }
    }
}
