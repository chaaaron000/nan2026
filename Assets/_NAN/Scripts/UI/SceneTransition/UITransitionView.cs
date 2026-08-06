using System;
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

        private void OnDisable()
        {
            activeTween?.Kill();
            activeTween = null;
        }

        /// <summary>
        /// 전환 이미지의 투명도를 연출 없이 즉시 설정합니다.
        /// </summary>
        /// <param name="alpha">0부터 1 사이의 목표 투명도입니다.</param>
        public void SetAlphaImmediately(float alpha)
        {
            activeTween?.Kill();
            activeTween = null;
            
            image.material.SetFloat("_Progress", 1 - alpha);

            // Color color = image.color;
            // color.a = Mathf.Clamp01(alpha);
            // image.color = color;
        }

        /// <summary>
        /// 전환 이미지의 투명도를 실시간 기준 선형 페이드로 변경합니다.
        /// </summary>
        /// <param name="alpha">0부터 1 사이의 목표 투명도입니다.</param>
        /// <param name="cancellationToken">오브젝트 파괴 시 연출을 중단할 토큰입니다.</param>
        public async UniTask FadeToAsync(float alpha, CancellationToken cancellationToken)
        {
            activeTween?.Kill();
            image.gameObject.SetActive(true);

            float targetAlpha = Mathf.Clamp01(alpha);
            Material material = image.material;
            float targetProgress = 1f - targetAlpha;
            bool isCovering = targetProgress < material.GetFloat("_Progress");
            float duration = isCovering ? coverDuration : revealDuration;
            Ease ease = isCovering ? Ease.Linear : Ease.OutQuad;

            Tween tween = material.DOFloat(targetProgress, "_Progress", duration).SetEase(ease).SetUpdate(true);
            activeTween = tween;

            try
            {
                await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(cancellationToken);

                // 완전히 투명해진 전환 이미지는 렌더 대상에서 제거해 다음 UI를 가리지 않습니다.
                if (activeTween == tween && targetAlpha <= 0f)
                {
                    image.gameObject.SetActive(false);
                }
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
    }
}
