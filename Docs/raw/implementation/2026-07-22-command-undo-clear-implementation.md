# Command Undo and Clear Implementation Snapshot

> Source: `Assets/_NAN/Scripts/Command/`; `Assets/_NAN/Scripts/Grid/GridState.cs`; `Assets/_NAN/Scripts/Grid/GridTestController.cs`; `Assets/_NAN/Scenes/GridTestScene.unity`
> Collected: 2026-07-22
> Published: 2026-07-22

## ICommand

```csharp
public interface ICommand
{
    bool Execute();
    void Undo();
}
```

`Execute`는 실제 실행 여부를 반환한다. 실행에 성공한 커맨드만 되돌리기 이력에 들어가며, `Undo`는 이미 실행된 커맨드의 상태를 복원한다.

## CommandController

`CommandController`는 `Stack<ICommand>`를 보유하는 MonoBehaviour다. `Execute(ICommand)`가 성공한 커맨드만 스택에 넣고, 내부 `UndoLast`는 마지막 커맨드 하나를 꺼내 되돌린다. 내부 `UndoAll`은 스택이 빌 때까지 최근 커맨드부터 역순으로 되돌린다.

Unity `Button.onClick` 연결용으로 반환값과 매개변수가 없는 다음 public 메서드를 제공한다.

```csharp
public void HandleUndoButtonClicked();
public void HandleClearButtonClicked();
```

`ClearHistory`는 커맨드를 실행 취소하지 않고 이력만 제거한다. 새 테스트 격자를 만들기 전에 이전 격자를 참조하는 커맨드를 폐기하는 용도로 사용한다.

## PaintBucketUseCommand 상태 기록

`PaintBucketUseCommand`는 확산 계산 결과에 포함된 좌표만 `List<PaintSnapshot>`에 저장한다. `PaintSnapshot`은 좌표와 실행 직전 `PaintState`를 함께 보관하는 readonly struct다. 특정 좌표를 검색하지 않고 실행 시 순서대로 기록한 뒤 Undo에서 전체 순회하므로 Dictionary 대신 연속 저장 구조인 List를 사용한다.

실행 흐름은 다음과 같다.

1. 같은 커맨드가 이미 실행 중인지 확인한다.
2. 벽과 범위를 반영해 영향 좌표를 계산한다.
3. 영향 좌표별 실행 직전 `PaintState`를 기록한다.
4. 물감통 소모에 성공하지 못하면 기록을 버리고 실패한다.
5. 각 좌표의 논리 상태와 View를 갱신한다.
6. 실행 완료 상태를 기록한다.

Undo는 저장된 모든 좌표에 `GridState.SetPaint`를 호출해 정확한 이전 값을 덮어쓰고, `GridView.SetCellPaint`로 화면을 동기화한 다음 사용한 물감통을 복원한다. `isExecuted`가 false라면 중복 Undo를 무시한다.

## GridState.SetPaint

RGB 물감은 비트 OR로 합성되므로 적용한 색만 역연산해서는 기존 혼합 상태를 정확히 복원할 수 없다. `SetPaint(position, paint)`는 셀 값을 전달받은 상태로 교체하며 커맨드 스냅샷 복원의 모델 API로 사용된다.

## GridTestController 연동

`GridTestController`는 물감통 사용 요청을 받으면 `PaintBucketUseCommand`를 생성하고 직접 실행하지 않고 `CommandController.Execute`에 전달한다. 새 테스트 격자를 생성할 때는 `ClearHistory`를 먼저 호출해 이전 `GridState`, `GridView`, 물감통 ID를 참조하는 커맨드를 폐기한다.

## GridTestScene 버튼 연결

- `UndoButton.onClick` → `CommandController.HandleUndoButtonClicked`
- `ClearButton.onClick` → `CommandController.HandleClearButtonClicked`

Clear 버튼은 `GridState.ClearAll`을 직접 호출하지 않는다. 현재까지 실행한 물감 사용을 전부 역순 Undo하여 셀 상태와 소모된 물감통을 함께 초기 상태로 복원한다.

## 현재 확장 지점

- `CanUndo` 값에 맞춘 Undo/Clear 버튼의 interactable 상태 갱신은 아직 없다.
- Redo 스택은 없다.
- `Undo`가 void이므로 Controller는 복원 실패를 전달받지 않는다.
- 실행 도중 예외가 발생했을 때 부분 변경을 롤백하는 트랜잭션 처리는 없다.
