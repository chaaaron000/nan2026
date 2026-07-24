using System.Collections.Generic;
using UnityEngine;

using System;

/// <summary>
/// 스테이지의 격자 크기, 물감통, 벽과 정답 셀 상태를 보관한다.
/// </summary>
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

    // 행 우선 방식으로 저장되는 각 셀의 목표 물감 상태
    [SerializeField]
    private PaintState[] answerPaintStates = Array.Empty<PaintState>();

    // 이전 정답 배열의 격자 크기다. 크기 변경 시 같은 좌표의 정답을 보존하는 데 사용한다.
    [SerializeField, HideInInspector]
    private int answerWidth;

    [SerializeField, HideInInspector]
    private int answerHeight;

    /// <summary>
    /// 스테이지 격자의 가로 셀 수를 반환한다.
    /// </summary>
    public int Width => width;

    /// <summary>
    /// 스테이지 격자의 세로 셀 수를 반환한다.
    /// </summary>
    public int Height => height;

    /// <summary>
    /// 스테이지에서 사용할 물감통 목록을 반환한다.
    /// </summary>
    public IReadOnlyList<PaintBucket> PaintBuckets => paintBuckets;

    /// <summary>
    /// 셀 좌표를 2배로 확장한 좌표계에서
    /// 셀 사이에 배치된 벽 좌표 목록을 반환한다.
    /// </summary>
    public IReadOnlyList<Vector2Int> WallPositions => wallPositions;

    /// <summary>
    /// 행 우선 방식으로 저장된 각 셀의 목표 물감 상태를 반환한다.
    /// </summary>
    public IReadOnlyList<PaintState> AnswerPaintStates => answerPaintStates;

    /// <summary>
    /// 전달받은 셀 상태 배열을 현재 격자 크기의 정답으로 설정한다.
    /// </summary>
    /// <param name="paintStates">행 우선 방식으로 정렬된 정답 셀 상태 목록.</param>
    public void SetAnswerPaintStates(IReadOnlyList<PaintState> paintStates)
    {
        if (paintStates == null)
        {
            throw new ArgumentNullException(nameof(paintStates));
        }

        int cellCount = width * height;

        if (paintStates.Count != cellCount)
        {
            throw new ArgumentException(
                $"Answer paint count must be {cellCount}, but was {paintStates.Count}.",
                nameof(paintStates));
        }

        answerPaintStates = new PaintState[cellCount];

        for (int index = 0; index < cellCount; index++)
        {
            if (!IsValidPaintState(paintStates[index]))
            {
                throw new ArgumentException(
                    $"Answer paint state at index {index} is invalid.",
                    nameof(paintStates));
            }

            answerPaintStates[index] = paintStates[index];
        }

        answerWidth = width;
        answerHeight = height;
    }

    private void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        ResizeAnswerPaintStates();

        HashSet<Vector2Int> validWalls = new();

        // 격자 크기가 줄어들었거나 에셋을 직접 편집한 경우에도
        // 잘못된 좌표와 중복 벽이 런타임으로 전달되지 않게 정리한다.
        wallPositions.RemoveAll(position =>
            !IsValidWallPosition(position) || !validWalls.Add(position)
        );
    }

    private void ResizeAnswerPaintStates()
    {
        int previousWidth = answerWidth > 0
            ? answerWidth
            : width;
        int previousHeight = answerHeight > 0
            ? answerHeight
            : height;

        int cellCount = width * height;
        bool hasExpectedSize = answerPaintStates != null
                               && answerPaintStates.Length == cellCount
                               && previousWidth == width
                               && previousHeight == height;

        if (hasExpectedSize)
        {
            NormalizeAnswerPaintStates(answerPaintStates);
            answerWidth = width;
            answerHeight = height;
            return;
        }

        PaintState[] resizedPaintStates = new PaintState[cellCount];

        if (answerPaintStates != null)
        {
            int copyWidth = Mathf.Min(previousWidth, width);
            int copyHeight = Mathf.Min(previousHeight, height);

            // 격자 크기가 바뀌어도 두 격자에 공통으로 존재하는 좌표의 정답만 보존한다.
            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    int sourceIndex = y * previousWidth + x;

                    if (sourceIndex >= answerPaintStates.Length)
                    {
                        continue;
                    }

                    int destinationIndex = y * width + x;
                    PaintState paintState = answerPaintStates[sourceIndex];

                    resizedPaintStates[destinationIndex] =
                        IsValidPaintState(paintState)
                            ? paintState
                            : PaintState.Empty;
                }
            }
        }

        answerPaintStates = resizedPaintStates;
        answerWidth = width;
        answerHeight = height;
    }

    private static void NormalizeAnswerPaintStates(PaintState[] paintStates)
    {
        for (int index = 0; index < paintStates.Length; index++)
        {
            if (!IsValidPaintState(paintStates[index]))
            {
                paintStates[index] = PaintState.Empty;
            }
        }
    }

    private static bool IsValidPaintState(PaintState paintState)
    {
        int validBits = (int)PaintState.White;

        return ((int)paintState & ~validBits) == 0;
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
