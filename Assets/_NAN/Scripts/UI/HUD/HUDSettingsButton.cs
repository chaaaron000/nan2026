using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 스테이지 HUD의 설정 버튼 입력과 공통 UI 효과음을 연결한다.
    /// </summary>
    public class HUDSettingsButton : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        private void Awake()
        {
            button.onClick.AddListener(GameSettingsService.Instance.ShowSettings);
            UIButtonSound.Attach(button);
        }
    }
}
