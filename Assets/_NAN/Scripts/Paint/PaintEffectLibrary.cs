using System;
using UnityEngine;

/// <summary>
/// 최종 물감 상태별 center/edge 이펙트 프리팹과 배치 설정을 제공한다.
/// </summary>
[Serializable]
public sealed class PaintEffectLibrary
{
    [SerializeField] private GameObject redCenter;
    [SerializeField] private GameObject redEdge;
    [SerializeField] private GameObject greenCenter;
    [SerializeField] private GameObject greenEdge;
    [SerializeField] private GameObject blueCenter;
    [SerializeField] private GameObject blueEdge;
    [SerializeField] private GameObject yellowCenter;
    [SerializeField] private GameObject yellowEdge;
    [SerializeField] private GameObject cyanCenter;
    [SerializeField] private GameObject cyanEdge;
    [SerializeField] private GameObject magentaCenter;
    [SerializeField] private GameObject magentaEdge;
    [SerializeField] private GameObject whiteCenter;
    [SerializeField] private GameObject whiteEdge;
    [SerializeField] private GameObject clear;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip("이펙트 프리팹의 채움 파티클이 차지하는 기준 한 변의 월드 크기")]
    private float referenceCellSize = 1.704f;

    [SerializeField]
    private string sortingLayer = "GridPaintEffect";

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("페인트 파티클과 거리별 확산 연출의 재생 배속")]
    private float playbackSpeed = 1.5f;

    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip("즉시 시작하는 물감 퍼짐 파티클 수명 중 다음 거리 wave로 넘어가기 전에 기다릴 비율")]
    private float waveAdvanceLifetimeRatio = 0.7f;

    /// <summary>현재 GridView 크기에 맞출 때 사용할 이펙트 제작 기준 셀 크기를 반환한다.</summary>
    public float ReferenceCellSize => Mathf.Max(referenceCellSize, 0.01f);

    /// <summary>파티클 렌더러에 적용할 Sorting Layer 이름을 반환한다.</summary>
    public string SortingLayer => string.IsNullOrWhiteSpace(sortingLayer)
        ? "GridPaintEffect"
        : sortingLayer;

    /// <summary>파티클 시뮬레이션과 wave 진행에 함께 적용할 재생 배속을 반환한다.</summary>
    public float PlaybackSpeed => Mathf.Max(playbackSpeed, 0.1f);

    /// <summary>물감 퍼짐 이펙트 수명 대비 다음 거리 wave를 시작할 시점 비율을 반환한다.</summary>
    public float WaveAdvanceLifetimeRatio => Mathf.Clamp(waveAdvanceLifetimeRatio, 0.1f, 1f);

    /// <summary>범위 지우기 연출에 사용할 프리팹을 반환한다.</summary>
    public GameObject ClearPrefab => clear;

    /// <summary>최종 물감 상태와 연출 종류에 맞는 프리팹을 반환한다.</summary>
    public GameObject GetPrefab(PaintState paintState, bool center)
    {
        return paintState switch
        {
            PaintState.Red => center ? redCenter : redEdge,
            PaintState.Green => center ? greenCenter : greenEdge,
            PaintState.Blue => center ? blueCenter : blueEdge,
            PaintState.Yellow => center ? yellowCenter : yellowEdge,
            PaintState.Cyan => center ? cyanCenter : cyanEdge,
            PaintState.Magenta => center ? magentaCenter : magentaEdge,
            PaintState.White => center ? whiteCenter : whiteEdge,
            PaintState.Empty => null,
            _ => throw new ArgumentOutOfRangeException(nameof(paintState), paintState, "Unsupported paint state."),
        };
    }

    /// <summary>
    /// 생성된 이펙트 인스턴스의 역할별 Renderer에 VisualSet Material을 적용한다.
    /// </summary>
    /// <param name="effectInstance">Material을 교체할 이펙트 인스턴스.</param>
    /// <param name="visualSet">현재 페인트 상태의 시각 리소스 묶음.</param>
    public void ApplyVisualSet(GameObject effectInstance, PaintVisualSet visualSet)
    {
        if (effectInstance == null || visualSet == null)
        {
            return;
        }

        foreach (Renderer renderer in effectInstance.GetComponentsInChildren<Renderer>(true))
        {
            PaintEffectMaterialType? materialType = GetMaterialType(renderer.gameObject.name);
            if (!materialType.HasValue)
            {
                continue;
            }

            Material material = visualSet.GetEffectMaterial(materialType.Value);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }

    private static PaintEffectMaterialType? GetMaterialType(string objectName)
    {
        return objectName switch
        {
            "paint" => PaintEffectMaterialType.Center,
            "Paint" => PaintEffectMaterialType.Edge,
            "bubble" => PaintEffectMaterialType.Bubble,
            "glow" => PaintEffectMaterialType.Glow,
            "glowSub" => PaintEffectMaterialType.GlowSub,
            _ => null,
        };
    }
}
