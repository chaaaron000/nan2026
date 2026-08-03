using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 확산 계획을 거리별로 재생하고 각 wave 종료 시 셀의 최종 재질을 반영한다.
/// </summary>
public sealed class PaintSpreadSequencePlayer : IDisposable
{
    private readonly GridView gridView;
    private readonly PaintEffectLibrary effectLibrary;
    private readonly Func<ColorPaletteSO> paletteProvider;
    private readonly PaintEffectPool effectPool;
    private readonly List<GameObject> activeEffects = new();
    private bool cancellationRequested;

    /// <summary>재생 대상 GridView와 이펙트 설정을 연결한다.</summary>
    public PaintSpreadSequencePlayer(
        GridView gridView,
        PaintEffectLibrary effectLibrary,
        Func<ColorPaletteSO> paletteProvider)
    {
        this.gridView = gridView ?? throw new ArgumentNullException(nameof(gridView));
        this.effectLibrary = effectLibrary ?? throw new ArgumentNullException(nameof(effectLibrary));
        this.paletteProvider = paletteProvider ?? throw new ArgumentNullException(nameof(paletteProvider));
        effectPool = new PaintEffectPool(
            gridView.transform,
            effectLibrary.SortingLayer,
            effectLibrary.PlaybackSpeed,
            RecycleCompletedEffect);
    }

    /// <summary>
    /// 일반 물감은 거리순으로 확산하고, 지우기 물감은 범위 전체의 연출과 셀 변경을 동시에 시작한다.
    /// </summary>
    public IEnumerator Play(PaintApplicationPlan plan, PaintType paintType, Action completed)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        cancellationRequested = false;

        if (paintType == PaintType.Clear)
        {
            yield return PlayClear(plan);
            completed?.Invoke();
            yield break;
        }

        foreach (PaintSpreadWave wave in plan.Waves)
        {
            if (cancellationRequested)
            {
                break;
            }

            List<GameObject> waveEffects = new();
            SpawnWaveEffects(wave, waveEffects);

            if (waveEffects.Count > 0)
            {
                // 프리팹의 시스템 duration은 5초지만 실제 채움 파티클은 1.5초에 끝난다.
                // 보이지 않는 시스템 잔여 시간 때문에 다음 wave가 늦어지지 않도록
                // 지연 없이 시작하는 핵심 채움 파티클의 수명을 진행 기준으로 사용한다.
                float waveAdvanceSeconds = GetWaveAdvanceSeconds(waveEffects);
                float elapsed = 0f;
                while (!cancellationRequested
                       && elapsed < waveAdvanceSeconds)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (!cancellationRequested)
            {
                ApplyWaveResults(wave);
            }
        }

        if (cancellationRequested)
        {
            ApplyAllResults(plan);
        }

        completed?.Invoke();
    }

    private IEnumerator PlayClear(PaintApplicationPlan plan)
    {
        List<GameObject> clearEffects = new();
        GameObject clearPrefab = effectLibrary.ClearPrefab;

        if (clearPrefab != null)
        {
            float scale = gridView.CellSize / effectLibrary.ReferenceCellSize;
            foreach (PaintSpreadWave wave in plan.Waves)
            {
                foreach (PaintSpreadCellStep step in wave.Steps)
                {
                    GameObject effect = effectPool.Spawn(
                        clearPrefab,
                        gridView.GetCellLocalPosition(step.Position),
                        Quaternion.identity,
                        scale);
                    activeEffects.Add(effect);
                    clearEffects.Add(effect);
                }
            }
        }

        // 지우기 물감은 거리별 확산 없이 모든 대상 셀의 연출과 상태 변경을 같은 프레임에 시작한다.
        ApplyAllResults(plan);

        if (clearEffects.Count == 0)
        {
            yield break;
        }

        float duration = GetWaveAdvanceSeconds(clearEffects);
        float elapsed = 0f;
        while (!cancellationRequested && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>진행 중 연출을 중단하고 다음 프레임에 모든 결과 상태를 즉시 반영하도록 요청한다.</summary>
    public void CompleteImmediately()
    {
        cancellationRequested = true;
        RecycleActiveEffects();
    }

    /// <summary>풀에 보관된 모든 런타임 이펙트 오브젝트를 제거한다.</summary>
    public void Dispose()
    {
        RecycleActiveEffects();
        effectPool.Dispose();
    }

    private void SpawnWaveEffects(PaintSpreadWave wave, List<GameObject> waveEffects)
    {
        bool center = wave.Distance == 0;
        float scale = gridView.CellSize / effectLibrary.ReferenceCellSize;

        foreach (PaintSpreadCellStep step in wave.Steps)
        {
            GameObject prefab = effectLibrary.GetPrefab(step.ResultState, center);
            if (prefab == null)
            {
                continue;
            }

            Vector3 position = gridView.GetCellLocalPosition(step.Position);

            if (center)
            {
                GameObject effect = effectPool.Spawn(prefab, position, Quaternion.identity, scale);
                ApplyVisualSet(effect, step.ResultState);
                activeEffects.Add(effect);
                waveEffects.Add(effect);
                continue;
            }

            SpawnEdgeForDirection(prefab, position, scale, step.ResultState, step.IncomingDirections, PaintIncomingDirection.FromBelow, 0f, waveEffects);
            SpawnEdgeForDirection(prefab, position, scale, step.ResultState, step.IncomingDirections, PaintIncomingDirection.FromLeft, -90f, waveEffects);
            SpawnEdgeForDirection(prefab, position, scale, step.ResultState, step.IncomingDirections, PaintIncomingDirection.FromAbove, 180f, waveEffects);
            SpawnEdgeForDirection(prefab, position, scale, step.ResultState, step.IncomingDirections, PaintIncomingDirection.FromRight, 90f, waveEffects);
        }
    }

    private void ApplyVisualSet(GameObject effect, PaintState paintState)
    {
        ColorPaletteSO palette = paletteProvider();
        effectLibrary.ApplyVisualSet(effect, palette?.GetVisualSet(paintState));
    }

    private void SpawnEdgeForDirection(
        GameObject prefab,
        Vector3 position,
        float scale,
        PaintState paintState,
        PaintIncomingDirection directions,
        PaintIncomingDirection requiredDirection,
        float zRotation,
        List<GameObject> waveEffects)
    {
        if ((directions & requiredDirection) == 0)
        {
            return;
        }

        GameObject effect = effectPool.Spawn(
            prefab,
            position,
            Quaternion.Euler(0f, 0f, zRotation),
            scale);
        ApplyVisualSet(effect, paintState);
        activeEffects.Add(effect);
        waveEffects.Add(effect);
    }

    private float GetWaveAdvanceSeconds(List<GameObject> waveEffects)
    {
        float longestImmediateLifetime = 0f;

        foreach (GameObject effect in waveEffects)
        {
            if (effect == null)
            {
                continue;
            }

            ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                ParticleSystem.MainModule main = system.main;
                if (main.startDelay.constantMax <= 0.01f)
                {
                    longestImmediateLifetime = Mathf.Max(
                        longestImmediateLifetime,
                        main.startLifetime.constantMax
                        / Mathf.Max(main.simulationSpeed, 0.01f));
                }
            }
        }

        return Mathf.Max(
            longestImmediateLifetime * effectLibrary.WaveAdvanceLifetimeRatio,
            0.01f);
    }

    private void ApplyWaveResults(PaintSpreadWave wave)
    {
        foreach (PaintSpreadCellStep step in wave.Steps)
        {
            gridView.SetCellPaint(step.Position, step.ResultState);
        }
    }

    private void ApplyAllResults(PaintApplicationPlan plan)
    {
        foreach (PaintSpreadWave wave in plan.Waves)
        {
            ApplyWaveResults(wave);
        }
    }

    private void RecycleActiveEffects()
    {
        foreach (GameObject effect in activeEffects)
        {
            effectPool.Recycle(effect);
        }

        activeEffects.Clear();
    }

    private void RecycleCompletedEffect(GameObject effect)
    {
        activeEffects.Remove(effect);
        effectPool.Recycle(effect);
    }

    private sealed class PaintEffectPool : IDisposable
    {
        private readonly Transform parent;
        private readonly string sortingLayer;
        private readonly float playbackSpeed;
        private readonly Action<GameObject> completedRecycle;
        private readonly Dictionary<GameObject, Stack<GameObject>> inactiveByPrefab = new();
        private readonly Dictionary<GameObject, GameObject> prefabByInstance = new();
        private readonly Dictionary<GameObject, ParticleSystem.MinMaxCurve[]> originalRotationsByInstance = new();

        public PaintEffectPool(
            Transform parent,
            string sortingLayer,
            float playbackSpeed,
            Action<GameObject> completedRecycle)
        {
            this.parent = parent;
            this.sortingLayer = sortingLayer;
            this.playbackSpeed = playbackSpeed;
            this.completedRecycle = completedRecycle;
        }

        public GameObject Spawn(
            GameObject prefab,
            Vector3 localPosition,
            Quaternion localRotation,
            float uniformScale)
        {
            if (!inactiveByPrefab.TryGetValue(prefab, out Stack<GameObject> inactive))
            {
                inactive = new Stack<GameObject>();
                inactiveByPrefab.Add(prefab, inactive);
            }

            GameObject instance = inactive.Count > 0
                ? inactive.Pop()
                : UnityEngine.Object.Instantiate(prefab, parent, false);

            prefabByInstance[instance] = prefab;
            Transform effectTransform = instance.transform;
            effectTransform.SetParent(parent, false);
            effectTransform.localPosition = localPosition;
            // View 정렬 Billboard는 부모 Z 회전을 화면상 이미지 회전으로 안정적으로 반영하지 않는다.
            // 위치와 스케일만 루트에 적용하고 방향은 각 파티클의 startRotation으로 처리한다.
            effectTransform.localRotation = Quaternion.identity;
            effectTransform.localScale = Vector3.one * uniformScale;
            instance.SetActive(true);

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            if (!originalRotationsByInstance.TryGetValue(instance, out ParticleSystem.MinMaxCurve[] originalRotations))
            {
                originalRotations = new ParticleSystem.MinMaxCurve[systems.Length];
                for (int index = 0; index < systems.Length; index++)
                {
                    originalRotations[index] = systems[index].main.startRotation;
                }

                originalRotationsByInstance.Add(instance, originalRotations);
            }

            float billboardRotationOffset = -localRotation.eulerAngles.z * Mathf.Deg2Rad;
            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem system = systems[index];
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = system.main;
                main.loop = false;
                main.simulationSpeed = playbackSpeed;
                // 원본은 Local 모드라 루트 프리팹 스케일을 무시한다.
                // 셀 한 변에 맞춘 루트 스케일이 모든 자식 파티클에 적용되도록 변경한다.
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.startRotation = AddRotation(
                    originalRotations[index],
                    billboardRotationOffset);
            }

            ParticleSystemRenderer[] renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                renderer.sortingLayerName = sortingLayer;
            }

            foreach (ParticleSystem system in systems)
            {
                system.Play(false);
            }

            PaintEffectAutoRecycler autoRecycler =
                instance.GetComponent<PaintEffectAutoRecycler>();
            if (autoRecycler == null)
            {
                autoRecycler = instance.AddComponent<PaintEffectAutoRecycler>();
            }

            autoRecycler.Begin(completedRecycle);

            return instance;
        }

        private static ParticleSystem.MinMaxCurve AddRotation(
            ParticleSystem.MinMaxCurve original,
            float radians)
        {
            return original.mode switch
            {
                ParticleSystemCurveMode.Constant =>
                    new ParticleSystem.MinMaxCurve(original.constant + radians),

                ParticleSystemCurveMode.TwoConstants =>
                    new ParticleSystem.MinMaxCurve(
                        original.constantMin + radians,
                        original.constantMax + radians),

                // 현재 이펙트는 Constant 또는 TwoConstants만 사용한다.
                // 곡선 모드는 원본을 보존해 예기치 않은 곡선 변형을 피한다.
                _ => original,
            };
        }

        public void Recycle(GameObject instance)
        {
            if (instance == null || !prefabByInstance.TryGetValue(instance, out GameObject prefab))
            {
                return;
            }

            instance.GetComponent<PaintEffectAutoRecycler>()?.Cancel();

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            instance.SetActive(false);
            inactiveByPrefab[prefab].Push(instance);
        }

        public void Dispose()
        {
            foreach (GameObject instance in prefabByInstance.Keys)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }

            prefabByInstance.Clear();
            originalRotationsByInstance.Clear();
            inactiveByPrefab.Clear();
        }
    }
}
