using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "NaN/Stage Data")]
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

    // 셀 좌표를 2배로 확장한 좌표계에서
    // 셀 사이에 배치된 벽의 위치
    [SerializeField]
    private List<Vector2Int> wallPositions = new();

    public int Width => width;
    public int Height => height;

    public IReadOnlyList<PaintBucket> PaintBuckets => paintBuckets;

    /// <summary>
    /// 셀 좌표를 2배로 확장한 좌표계에서
    /// 셀 사이에 배치된 벽 좌표 목록을 반환한다.
    /// </summary>
    public IReadOnlyList<Vector2Int> WallPositions => wallPositions;

    private void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        HashSet<Vector2Int> validWalls = new();

        // 격자 크기가 줄어들었거나 에셋을 직접 편집한 경우에도
        // 잘못된 좌표와 중복 벽이 런타임으로 전달되지 않게 정리한다.
        wallPositions.RemoveAll(position =>
            !IsValidWallPosition(position) || !validWalls.Add(position)
        );
    }

    private bool IsValidWallPosition(Vector2Int position)
    {
        int maxX = (width - 1) * 2;
        int maxY = (height - 1) * 2;

        if (position.x < 0 || position.x > maxX || position.y < 0 || position.y > maxY)
        {
            return false;
        }

        bool xIsOdd = position.x % 2 != 0;
        bool yIsOdd = position.y % 2 != 0;

        // 셀 중심(짝수, 짝수)과
        // 셀 모서리(홀수, 홀수)는 벽 좌표가 아니다.
        return xIsOdd != yIsOdd;
    }
}
