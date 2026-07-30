using System;
using UnityEngine;

/// <summary>
/// 이펙트에 포함된 모든 파티클 재생이 끝나면 해당 인스턴스를 풀로 반환한다.
/// </summary>
public sealed class PaintEffectAutoRecycler : MonoBehaviour
{
    private ParticleSystem[] particleSystems;
    private Action<GameObject> recycle;

    /// <summary>
    /// 현재 재생 중인 파티클을 감시하고 모두 끝났을 때 실행할 반환 동작을 등록한다.
    /// </summary>
    public void Begin(Action<GameObject> recycleAction)
    {
        particleSystems ??= GetComponentsInChildren<ParticleSystem>(true);
        recycle = recycleAction ?? throw new ArgumentNullException(nameof(recycleAction));
    }

    /// <summary>
    /// 명시적으로 회수되는 인스턴스의 자동 반환 예약을 취소한다.
    /// </summary>
    public void Cancel()
    {
        recycle = null;
    }

    private void LateUpdate()
    {
        if (recycle == null || IsAnyParticleAlive())
        {
            return;
        }

        Action<GameObject> recycleAction = recycle;
        recycle = null;
        recycleAction(gameObject);
    }

    private bool IsAnyParticleAlive()
    {
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem != null && particleSystem.IsAlive(true))
            {
                return true;
            }
        }

        return false;
    }
}
