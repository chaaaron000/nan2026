# Grid Core Implementation Snapshot

> Source: `Assets/_NAN/Scripts/Grid/`
> Collected: 2026-07-20
> Published: 2026-07-20

## GridState.cs

```csharp
public sealed class GridState
{
    private readonly PaintState[] cells;
    public int Width { get; }
    public int Height { get; }

    public GridState(int width, int height);
    public PaintState GetPaint(Vector2Int position);
    public PaintState AddPaint(Vector2Int position, PaintState paint);
    public PaintState ClearPaint(Vector2Int position);
    public void ClearAll();
    public bool IsInside(Vector2Int position);
}
```

현재 구현은 `width * height` 크기의 1차원 `PaintState` 배열을 생성하고, `position.y * Width + position.x` 공식으로 논리 좌표를 배열 인덱스로 변환한다. 범위를 벗어난 좌표는 `ArgumentOutOfRangeException`으로 거부한다.

## GridView.cs

```csharp
public sealed class GridView : MonoBehaviour
{
    [SerializeField] private CellView cellPrefab;
    [SerializeField] private float cellSize = 1f;
    private CellView[] cellViews;

    public void CreateGrid(GridState gridState);
    public void ClearGrid();
}
```

`GridState`의 너비와 높이를 순회하며 `CellView` 프리팹을 생성한다. 각 셀의 로컬 위치는 논리 좌표에 `cellSize`를 곱해 결정한다. 다시 생성하기 전에는 기존 셀을 제거한다.

## CellView.cs

```csharp
public sealed class CellView : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public event Action<Vector2Int> Clicked;
    public void Initialize(Vector2Int gridPosition);
}
```

각 셀의 논리 좌표를 기억하고, 클릭 시 해당 좌표를 이벤트로 전달한다. 현재는 색상 렌더링과 애니메이션 메서드가 아직 구현되지 않았다.

## PaintState.cs

```csharp
[Flags]
public enum PaintState
{
    Empty = 0,
    Red   = 1 << 2,
    Green = 1 << 1,
    Blue  = 1 << 0,
    Yellow  = Red | Green,
    Cyan    = Green | Blue,
    Magenta = Red | Blue,
    White   = Red | Green | Blue
}
```

R, G, B 포함 여부를 독립적인 비트로 표현하며, 혼합색은 기본색 플래그의 조합으로 정의한다.
