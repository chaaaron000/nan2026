using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물감통들의 스프라이트 종류를 담고있는 클래스, paintbucketgenerator가 가지게 된다.
/// </summary>
[CreateAssetMenu(
    fileName = "PaintBucketVisualData",
    menuName = "NaN/Paint Bucket Visual Data")]
public sealed class PaintBucketVisualData
    : ScriptableObject
{
    [Serializable]
    private struct BucketPaintMaterialEntry
    {
        [SerializeField]
        private PaintType paintType;

        [SerializeField]
        private Color paletteColor;

        [SerializeField]
        private Material material;

        public Material Material => material;

        public bool Matches(
            PaintType targetPaintType,
            Color targetPaletteColor)
        {
            return paintType == targetPaintType
                   && Approximately(paletteColor, targetPaletteColor);
        }

        private static bool Approximately(Color left, Color right)
        {
            const float Tolerance = 0.0001f;

            return Mathf.Abs(left.r - right.r) <= Tolerance
                   && Mathf.Abs(left.g - right.g) <= Tolerance
                   && Mathf.Abs(left.b - right.b) <= Tolerance
                   && Mathf.Abs(left.a - right.a) <= Tolerance;
        }
    }

    [SerializeField]
    private Sprite redSprite;

    [SerializeField]
    private Sprite greenSprite;

    [SerializeField]
    private Sprite blueSprite;

    [SerializeField]
    private Sprite clearSprite;

    [Header("Paint Bucket Prefabs")]
    [SerializeField]
    private GameObject redPrefab;

    [SerializeField]
    private GameObject greenPrefab;

    [SerializeField]
    private GameObject bluePrefab;

    [SerializeField]
    private GameObject clearPrefab;

    [Header("Paint Bucket Materials")]
    [SerializeField]
    private List<BucketPaintMaterialEntry> bucketPaintMaterials = new();

    /// <summary>
    /// 색깔 별 물감통 스프라이트를 반환
    /// </summary>
    public Sprite GetSprite(PaintType paintType)
    {
        return paintType switch
        {
            PaintType.Red => redSprite,
            PaintType.Green => greenSprite,
            PaintType.Blue => blueSprite,
            PaintType.Clear => clearSprite,

            _ => throw new ArgumentOutOfRangeException(
                nameof(paintType),
                paintType,
                null)
        };
    }

    /// <summary>
    /// 지정한 물감 종류에 대응하는 월드 물감통 프리팹을 반환한다.
    /// </summary>
    /// <param name="paintType">조회할 물감 종류.</param>
    /// <returns>색상별 물감통 프리팹. 등록되지 않았으면 null을 반환한다.</returns>
    public GameObject GetPrefab(PaintType paintType)
    {
        return paintType switch
        {
            PaintType.Red => redPrefab,
            PaintType.Green => greenPrefab,
            PaintType.Blue => bluePrefab,
            PaintType.Clear => clearPrefab,

            _ => throw new ArgumentOutOfRangeException(
                nameof(paintType),
                paintType,
                null)
        };
    }

    /// <summary>
    /// 현재 팔레트 색상과 물감 종류에 맞는 물감통 전용 Material을 반환한다.
    /// </summary>
    /// <param name="paintType">조회할 물감 종류.</param>
    /// <param name="paletteColor">현재 접근성 팔레트에서 지정한 물감 색상.</param>
    /// <returns>일치하는 물감통 Material. 등록되지 않았으면 null을 반환한다.</returns>
    public Material GetBucketPaintMaterial(
        PaintType paintType,
        Color paletteColor)
    {
        foreach (BucketPaintMaterialEntry entry in bucketPaintMaterials)
        {
            if (entry.Matches(paintType, paletteColor))
            {
                return entry.Material;
            }
        }

        return null;
    }
}
