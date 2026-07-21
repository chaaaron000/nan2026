using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 생성된 물감통 한 개의 시각적 표현과 클릭 입력을 담당하는 클래스
/// </summary>
public sealed class PaintBucketView : MonoBehaviour
{
    // 물감통 클릭 입력을 받는 UI 버튼
    [SerializeField]
    private Button button;

    // 물감통 Sprite를 표시하는 이미지
    [SerializeField]
    private Image bucketImage;

    // 물감통의 확산 범위를 표시하는 텍스트
    [SerializeField]
    private TMP_Text rangeText;

    /// <summary>
    /// 플레이어가 이 물감통을 클릭했을 때 발생한다.
    /// </summary>
    public event Action<PaintBucketView> Clicked;

    private void Awake()
    {
        button.onClick.AddListener(
            HandleButtonClicked);
    }

    /// <summary>
    /// 물감통 데이터와 Sprite를 바탕으로
    /// View의 최초 표시 상태 (이미지, 텍스트)를 설정한다.
    /// </summary>
    public void Initialize(
        int range,
        Sprite bucketSprite)
    {
        if (bucketSprite == null)
        {
            throw new ArgumentNullException(
                nameof(bucketSprite));
        }

        //스프라이트와 거리 텍스트 표시
        bucketImage.sprite = bucketSprite;
        rangeText.text = range.ToString();

        SetSelected(false);
        SetConsumed(false);
    }

    /// <summary>
    /// 물감통 선택시 강조하는 효과를 넣는다. 현재는 아무 기능이 없다.
    /// </summary>
    public void SetSelected(bool selected)
    {

    }

    /// <summary>
    /// 물감통의 소모 상태를 화면에 반영한다. 현재는 소모 상태를 SetActive로 표현한다.
    /// </summary>
    public void SetConsumed(bool consumed)
    {
        button.interactable = !consumed;

        if (consumed)
        {
            SetSelected(false);
        }

        gameObject.SetActive(!consumed);
    }

    private void HandleButtonClicked()
    {
        Clicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleButtonClicked);
        }
    }
}