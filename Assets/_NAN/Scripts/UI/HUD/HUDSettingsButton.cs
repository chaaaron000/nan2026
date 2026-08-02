using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nan.UI
{
    public class HUDSettingsButton : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        private void Awake()
        {
            button.onClick.AddListener(GameSettingsService.Instance.ShowSettings);
        }
    }
}