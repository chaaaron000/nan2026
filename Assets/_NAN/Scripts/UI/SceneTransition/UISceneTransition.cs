using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace Nan.UI
{
    /// <summary>
    /// 씬 전환 화면의 페이드 연출과 사용자 입력 차단 상태를 관리합니다.
    /// </summary>
    public sealed class UISceneTransition : MonoBehaviour
    {
        [SerializeField]
        private GameObject raycastBlocker;

        [SerializeField]
        private UITransitionView transitionView;

        private Image raycastBlockerImage;

        /// <summary>
        /// 화면을 즉시 투명하게 만들고 사용자 입력 차단을 해제합니다.
        /// </summary>
        public void InitializeHidden()
        {
            transitionView.SetAlphaImmediately(0f);
            raycastBlocker.SetActive(true);
            SetInputBlocked(false);
        }

        /// <summary>
        /// 사용자 입력을 차단하고 화면을 완전히 가릴 때까지 기다립니다.
        /// </summary>
        /// <param name="cancellationToken">오브젝트 파괴 시 연출을 중단할 토큰입니다.</param>
        public async UniTask CoverAsync(CancellationToken cancellationToken)
        {
            SetInputBlocked(true);
            await transitionView.FadeToAsync(1f, cancellationToken);
        }

        /// <summary>
        /// 화면을 다시 표시하고 페이드가 끝난 뒤 사용자 입력 차단을 해제합니다.
        /// </summary>
        /// <param name="cancellationToken">오브젝트 파괴 시 연출을 중단할 토큰입니다.</param>
        public async UniTask RevealAsync(CancellationToken cancellationToken)
        {
            try
            {
                await transitionView.FadeToAsync(0f, cancellationToken);
            }
            finally
            {
                SetInputBlocked(false);
            }
        }

        private void SetInputBlocked(bool blocksInput)
        {
            if (raycastBlockerImage == null)
            {
                raycastBlockerImage = raycastBlocker.GetComponent<Image>();
            }

            if (raycastBlockerImage == null)
            {
                DebugConsole.LogError(
                    "[UISceneTransition] Raycast blocker has no Image component.",
                    this);
                return;
            }

            raycastBlockerImage.raycastTarget = blocksInput;
        }
    }
}
