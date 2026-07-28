using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Nan.UI
{
    /// <summary>
    /// 화면 전환 연출을 재생하는 클래스. 현재는 임시로 Image 페이드 인아웃
    /// </summary>
    public sealed class UITransitionView : MonoBehaviour
    {
        [SerializeField]
        private Image image;

        [SerializeField]
        [Min(0f)]
        private float coverDuration = 0.3f;

        [SerializeField]
        [Min(0f)]
        private float revealDuration = 0.5f;

        private Tween activeTween;

        /// <summary>
        /// 전환 이미지의 투명도를 연출 없이 즉시 설정합니다.
        /// </summary>
        /// <param name="alpha">0부터 1 사이의 목표 투명도입니다.</param>
        public void SetAlphaImmediately(float alpha)
        {
            activeTween?.Kill();
            activeTween = null;

            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        /// <summary>
        /// 전환 이미지의 투명도를 실시간 기준 선형 페이드로 변경합니다.
        /// </summary>
        /// <param name="alpha">0부터 1 사이의 목표 투명도입니다.</param>
        /// <param name="cancellationToken">오브젝트 파괴 시 연출을 중단할 토큰입니다.</param>
        public async UniTask FadeToAsync(float alpha, CancellationToken cancellationToken)
        {
            activeTween?.Kill();

            float targetAlpha = Mathf.Clamp01(alpha);
            bool isCovering = targetAlpha > image.color.a;
            float duration = isCovering ? coverDuration : revealDuration;
            Ease ease = isCovering ? Ease.Linear : Ease.OutQuad;

            Tween tween = image.DOFade(targetAlpha, duration).SetEase(ease).SetUpdate(true);
            activeTween = tween;

            try
            {
                await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tween.Kill();
                }

                if (activeTween == tween)
                {
                    activeTween = null;
                }
            }
        }

        private void OnDisable()
        {
            activeTween?.Kill();
            activeTween = null;
        }
    }
}
