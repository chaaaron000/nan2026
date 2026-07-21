using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageData",
    menuName = "NaN/Stage Data")]
public sealed class StageData : ScriptableObject
{
    //격자 가로, 세로
    [SerializeField]
    private int width = 5;

    [SerializeField]
    private int height = 5;

    //스테이지에 들어가는 물감통
    [SerializeField]
    private List<PaintBucket> paintBuckets = new();

    public int Width => width;
    public int Height => height;

    public IReadOnlyList<PaintBucket> PaintBuckets =>
        paintBuckets;
}