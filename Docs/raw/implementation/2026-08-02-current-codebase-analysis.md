# NaN2026 Current Codebase Analysis Snapshot

> Source: RiderMCP analysis of `Assets/_NAN/Scripts/`; unity-cli inspection of the open Unity project, build scenes, StageData assets, compilation state, and Console
> Collected: 2026-08-02
> Published: 2026-08-02

## 분석 범위와 도구 상태

- Rider가 `Assembly-CSharp`, `Assembly-CSharp-Editor`, 플러그인 어셈블리를 포함한 Unity 솔루션을 인식했다.
- `Assets/_NAN/Scripts/`에는 런타임 및 에디터 C# 파일 46개가 있다.
- Unity Editor는 `D:/02_Work/GameDevs/nan2026` 프로젝트에 연결되었고 버전은 6000.3.19f1, unity-cli Connector는 0.3.22다.
- Rider 프로젝트 문제 조회 결과 Warning 이상 문제는 0개였다.
- Build Settings 씬은 `Title`, `GridTestScene`, `SampleScene` 순서다.

## 코드 디렉터리

```text
Assets/_NAN/Scripts
├─ Grid/       격자 상태, 좌표·배치 규칙, 셀·보드 View, 플레이 조립
├─ Paint/      물감통 데이터·View·선택, BFS 확산 계획, 확산 연출
├─ Command/    물감 사용 실행과 Undo/Clear 이력
├─ Stage/      스테이지 에셋, 카탈로그, 정답 판정, 씬 간 선택 상태, 에디터
├─ UI/         타이틀 패널, 스테이지 미리보기, 설정·씬 전환 UI
└─ root        전역 설정·사운드·씬 전환·접근성 팔레트·공통 싱글톤
```

## 주요 실행 흐름

```text
Title 메인 → StagePreviewSystem에서 스테이지 선택
→ StageRunContext.SelectStage
→ SceneTransitionManager.LoadSceneAndWaitForReady("GridTestScene")
→ GridTestController가 StageData로 플레이판·정답판·물감통 생성
→ NotifySceneReady

물감통 선택 + 셀 클릭
→ PaintBucketController.BucketUseRequested
→ PaintBucketUseCommand.Execute
→ PaintSpreadCalculator.Calculate
→ GridState 최종 상태 확정 + PaintApplicationPlan 보관
→ PaintSpreadSequencePlayer가 거리별 View/이펙트 재생
→ StageClearChecker.Check
```

## 현재 스테이지 콘텐츠

`StageCatalog.asset`에는 `StageData1`부터 `StageData10`까지 10개 스테이지가 등록되어 있다. 크기는 5×5, 6×6, 7×7이며, 물감통 2~10개와 벽 0~21개를 사용한다. 같은 디렉터리에 카탈로그에서 사용하지 않는 SampleStageData 에셋도 있다.

## 구조적 관찰

- `GridState`, `PaintSpreadCalculator`, `PaintApplicationPlan`, `StageClearChecker`는 Unity 생명주기와 분리된 일반 C# 객체다.
- `StageData`, `StageCatalog`, `ColorPaletteSO`, `PaintVisualSet`, `SoundLibrary`는 콘텐츠와 공통 리소스를 ScriptableObject로 보관한다.
- `GridTestController`는 이름과 달리 현재 스테이지 플레이의 구성 루트 역할을 한다.
- 논리 상태는 커맨드 실행 시 즉시 최종값으로 바뀌고, 화면은 불변 확산 계획을 나중에 재생한다.
- 확산 중에는 물감통 입력과 Undo/Clear 입력을 함께 잠근다.
- Clear 물감은 모든 대상 셀을 같은 프레임에 지우고, RGB 물감은 최단 거리 wave 순으로 진행한다.
- 색상 규칙과 표시는 `PaintState`와 `ColorPaletteSO`로 분리되며 팔레트·심볼 변경 이벤트가 셀과 미리보기에 전파된다.
- 씬 전환과 설정 팝업은 Resources 프리팹을 지연 생성하고 UniTask와 DOTween으로 연출한다.

## 확인된 현재 경계

- 클리어 이벤트의 실제 후속 UI/진행 저장은 아직 없고 로그만 출력한다.
- 설정 팝업은 표시·숨김 골격과 사운드 볼륨 API가 있으나, 현재 읽은 코드에서 팔레트·심볼·볼륨 컨트롤의 연결은 확인되지 않았다.
- `SoundManager`는 자체 싱글톤 패턴을 사용해 `LazyPersistentSingleton<T>` 계열과 구현 방식이 다르다.
- Redo, 커맨드 실행 예외 시 트랜잭션 롤백, Undo 버튼 자동 활성화는 구현되어 있지 않다.
