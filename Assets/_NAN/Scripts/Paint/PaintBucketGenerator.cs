using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지에서 제공하는 물감통 데이터를 바탕으로
/// 물감통 GameObject를 생성하는 클래스
/// </summary>
public sealed class PaintBucketGenerator : MonoBehaviour
{
    // 물감통 프리팹
    [SerializeField]
    private PaintBucketView paintBucketPrefab;

    // 생성된 물감통들을 배치할 부모 Transform
    [SerializeField]
    private Transform paintBucketParent;

    [SerializeField]
    private Transform paintBucketVisualParent;

    // 색깔 별 물감통 Sprite를 제공하는 시각 데이터
    [SerializeField]
    private PaintBucketVisualData visualData;

    [SerializeField]
    private AccessibilityDisplaySettings accessibilityDisplaySettings;

    private const int MaxSingleRowBucketCount = 5;
    private const int BucketLayoutPaddingHorizontal = 20;
    private static readonly Vector2 BucketLayoutCellSize = new(126f, 96f);
    private static readonly Vector2 BucketLayoutSpacing = new(12f, 20f);
    

    /// <summary>
    /// 전달받은 물감통 데이터마다 물감통 프리팹을 하나씩 생성한다.
    /// </summary>
    public IReadOnlyList<PaintBucketView> Generate(
        IReadOnlyList<PaintBucket> bucketData)
    {
        if (bucketData == null)
        {
            throw new ArgumentNullException(
                nameof(bucketData));
        }

        PaintBucketView[] generatedViews =
            new PaintBucketView[bucketData.Count];

        for (int index = 0;
             index < bucketData.Count;
             index++)
        {
            generatedViews[index] =
                GenerateBucket(bucketData[index]);
        }

        ConfigureBucketLayout(bucketData.Count);

        //완성된 view list를 반환
        return generatedViews;
    }
    
    /// <summary>
    /// 물감통 하나를 생성하는 함수
    /// </summary>
    private PaintBucketView GenerateBucket(
        PaintBucket bucket)
    {
        if (bucket == null)
        {
            throw new ArgumentNullException(
                nameof(bucket));
        }

        PaintBucketView bucketView =
            Instantiate(
                paintBucketPrefab,
                paintBucketParent);

        //visualData에 있는 스프라이트를 바탕으로 bucketview 초기화 호출
        Sprite bucketSprite =
            visualData.GetSprite(bucket.PaintType);

        bucketView.Initialize(
            bucket.Range,
            bucketSprite,
            bucket.PaintType);
        bucketView.SetVisualData(visualData);
        bucketView.SetVisualPrefab(
            visualData.GetPrefab(bucket.PaintType),
            paintBucketVisualParent);
        bucketView.SetAccessibilityDisplaySettings(
            GetAccessibilityDisplaySettings());

        return bucketView;
    }

    private void ConfigureBucketLayout(int bucketCount)
    {
        if (paintBucketParent == null)
        {
            return;
        }

        GridLayoutGroup gridLayoutGroup =
            paintBucketParent.GetComponent<GridLayoutGroup>();

        if (gridLayoutGroup == null)
        {
            DebugConsole.LogError(
                "PaintBucketParent requires a GridLayoutGroup.",
                paintBucketParent);
            return;
        }

        int rowCount = bucketCount > MaxSingleRowBucketCount ? 2 : 1;
        int columnCount = Mathf.Max(
            1,
            Mathf.CeilToInt(bucketCount / (float)rowCount));

        gridLayoutGroup.enabled = true;
        gridLayoutGroup.padding = new RectOffset(
            BucketLayoutPaddingHorizontal,
            BucketLayoutPaddingHorizontal,
            0,
            0);
        gridLayoutGroup.cellSize = CalculateBucketCellSize(
            columnCount,
            rowCount);
        gridLayoutGroup.spacing = BucketLayoutSpacing;
        gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        gridLayoutGroup.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = columnCount;
    }

    private Vector2 CalculateBucketCellSize(
        int columnCount,
        int rowCount)
    {
        RectTransform parentRectTransform =
            paintBucketParent as RectTransform;

        if (parentRectTransform == null)
        {
            return BucketLayoutCellSize;
        }

        Rect parentRect = parentRectTransform.rect;
        float availableWidth =
            parentRect.width
            - BucketLayoutPaddingHorizontal
            - BucketLayoutPaddingHorizontal
            - (BucketLayoutSpacing.x * Mathf.Max(0, columnCount - 1));
        float availableHeight =
            parentRect.height
            - (BucketLayoutSpacing.y * Mathf.Max(0, rowCount - 1));

        float cellWidth = Mathf.Min(
            BucketLayoutCellSize.x,
            availableWidth / Mathf.Max(1, columnCount));
        float cellHeight = Mathf.Min(
            BucketLayoutCellSize.y,
            availableHeight / Mathf.Max(1, rowCount));

        // 너무 줄어들어 드래그 시작과 글씨 판독이 어려워지는 상황만 방어한다.
        return new Vector2(
            Mathf.Max(96f, cellWidth),
            Mathf.Max(84f, cellHeight));
    }

    private AccessibilityDisplaySettings GetAccessibilityDisplaySettings()
    {
        accessibilityDisplaySettings = AccessibilityDisplaySettings.Instance;

        if (accessibilityDisplaySettings == null)
        {
            throw new InvalidOperationException(
                "PaintBucketGenerator requires an AccessibilityDisplaySettings reference.");
        }

        if (accessibilityDisplaySettings.ActivePalette == null)
        {
            throw new InvalidOperationException(
                "AccessibilityDisplaySettings requires an active palette.");
        }

        return accessibilityDisplaySettings;
    }
}
