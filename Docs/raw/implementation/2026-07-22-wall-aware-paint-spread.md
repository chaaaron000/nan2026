# Wall-Aware Paint Spread Implementation Snapshot

> Source: `Assets/_NAN/Scripts/Grid/`; `Assets/_NAN/Scripts/Paint/PaintSpreadCalculator.cs`; `Assets/_NAN/Scripts/Command/PaintBucketUseCommand.cs`
> Collected: 2026-07-22
> Published: 2026-07-22

## GridIndexUtility.cs

```csharp
public static class GridIndexUtility
{
    public static bool IsInside(Vector2Int position, int width, int height);
    public static int ToIndex(Vector2Int position, int width, int height);
    public static Vector2Int ToPosition(int index, int width, int height);
}
```

2차원 격자 좌표와 행 우선 1차원 배열 인덱스 사이의 변환 및 범위 검사를 공통화한다. `GridState`, `GridView`, `PaintSpreadCalculator`가 같은 변환 규칙을 사용한다.

## GridDirection.cs

```csharp
public enum GridDirection
{
    UP,
    RIGHT,
    DOWN,
    LEFT,
}

public static class GridDirectionUtility
{
    public static Vector2Int ToOffset(this GridDirection direction);
}
```

격자 이동 방향을 명시하고 각 방향을 한 칸짜리 `Vector2Int` 오프셋으로 변환한다.

## GridState 벽과 이동 판정

```csharp
public sealed class GridState
{
    private readonly PaintState[] cells;
    private readonly HashSet<Vector2Int> walls;

    public IReadOnlyCollection<Vector2Int> WallPositions { get; }
    public bool CanMove(Vector2Int from, Vector2Int to);
    public bool CanMove(Vector2Int position, GridDirection direction);
}
```

벽은 셀 좌표를 2배로 확장한 좌표계에 저장한다. 인접한 두 셀 좌표의 합이 두 셀 사이의 벽 위치가 되며, `HashSet.Contains`로 벽 존재 여부를 조회한다. 격자 밖이거나 인접하지 않은 셀 사이는 이동할 수 없다.

## PaintSpreadCalculator.cs

```csharp
public sealed class PaintSpreadCalculator
{
    public IReadOnlyList<Vector2Int> Calculate(
        GridState gridState,
        Vector2Int origin,
        int range);
}
```

Queue 기반 BFS로 시작 셀부터 실제 이동 가능한 최단 경로를 탐색한다. 범위 N은 시작 셀을 거리 0으로 보고 최대 N-1회 이동한 셀까지 포함한다. 각 셀에서 상·우·하·좌를 확인하고 `GridState.CanMove`가 true인 미방문 셀만 Queue에 추가한다.

방문 여부는 `bool[]`에 행 우선 셀 인덱스로 저장한다. 같은 셀이 여러 경로로 Queue에 중복 삽입되지 않도록 enqueue 시점에 방문 처리한다. 반환 목록은 BFS 특성상 최단 거리 비내림차순이다.

## PaintBucketUseCommand.cs

```csharp
public sealed class PaintBucketUseCommand : ICommand
{
    public bool Execute();
    public void Undo();
}
```

`Execute`는 Calculator로 영향 좌표를 계산하고, 물감통 소모가 성공하면 각 셀에 `GridState.AddPaint` 또는 `ClearPaint`를 적용한다. 반환된 최종 상태를 `GridView.SetCellPaint`에 전달해 화면을 동기화한다. `Undo`는 인터페이스 경계만 마련되어 있으며 아직 상태 복원은 구현되지 않았다.
