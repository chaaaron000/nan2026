using System;
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
    [SerializeField]
    private Sprite redSprite;

    [SerializeField]
    private Sprite greenSprite;

    [SerializeField]
    private Sprite blueSprite;

    [SerializeField]
    private Sprite clearSprite;

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
}