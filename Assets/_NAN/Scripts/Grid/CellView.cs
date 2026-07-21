using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 셀 한칸의 시각적인 표현을 담당하는 클래스, 추후 paintstate에 따라 애니메이션 재생등을 담당
/// </summary>
public sealed class CellView : MonoBehaviour, IPointerClickHandler
{
    //셀의 grid 내에서의 논리 좌표
    public Vector2Int GridPosition { get; private set; }

    //추후 물감통을 들고 셀을 클릭할때 호출 될 수 있는 이벤트
    public event Action<Vector2Int> Clicked;
    
    // 셀의 Sprite 색상을 변경할 Renderer, 임시용으로 물감 사용을 처리하기 위해 사용.
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();
        SetPaint(PaintState.Empty);
    }
    
    /// <summary>
    /// 셀 초기화 함수, 논리좌표를 구하고, 셀 이름을 편집한다
    /// </summary>
    public void Initialize(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        gameObject.name =
            $"Cell ({gridPosition.x}, {gridPosition.y})";
    }

    /// <summary>
    /// 마우스 클릭 또는 터치 입력을 받아
    /// 셀 클릭 이벤트를 발생시킨다.
    /// </summary>
    public void OnPointerClick(
        PointerEventData eventData)
    {
        Clicked?.Invoke(GridPosition);
    }

    
    /// <summary>
    /// PaintState에 맞게 셀의 임시 표시 색상을 변경한다.
    /// </summary>
    public void SetPaint(PaintState paintState)
    {
        spriteRenderer.color =
            GetPaintColor(paintState);
    }
    private Color GetPaintColor(
        PaintState paintState)
    {
        return paintState switch
        {
            PaintState.Empty =>
                new Color(0.25f, 0.25f, 0.25f),

            PaintState.Red => Color.red,
            PaintState.Green => Color.green,
            PaintState.Blue => Color.blue,
            PaintState.Yellow => Color.yellow,
            PaintState.Cyan => Color.cyan,
            PaintState.Magenta => Color.magenta,
            PaintState.White => Color.white,

            _ => Color.black
        };
    }
}