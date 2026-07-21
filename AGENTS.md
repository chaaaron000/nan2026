# 프로젝트 작업 지침

## 위키 작업 위치

- 모든 LLM 위키 작업은 프로젝트 루트의 `Docs/` 아래에서 수행한다.
- 원문 자료는 `Docs/raw/`, 정리된 위키 문서는 `Docs/wiki/`에 저장한다.
- 프로젝트 루트에 `raw/` 또는 `wiki/` 폴더를 새로 만들지 않는다.
- 위키 관련 스킬이나 일반 지침에서 `raw/`와 `wiki/`를 프로젝트 루트 기준으로 설명하더라도, 이 프로젝트에서는 각각 `Docs/raw/`와 `Docs/wiki/`로 해석한다.
- 대화에서 위키 문서를 링크할 때도 `Docs/wiki/<topic>/<article>.md` 형식의 프로젝트 루트 상대 경로를 사용한다.

## 위키 파일 이름

- `Docs/wiki/` 아래에 새 위키 문서를 만들 때 파일 이름은 한국어로 작성할 수 있으며, 기본적으로 문서 제목과 일치하는 한국어 개념명을 사용한다.
- 한국어와 공백을 허용한다. 예: `Docs/wiki/game-design/물감 확산 퍼즐.md`
- 원본 파일의 Notion ID나 임의 식별자는 위키 문서 파일 이름에 포함하지 않는다.
- 문서 이름을 바꾸면 `Docs/wiki/index.md`와 다른 위키 문서의 상대 링크도 함께 갱신하고 링크가 유효한지 확인한다.
- 위키 시스템의 고정 파일인 `Docs/wiki/index.md`와 `Docs/wiki/log.md`는 기존 영문 이름을 유지한다.

## Unity Editor 작업

- Unity 관련 `unity-cli` 명령은 프로젝트 루트에서 실행하고 프로젝트 경로는 상대 경로 `.`으로 지정한다.
- Unity 스크립트를 작성하거나 수정한 뒤에는 `unity-cli --project "." editor refresh --compile`로 스크립트 재컴파일을 요청하고 컴파일이 끝날 때까지 기다린다.
- 컴파일 후에는 `unity-cli --project "." console --type error,warning`으로 Unity Console의 오류와 경고를 확인하고, 변경으로 인해 발생한 문제를 해결한 뒤 다시 검증한다.
- Play Mode 진입·종료, Asset Database 새로고침, 메뉴 실행, 테스트, 씬 또는 GameObject 상태 확인 등 Unity Editor 조작은 `unity-cli`를 사용한다.
- 여러 Unity Editor 인스턴스가 실행 중일 수 있으므로 모든 `unity-cli` 명령에 `--project "."`을 지정하여 현재 프로젝트의 인스턴스를 명시적으로 선택한다.
- 에디터 조작 전에는 `unity-cli --project "." status`로 연결 상태와 대상 프로젝트를 확인한다.

## Unity 로그 작성

- Unity 스크립트에서 로그, 경고, 오류, 예외, Assert를 출력할 때는 `Assets/_NAN/Scripts/DebugConsole.cs`에 정의된 래퍼를 사용한다.
- `UnityEngine.Debug` 또는 `Debug`의 `Log`, `LogWarning`, `LogError`, `LogException`, `LogFormat`, `LogWarningFormat`, `LogErrorFormat`, `Assert`를 직접 호출하지 않는다.
- 각 호출은 대응하는 `DebugConsole.Log`, `DebugConsole.LogWarning`, `DebugConsole.LogError`, `DebugConsole.LogException`, `DebugConsole.LogFormat`, `DebugConsole.LogWarningFormat`, `DebugConsole.LogErrorFormat`, `DebugConsole.Assert`로 작성한다.
- 새 로그 함수를 작성하거나 기존 로그 코드를 수정할 때도 `DebugConsole.cs`의 빌드 조건과 오버로드 규칙을 따른다.
