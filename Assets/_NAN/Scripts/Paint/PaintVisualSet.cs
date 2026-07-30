using System;
using UnityEngine;

/// <summary>
/// 페인트 이펙트가 사용하는 Material 종류를 식별한다.
/// </summary>
public enum PaintEffectMaterialType
{
    /// <summary>중앙 페인트 Material.</summary>
    Center,

    /// <summary>외곽 페인트 Material.</summary>
    Edge,

    /// <summary>기포 Material.</summary>
    Bubble,

    /// <summary>빛 번짐 Material.</summary>
    Glow,

    /// <summary>보조 빛 번짐 Material.</summary>
    GlowSub,
}

/// <summary>
/// 하나의 PaintState에 필요한 셀과 페인트 이펙트 Material 묶음이다.
/// </summary>
[CreateAssetMenu(
    fileName = "PaintVisualSet",
    menuName = "NaN/Paint Visual Set")]
public sealed class PaintVisualSet : ScriptableObject
{
    [SerializeField]
    private Material cellMaterial;

    [Header("페인트 이펙트 Material")]
    [SerializeField]
    private Material centerMaterial;

    [SerializeField]
    private Material edgeMaterial;

    [SerializeField]
    private Material bubbleMaterial;

    [SerializeField]
    private Material glowMaterial;

    [SerializeField]
    private Material glowSubMaterial;

    /// <summary>
    /// 셀에 표시할 Material을 반환한다.
    /// </summary>
    public Material CellMaterial => cellMaterial;

    /// <summary>
    /// 지정한 이펙트 종류에 해당하는 Material을 반환한다.
    /// </summary>
    /// <param name="materialType">조회할 이펙트 Material 종류.</param>
    /// <returns>해당 종류에 등록된 Material.</returns>
    public Material GetEffectMaterial(PaintEffectMaterialType materialType)
    {
        return materialType switch
        {
            PaintEffectMaterialType.Center => centerMaterial,
            PaintEffectMaterialType.Edge => edgeMaterial,
            PaintEffectMaterialType.Bubble => bubbleMaterial,
            PaintEffectMaterialType.Glow => glowMaterial,
            PaintEffectMaterialType.GlowSub => glowSubMaterial,
            _ => throw new ArgumentOutOfRangeException(
                nameof(materialType),
                materialType,
                "Unsupported paint effect material type."),
        };
    }
}
