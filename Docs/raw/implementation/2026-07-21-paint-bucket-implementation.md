# Paint Bucket Implementation Snapshot

> Source: `Assets/_NAN/Scripts/Paint/`; `Assets/_NAN/Scripts/Grid/GridView.cs`
> Collected: 2026-07-21
> Published: 2026-07-21

## PaintBucket.cs

```csharp
[Serializable]
public sealed class PaintBucket
{
    [SerializeField] private PaintType paintType;
    [SerializeField, Min(1)] private int range = 1;

    public PaintType PaintType => paintType;
    public int Range => range;
}
```

스테이지에 직렬화할 물감통 한 개의 사양이다. 같은 사양의 물감통이 여러 개라면 수량 필드를 두지 않고 목록에 항목을 개수만큼 둔다.

## PaintType.cs

```csharp
public enum PaintType
{
    Red,
    Green,
    Blue,
    Clear
}
```

`Clear`는 셀에 남는 흰색 상태가 아니라 기존 RGB를 제거하는 물감 행동을 뜻한다.

## PaintBucketVisualData.cs

```csharp
public sealed class PaintBucketVisualData : ScriptableObject
{
    public Sprite GetSprite(PaintType paintType);
}
```

R, G, B, Clear별 공통 물감통 Sprite를 보관하고 `PaintType`에 대응하는 Sprite를 반환한다.

## PaintBucketGenerator.cs

```csharp
public sealed class PaintBucketGenerator : MonoBehaviour
{
    [SerializeField] private PaintBucketView paintBucketPrefab;
    [SerializeField] private Transform paintBucketParent;
    [SerializeField] private PaintBucketVisualData visualData;

    public IReadOnlyList<PaintBucketView> Generate(
        IReadOnlyList<PaintBucket> bucketData);
}
```

전달받은 물감통 데이터마다 프리팹을 하나씩 생성한다. `PaintBucketVisualData.GetSprite`로 종류별 Sprite를 찾고 `PaintBucketView.Initialize(range, sprite)`를 호출한다. 생성 결과는 입력 데이터와 같은 순서의 배열로 반환하며, 생성된 목록을 내부에서 계속 소유하거나 제거하지 않는다.

## PaintBucketView.cs

```csharp
public sealed class PaintBucketView : MonoBehaviour
{
    public event Action<PaintBucketView> Clicked;

    public void Initialize(int range, Sprite bucketSprite);
    public void SetSelected(bool selected);
    public void SetConsumed(bool consumed);
}
```

UI `Button`, `Image`, `TMP_Text`를 참조한다. 초기화 시 Sprite와 범위 숫자를 표시하고, 버튼 클릭을 `Clicked` 이벤트로 변환한다. 현재 선택 표현은 비어 있으며, 소모는 버튼과 GameObject 비활성화로 표현한다.

## PaintBucketController.cs

```csharp
public sealed class PaintBucketController : MonoBehaviour
{
    public event Action<int, PaintBucket, Vector2Int>
        BucketUseRequested;

    public void Initialize(IReadOnlyList<PaintBucket> bucketData);
    public bool Consume(int bucketId);
    public bool Restore(int bucketId);
}
```

Generator가 반환한 View와 원본 데이터, 생성 순서 ID, 소모 상태를 내부 `BucketEntry`로 묶는다. View 클릭으로 선택 상태를 관리하고, `GridView.CellClicked`를 구독해 물감통 ID·사양·셀 좌표를 `BucketUseRequested`로 전달한다. 실제 확산이나 셀 상태 변경은 수행하지 않는다. `Consume`과 `Restore`는 향후 커맨드가 호출할 공개 경계다.

재초기화 시 기존 View 클릭 이벤트를 해제하고 물감통 GameObject를 제거하며 목록·조회 테이블·선택 상태를 비운다. GridView 이벤트는 해제 후 재구독해 중복 등록을 방지하고, 파괴 시 모든 구독과 생성 오브젝트를 정리한다.

## GridView 연동

`GridView`는 생성한 각 `CellView.Clicked`를 구독하고 논리 좌표를 `CellClicked` 이벤트로 중계한다. `PaintBucketController`는 개별 셀을 직접 구독하지 않고 이 단일 이벤트를 통해 셀 선택을 받는다.
