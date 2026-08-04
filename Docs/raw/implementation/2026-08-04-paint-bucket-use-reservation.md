# Paint Bucket Use Reservation Implementation Snapshot

> Source: NaN2026 local implementation in `Assets/_NAN/Scripts/Grid/GridController.cs`, `Assets/_NAN/Scripts/Paint/PaintBucketController.cs`, and `Assets/_NAN/Scripts/Paint/PaintBucketView.cs`
> Collected: 2026-08-04
> Published: 2026-08-04

## 구현 배경

기존 플레이 흐름은 물감통 하나를 사용하면 확산 연출이 끝날 때까지 물감통 입력과 Undo/Clear 입력을 함께 막았다. 그래서 플레이어는 현재 물감통의 연출이 끝난 뒤에야 다음 물감통을 드래그해서 사용할 수 있었다.

새 요구사항은 현재 물감통의 연출 및 사용 처리가 끝나기 전에 다음 물감통 드래그 입력을 미리 받을 수 있게 하는 것이다. 단, 모델 상태와 Undo 이력은 여전히 한 번에 하나의 물감통 사용만 실행해야 하므로, 연출 중 들어온 입력은 즉시 커맨드로 실행하지 않고 예약 상태로 보관한다.

## GridController 변경

`GridController`는 예약 요청을 순서대로 보관하기 위해 `Queue<PendingBucketUseRequest>`를 가진다. `PendingBucketUseRequest`는 물감통 ID, `PaintBucket` 데이터, 드롭한 셀 좌표를 저장하는 읽기 전용 struct다.

`HandleBucketUseRequested`는 `activePlan`이 있으면 현재 확산 연출이 진행 중이라고 보고 `ReserveBucketUse`로 요청을 넘긴다. 진행 중인 연출이 없으면 기존처럼 `ExecuteBucketUse`를 호출해 즉시 커맨드를 실행한다.

`ExecuteBucketUse`는 `playUseSound` 인자를 받아 즉시 사용 입력에서는 효과음을 재생하고, 예약 큐에서 실제 실행되는 경우에는 이미 예약 시점에 피드백을 준 것으로 보고 중복 효과음을 내지 않는다. 커맨드 실행에 성공하면 `activePlan`을 저장하고 `PaintSpreadSequencePlayer.Play` 코루틴을 시작한다.

연출 중 입력 잠금은 커맨드 입력과 물감통 입력을 분리했다. 커맨드 실행 후에는 `SetGameplayInputEnabled(false, true)`로 Undo/Clear 같은 커맨드 UI는 막고, 물감통 드래그/드롭 입력은 계속 허용한다. 이렇게 해야 연출 중 다음 물감통을 드래그해 예약할 수 있다.

`HandlePaintSequenceCompleted`는 현재 연출 상태를 비우고 입력을 풀어준 뒤 `TryExecuteNextPendingBucketUse`를 먼저 호출한다. 예약된 요청이 실제 커맨드 실행에 성공하면 다음 연출이 바로 시작되므로 정답 판정을 미룬다. 예약 큐가 비었거나 실행할 요청이 없을 때만 `StageClearChecker.Check`를 호출한다.

`CompleteActiveSequenceImmediately`는 현재 연출을 강제로 끝낼 때 남은 View 결과를 즉시 적용하고 입력을 복구한다. `CreateGrid`와 `OnDisable`은 `ClearPendingBucketUseRequests`를 호출해 아직 실제 실행되지 않은 예약을 취소하고 물감통 예약 표시를 되돌린다.

## PaintBucketController 변경

`PaintBucketController.BucketEntry`에 `IsReserved` 상태가 추가되었다. 예약된 물감통은 아직 `PaintBucketUseCommand`가 실행된 것은 아니지만, 플레이어가 이미 다음 사용으로 지정한 물감통이므로 다시 클릭하거나 드래그할 수 없어야 한다.

`Reserve(int bucketId)`는 물감통이 이미 소모되었거나 예약되었으면 false를 반환한다. 예약 가능하면 선택 상태를 해제하고 `BucketEntry.SetReserved(true)`로 View 상태까지 함께 바꾼다.

`ReleaseReservation(int bucketId)`는 초기화, 비활성화, 예약 실행 실패 같은 상황에서 아직 실행되지 않은 예약을 되돌린다.

`Consume(int bucketId)`는 예약된 물감통이 실제 실행될 때 예약 상태를 먼저 해제한 뒤 소모 상태로 확정한다. 따라서 예약 상태와 소모 상태가 동시에 오래 유지되지 않고, 최종적으로는 기존 소모 처리와 같은 상태가 된다.

물감통 클릭, 선택 물감통으로 셀 클릭, 드래그 드롭 사용 요청은 모두 `IsConsumed || IsReserved`이면 무시한다. 이 검사는 같은 물감통이 여러 번 예약되거나 이미 예약된 물감통이 다시 사용 요청을 내는 것을 막는다.

## PaintBucketView 변경

`PaintBucketView`에는 `isReserved` 상태와 `SetReserved(bool)` API가 추가되었다. 예약된 물감통은 소모된 물감통과 마찬가지로 Button 입력을 막고 GameObject와 월드 비주얼을 숨긴다.

예약 물감통을 반투명하게 남겨두는 방식도 고려되었지만, 드래그 중 레이아웃에서 빠졌던 물감통이 드롭 직후 다시 돌아오면 아래 물감통 레이아웃이 되돌아오는 시각적 흔들림이 생긴다. 최종 구현은 예약 순간 물감통을 숨겨서 사용된 물감통처럼 보이게 하고, 이후 실제 커맨드 실행 전까지 추가 처리를 받지 않게 한다.

## 현재 동작 요약

```text
첫 번째 물감통 드롭
→ PaintBucketUseCommand 즉시 실행
→ GridState 최종 상태 적용
→ 확산 연출 시작
→ 커맨드 입력 잠금, 물감통 입력 유지

연출 중 두 번째 물감통 드롭
→ 커맨드 실행하지 않음
→ 물감통 예약 상태로 전환
→ 예약 큐에 요청 저장
→ 예약 물감통 숨김

첫 번째 연출 완료
→ 예약 큐에서 다음 요청 Dequeue
→ 두 번째 PaintBucketUseCommand 실행
→ 두 번째 확산 연출 시작

예약 큐가 비고 마지막 연출 완료
→ StageClearChecker.Check
```

## 검증

- `dotnet build NaN2026.sln` 성공
- `unity-cli --project "<현재 프로젝트 절대 경로>" editor refresh --compile` 성공
- `unity-cli --project "<현재 프로젝트 절대 경로>" console --type error,warning` 결과 `[]`
