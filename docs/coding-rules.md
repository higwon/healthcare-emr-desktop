> 2026-10-01 재설계: 제품·Feature·실행 우선순위는 [Windows 헬스케어 재설계](healthcare-windows-baseline.md)를 따른다. 아래 공통 계층·런타임·C#/WPF·수명·검증 규칙은 유지하되, 이전 Feature 모델/endpoint/이슈 순서/시안 승인을 새 제품 기준으로 사용하지 않는다.

# C# · WPF 코딩 규칙

설계 기준 PR에서 검토한다. `.editorconfig`는 이 규칙의 자동 검사 가능한 부분만 담당한다.

## C# 기본

- 타입/메서드/속성 PascalCase, private field `_camelCase`, local/parameter camelCase, interface `I` 접두어, Task 반환 메서드 `Async` 접미어.
- 파일당 주요 타입 하나, 타입 이름과 파일 이름 일치. namespace는 프로젝트·폴더 경계를 반영한다.
- block-scoped namespace와 네 칸 들여쓰기를 사용한다. 한 줄에 여러 선언·조건·업무 처리를 압축하지 않는다.
- nullable을 켜고 경계 입력을 검증한다. `!`로 경고를 숨기는 대신 검증·명시적 상태를 사용한다.
- public mutable domain DTO를 Binding과 저장 객체로 동시에 사용하지 않는다. entity 변경은 유효한 메서드/명령, 입력은 불변 snapshot.
- 날짜·시간·ID·단위에 값 객체를 사용한다. 금액·건강 측정 값은 필요한 정밀도를 정의한 decimal. UI 차트 좌표만 double.
- Domain에서 DateTime.Now, Guid.NewGuid, HTTP, DB, MessageBox, 서비스 locator를 직접 호출하지 않는다. clock/ID port 또는 호출자 값을 사용한다.
- 예외는 기술 실패, validation/conflict는 명시적인 결과로 모델링한다. 원문 exception을 사용자 화면에 출력하지 않는다.
- catch(Exception)으로 성공처럼 진행하거나 오류를 삼키지 않는다. 앱 최종 error boundary에서는 안전한 메시지·진단·종료/복구 정책을 적용한다.

## 비동기·명령

- `.Result`, `.Wait`, dispatcher thread의 blocking I/O 금지. HTTP/DB async를 Task.Run으로 감싸지 않는다.
- `async void`는 이벤트 또는 ICommand 실행 경계에만 허용하고 경계 내부에서 오류를 처리한다. 업무 메서드는 Task/Task<T>.
- 명령 실행 중 중복 실행을 막고 CanExecute 변경을 통지한다. cancel/exception/finally 후 상태를 항상 정리한다.
- CancellationToken은 연쇄 전달한다. read 취소와 서버 write 취소 의미를 혼동하지 않는다.
- 실행 중 입력 변경에 영향을 받지 않도록 command snapshot을 만든다. operation ID와 expected version을 해당 snapshot 수명 동안 보존한다.
- Application/Infrastructure의 await는 필요한 경우 ConfigureAwait(false), ViewModel은 UI context 정책을 명시한다.
- lock 내부에서 await·이벤트·외부 callback을 호출하지 않는다. DB transaction·동시성 검사로 불변식을 지킨다.
- HttpClient는 adapter 수명에 맞춰 재사용하고 request/response를 dispose한다. 타임아웃·취소·연결 실패·status code를 구분한다.

## MVVM 경계

- View: Binding·템플릿·포커스·시각 상태·Animation. ViewModel: 화면 상태·명령·Application port 호출. 업무 규칙: Domain/Application.
- ViewModel에서 View/Window/Control/HttpClient/DB/MessageBox를 직접 참조하지 않는다. dispatcher/navigation/dialog는 작은 port 또는 View bridge.
- 모든 화면을 MainViewModel에 넣지 않는다. navigation·registration·schedule·detail 상태와 수명을 분리한다.
- computed property의 입력이 바뀌면 해당 property도 통지한다. 매번 전체 PropertyChanged(null) 호출로 의존성을 숨기지 않는다.
- CommandManager 의존은 Desktop 경계에만 둔다. Domain/Application 테스트가 WPF STA를 요구하지 않아야 한다.
- Toolkit version·command 패턴은 ADR-002 이후 통일한다. 일부 화면만 별도 MVVM 프레임워크로 만들지 않는다.

## WPF XAML·Control

- 색·간격·글자·반경·상태 리소스를 ResourceDictionary에서 관리한다. 반복 inline 색상·복제 ControlTemplate을 줄인다.
- 화면은 UserControl/DataTemplate로 분리하고 Window는 shell/host로 둔다. 업무 화면 전체를 한 XAML에 압축하지 않는다.
- code-behind는 initialize·시각 동작·윈도 수명 bridge만 허용한다. 업무 변경은 Command binding.
- Grid/Auto/* 크기와 wrap/minmax로 설계한다. 고정 위치 Canvas는 차트 등 좌표가 의미인 요소에 한정한다.
- 목록은 기본 가상화·Recycling·bounded query부터 사용한다. ItemsControl/외부 ScrollViewer가 가상화를 깨는지 실제 측정한다.
- 조회 pagination과 UI virtualization은 서로 다른 문제다. 전체 이력을 불러온 뒤 virtualization만 켜고 완료로 하지 않는다.
- Custom Control은 첫 데이터 탐색 단계부터 dependency property·템플릿·키보드·AutomationPeer를 제공한다. 외부 Chart/UI library를 사용하지 않는다.
- Measure/Arrange·hit testing·selection·focus·DPI 좌표 변환과 invalidation 근거를 분리한다. 모든 DP에 AffectsMeasure를 붙이거나 hover마다 전체 데이터를 재계산하지 않는다.
- 대량 데이터는 query paging·container recycling·drawing 비용을 별도 측정한다. downsampling은 selection·결측·극값·표 원본을 보존할 때만 측정 근거로 도입한다.
- DependencyProperty callback에서 HTTP·DB를 호출하지 않는다. drawing과 데이터 갱신·선택 상태를 분리한다.
- 포커스 시각 표시를 제거하지 않는다. tab 순서·escape·초점 복귀·기본 버튼·스크린리더 명칭을 정의한다.
- high DPI는 DIP·UseLayoutRounding·아이콘/텍스트/선/Popup를 함께 확인한다. 브라우저 폭 테스트를 WPF DPI 검증으로 대체하지 않는다.
- 상품 메뉴에 개발용 장애 버튼을 섞지 않는다. 후속 기능은 눌러도 빈 placeholder만 뜨는 탐색으로 방치하지 않는다.

## 수명·해제

- event/messenger/timer/token 등록에는 같은 scope의 해제가 있어야 한다. 중복 Loaded 등록 방지.
- View가 외부 event를 연결하면 Loaded/Unloaded에서 짝을 맞추되, ViewModel Dispose는 실제 scope/window 종료 때만 호출한다.
- 짧은 Unloaded/reload를 앱 종료로 취급하지 않는다. 취소·dispose 이후 지연 응답의 화면 변경을 막는다.
- static event·DispatcherTimer·CollectionChanged·비동기 callback의 root와 해제를 실제 profiler로 확인한다. 반복 상세 열기/닫기와 Loaded/Unloaded 시나리오를 초기 UI PR부터 보존한다.
- async initialization은 idempotent. 작업 종료 순서와 UI dispatcher shutdown 중 callback 정책을 테스트한다.

## 데이터·테스트·로그

- DTO는 property casing·optional/null·enum·decimal·UTC 형식을 계약 테스트로 확인한다. reflection 기반 private mutation이나 문자열 JSON 조립 금지.
- operation key를 로그에 남길 수 있어도 의료 본문·약 이름·증상·token·원문 payload는 남기지 않는다. 개인정보 없는 allowlist 진단.
- 테스트 이름은 상황_행동_결과. clock·ID·지연을 주입하고 Thread.Sleep에 의존하지 않는다.
- 규칙·race·실패·중복·복구에 집중한다. getter/setter를 그대로 따라 쓰는 테스트나 허위 커버리지 목표를 만들지 않는다.
- WPF 구현 PR은 Task 종류별 관련 품질 항목만 적용한다(ui-guide/epics-and-tasks). UI Foundation은 DPI/focus/Automation/Binding, Data UI는 paging/query/apply, Control은 직접 fixture profiling, Workflow UI는 실패/retry/conflict를 검증한다. 화면 변경 없는 기반 PR에 rendering benchmark를 요구하지 않는다. 컴파일만 통과했다고 UI 완료로 하지 않는다.
