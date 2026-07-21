using System;
using System.Collections.Generic;
using UnityEngine;

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

    // 색깔 별 물감통 Sprite를 제공하는 시각 데이터
    [SerializeField]
    private PaintBucketVisualData visualData;
    

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
            bucketSprite);

        return bucketView;
    }
}