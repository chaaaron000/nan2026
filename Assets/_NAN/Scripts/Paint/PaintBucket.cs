using System;
using UnityEngine;

/// <summary>
/// 물감통 한 개 단위 직렬화 가능한 클래스
/// </summary>
[Serializable]
public sealed class PaintBucket
{
    [SerializeField]
    private PaintType paintType;

    [SerializeField]
    [Min(1)]
    private int range = 1;

    public PaintType PaintType => paintType;
    public int Range => range;
}