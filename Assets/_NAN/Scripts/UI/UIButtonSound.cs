using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 연결된 UI 버튼의 마우스 오버와 클릭 입력에 공통 효과음을 재생한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSound : MonoBehaviour, IPointerEnterHandler
    {
        private Button button;

        /// <summary>
        /// 지정한 버튼에 공통 UI 효과음 동작을 한 번만 연결한다.
        /// </summary>
        /// <param name="targetButton">효과음을 연결할 UI 버튼.</param>
        public static void Attach(Button targetButton)
        {
            if (targetButton == null || targetButton.TryGetComponent(out UIButtonSound _))
            {
                return;
            }

            targetButton.gameObject.AddComponent<UIButtonSound>();
        }

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            ResolveButton().onClick.AddListener(PlayClickSound);
        }

        /// <summary>
        /// 상호작용 가능한 버튼 안으로 포인터가 들어오면 마우스 오버 효과음을 재생한다.
        /// </summary>
        /// <param name="eventData">현재 포인터 이벤트 정보.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            Button targetButton = ResolveButton();
            if (targetButton.IsActive() && targetButton.IsInteractable())
            {
                SoundManager.Instance?.PlaySfx(SoundKeys.UiButtonHover);
            }
        }

        private Button ResolveButton()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            return button;
        }

        private void PlayClickSound()
        {
            SoundManager.Instance?.PlaySfx(SoundKeys.UiButtonClick);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
            }
        }
    }
}
