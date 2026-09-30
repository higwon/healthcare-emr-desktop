# HC-102 MVVM·탐색·수명 구현 계약

Task [#11](https://github.com/higwon/healthcare-emr-desktop/issues/11). 제품 Shell/디자인 시스템은 HC-103이다.

## 이번 단계의 실제 호출 경로

기존 /api/v1/health를 연결 상태 화면에서 조회한다. 화면 열기·닫기·재열기는 ShellViewModel이 직접 소유한다.
빈 업무 화면/메뉴, 범용 navigation/dialog service, DI container, BaseViewModel은 추가하지 않는다.
수동 composition root는 앱 수명의 HTTP adapter와 diagnostics, 창 수명의 Shell, 화면 수명의 ConnectionViewModel을 연결한다.
Application의 IClientReadinessQuery만 HTTP 계약을 추상화한다. Domain은 그대로 비워 둔다.

UI Dispatcher 경계는 비동기 응답의 실제 UI apply에만 사용한다.
진단은 enum 종류·숫자 scope ID·elapsed milliseconds만 허용하는 구체 타입으로 시작한다.
query와 apply 시간을 분리하되 layout/render 측정은 이번 단계에 포함하지 않는다.
공용 성능 예산/성능 최적화 사례를 주장하지 않는다.

## 상태·수명

- 명령의 CanExecute 외에 실행 메서드에서도 중복 요청을 차단한다.
- 취소는 조회 세대를 변경하고 busy를 해제한다. 새 조회를 허용하며 취소를 무시한 이전 응답은 적용하지 않는다.
- 응답은 UI dispatcher에서 세대·취소·Dispose를 다시 검사한다. query 종료와 UI 적용 사이의 race도 차단한다.
- 화면 초기화는 한 번만 수행한다. 실패 후 재시도는 명시적 조회 명령이다.
- 화면 닫기/교체는 이전 VM을 Dispose한다. Window의 Closed는 Shell Dispose를 연결한다.
- Loaded/Unloaded마다 scope를 생성하거나 Dispose하지 않는다.
- 실제 추가한 event/timer는 해제한다. static event·CollectionChanged·messenger를 이번 단계에서 사용하지 않으므로 해제 확인을 위한 가짜 구독을 만들지 않는다.
- HTTP 실패/timeout/계약 오류는 안전한 고정 문구로 표시한다. 예외 본문·payload·URL을 진단에 넣지 않는다.
- Demo API는 명시적 http://127.0.0.1:<port>/로 제한한다. 설정은 composition 이전에 검증한다.

## 패키지 선택 후보와 검증

CommunityToolkit.Mvvm 8.4.2의 ObservableObject/RelayCommand/AsyncRelayCommand를 직접 사용한다.
generator·global Ioc·messenger는 사용하지 않는다. netstandard2.0 자산을 net48에서 실제 build/test/startup으로 검증한 뒤 ADR-002 선택을 확정한다.
패키지와 전이 의존성은 lockfile에 기록한다.
[공식 패키지](https://www.nuget.org/packages/CommunityToolkit.Mvvm/8.4.2),
[공식 async command 문서](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand).

## 검증 계획

제어 가능한 TaskCompletionSource로 중복, 취소 무시, out-of-order 응답, dispatcher 대기 중 Dispose, 초기화 반복, 닫기/재열기를 검증한다.
실제 WPF dispatcher·binding·Window Closed와 compiled executable startup도 검증한다.
HTTP adapter는 handler fixture로 성공/실패/계약/취소를 검사하며 loopback API 별도 프로세스 smoke를 유지한다.
DPI/Automation/대량 layout·render/메모리 profiler는 해당 후속 Task의 증거를 필요로 한다.

## 실제 실행·검증 결과

Windows 11 build 26200 / SDK 10.0.300 / Release Any CPU:
참조 경계, locked restore, Release build(경고 0·오류 0), 실제 net48 runner 테스트 34개,
별도 Desktop 프로세스 startup/ContentRendered/종료·잘못된 설정 exit 3,
별도 loopback API smoke와 API publish를 검증했다.
첫 빌드는 net48 System.Net.Http 명시 참조 누락으로 실패했고, Desktop/test 프로젝트에 framework reference를 추가한 뒤 통과했다.

WPF 테스트는 실제 Window Show, binding error trace, 백그라운드 query의 UI thread apply,
Unloaded/Loaded 반복과 Closed 해제를 확인한다.
50회 화면 scope 열기/닫기는 scope 종료 균형 검사이며 GC 수집·메모리 profiler 증거가 아니다.
HTTP adapter의 응답/오류/취소는 handler fixture 검증이다. 실제 API 별도 프로세스 smoke와 구분한다.
제품 DPI/키보드/Automation/전체 Windows matrix를 검증했다고 주장하지 않는다.

테스트가 생성하는 artifacts/ui/hc102-shell.png는 compiled WPF **개발 기반 화면**이다.
제품 UI 시안/제품 완료 증거가 아니며 CI artifact에 TRX·API 로그와 함께 보존한다.
CI 성공은 관련 PR checks에서 확인한다.

## 실행과 호출 제약

기존 [실행 가이드](foundation-build.md)의 명령을 그대로 사용한다.

```powershell
./src/HealthNote.Desktop/bin/Release/net48/HealthNote.Desktop.exe
./src/HealthNote.Desktop/bin/Release/net48/HealthNote.Desktop.exe --api-base-url http://127.0.0.1:5078/
```

API는 별도로 실행한다. 서버를 끈 상태에서도 Shell은 열리고 안전한 오류·명시적 재시도를 제공한다.
기본 HTTP timeout은 10초다. 잘못된 옵션은 창/HTTP composition 이전에 exit 3으로 종료한다.
--smoke의 exit 0은 ContentRendered 증거이며 API 연결 성공을 의미하지 않는다.

Shell/Connection의 명령·초기화·취소·Dispose 진입은 UI thread 소유다.
query continuation은 background에서 돌아오며 IUiDispatcher.ApplyAsync로 돌아와 세대/취소/종료를 확인한다.
AllowConcurrentExecutions는 취소를 무시한 옛 Task가 남아 있어도 새 세대의 재시도를 허용하기 위한 설정이다.
실행 메서드 자체의 busy/current guard로 유효한 조회/화면 생성을 하나만 허용한다.
진단 종류 enum, 숫자 scope ID, elapsed만 sink에 전달한다. 기본 Trace sink와 테스트 sink는 같은 allowlist를 받는다.
소유한 HTTP client는 앱 종료, Shell은 Window Closed, 화면 VM은 닫기/교체에 해제한다.
요청 CTS는 취소 요청만 먼저 하고 해당 provider와 queued apply가 끝난 finally에서 Dispose한다.
취소를 영구적으로 무시하는 provider의 Task 수명까지 강제 종료한다고 주장하지 않는다.
